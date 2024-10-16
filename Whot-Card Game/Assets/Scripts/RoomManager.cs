using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class RoomManager : MonoBehaviourPunCallbacks
{
    public int selectedPlayerCount = 2; // Default to 2 players
    public GameObject twoPlayersButton; // Button for 2 players
    public GameObject threePlayersButton; // Button for 3 players

    Vector2 downSpawnPoint;
    Vector2 upSpawnPoint;
    Vector2 middleSpawnPoint1, middleSpawnPoint2; // For the third player

    public GameObject playerHandPrefab; // Prefab for instantiating player hands
    private Dictionary<int, GameObject> playerHands = new Dictionary<int, GameObject>();
    GameManager gameManager;
    public GameObject roomPanel;
    public GameObject errorPanel;
    public TMP_InputField joinRoomInput;
    private string lastCreatedRoomId;
    public int roomIdLength = 6; // Length of the room ID
    private const string RoomIdProperty = "RoomID";
    private bool isConnectedToLobby = false;
    private bool roomListUpdated = false;
    private List<RoomInfo> roomList = new List<RoomInfo>();
    public static string currentRoomName = "RoomID";
    public bool isMax = false;
    LoadConnecting loadConnecting;
    public GameObject settingGameScreen;
    public GameObject loadingScreen, roomMenuScreen, errorMenu, createRoomMenu, joinRoomMenu;

    void Start()
    {
        PhotonNetwork.ConnectUsingSettings();
        gameManager = FindObjectOfType<GameManager>();
        downSpawnPoint = new Vector2(0.1f, -3f);
        upSpawnPoint = new Vector2(0.1f, 3f);
        middleSpawnPoint1 = new Vector2(-1f, 3f); // Middle position for the third player
        middleSpawnPoint2 = new Vector2(1f, 3f);
        loadConnecting = FindObjectOfType<LoadConnecting>();
    }

    // Method to handle when the "2 Players" button is clicked
    public void OnClickTwoPlayers()
    {
        selectedPlayerCount = 2;
        Debug.Log("Selected player count: 2");
    }

    // Method to handle when the "3 Players" button is clicked
    public void OnClickThreePlayers()
    {
        selectedPlayerCount = 3;
        Debug.Log("Selected player count: 3");
    }

    // This method is called when creating a room
    public void OnClickCreate()
    {
        lastCreatedRoomId = GenerateRoomId();
        RoomOptions roomOptions = new RoomOptions { MaxPlayers = (byte)selectedPlayerCount }; // Use selected player count
        roomOptions.CustomRoomProperties = new ExitGames.Client.Photon.Hashtable { { RoomIdProperty, lastCreatedRoomId } };
        roomOptions.CustomRoomPropertiesForLobby = new string[] { RoomIdProperty };
        PhotonNetwork.CreateRoom(lastCreatedRoomId, roomOptions);
        loadConnecting.roomIDText.text = lastCreatedRoomId;
        Debug.Log("Room created with ID: " + lastCreatedRoomId + " for " + selectedPlayerCount + " players.");
    }

    public void OnJoinCreate()
    {
        if (isConnectedToLobby)
        {
            string roomIdToJoin = joinRoomInput.text.ToUpper();

            if (!string.IsNullOrEmpty(roomIdToJoin))
            {
                PhotonNetwork.JoinRoom(roomIdToJoin);
            }
            else
            {
                Debug.LogWarning("No room ID entered to join.");
            }
        }
        else
        {
            Debug.LogWarning("Not connected to lobby yet. Please wait.");
        }
    }
    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        base.OnJoinedLobby();
        Debug.Log("We are in the lobby");
        isConnectedToLobby = true;
        settingGameScreen.SetActive(false);

    }

    public override void OnRoomListUpdate(List<RoomInfo> updatedRoomList)
    {
        base.OnRoomListUpdate(updatedRoomList);
        roomList.Clear();
        roomList.AddRange(updatedRoomList);
        roomListUpdated = true;
        Debug.Log("Room list updated with " + roomList.Count + " rooms.");

        // Try joining existing rooms only after the room list has been updated

        if (loadConnecting.isPlayRandom)
            TryJoinExistingRoom();
    }

    public void CheckRoomList()
    {
        TryJoinExistingRoom();
    }
    // Override OnJoinedRoom to handle the correct number of players
    public override void OnJoinedRoom()
    {
        photonView.RPC("RPC_InstantiatePlayerHand", RpcTarget.AllBuffered, PhotonNetwork.LocalPlayer.ActorNumber);

        if (PhotonNetwork.IsMasterClient)
        {
            gameManager.InitializeGame();
        }
    }

    [PunRPC]
    void RPC_InstantiatePlayerHand(int actorNumber)
    {
        if (!playerHands.ContainsKey(actorNumber))
        {
            // Determine where to spawn the player's hand based on their ActorNumber
            Vector2 spawnPoint;

            if (selectedPlayerCount == 2)
            {
                spawnPoint = (actorNumber == PhotonNetwork.LocalPlayer.ActorNumber) ? downSpawnPoint : upSpawnPoint;
            }
            else // For 3 players
            {
                if (actorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
                    spawnPoint = downSpawnPoint;
                else if (playerHands.Count == 1)
                    spawnPoint = middleSpawnPoint1; // Middle position for 2nd player
                else
                    spawnPoint = middleSpawnPoint2; // Upper position for 3rd player
            }

            // Instantiate player hand object at the correct spawn point
            GameObject playerHandObject = PhotonNetwork.Instantiate(playerHandPrefab.name, spawnPoint, Quaternion.identity);
            playerHands[actorNumber] = playerHandObject;

            // If it's the local player, set the player hand for local interaction
            if (actorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                gameManager.SetPlayerHandTransform(playerHandObject.transform); // Set for local player
            }

            Debug.Log($"Player {actorNumber}'s hand instantiated at {(actorNumber == PhotonNetwork.LocalPlayer.ActorNumber ? "bottom" : (playerHands.Count == 1 ? "middle" : "top"))}.");
        }
    }


    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        photonView.RPC("RPC_InstantiatePlayerHand", RpcTarget.AllBuffered, newPlayer.ActorNumber);

        if (PhotonNetwork.IsMasterClient)
        {
            int currentPlayerCount = PhotonNetwork.PlayerList.Length;

            // Check if we have the correct number of players before starting the game
            if (currentPlayerCount == selectedPlayerCount)
            {
                StartCoroutine(DelayRetrieval());
            }
            else
            {
                Debug.Log("Waiting for more players. Current count: " + currentPlayerCount);
            }
        }
    }


    [PunRPC]
    void RPC_NewPlayerJoinedRoom()
    {
        loadingScreen.SetActive(false);
        roomMenuScreen.SetActive(false);
        errorMenu.SetActive(false);
        createRoomMenu.SetActive(false);
        joinRoomMenu.SetActive(false);
    }

    IEnumerator DelayRetrieval()
    {
        yield return new WaitForSeconds(3.0f);
        foreach (var player in PhotonNetwork.PlayerList)
        {
            photonView.RPC("RPC_DealInitialCards", RpcTarget.OthersBuffered, player.ActorNumber);
            photonView.RPC("RPC_NewPlayerJoinedRoom", RpcTarget.All);
        }
    }

    public void CreateNewRoom()
    {
        RoomOptions roomOptions = new RoomOptions { MaxPlayers = (byte)selectedPlayerCount }; // Updated to allow dynamic player count
        currentRoomName = GenerateRoomId();
        PhotonNetwork.CreateRoom(currentRoomName, roomOptions);
    }
    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);
        Debug.Log("Player left the room: " + otherPlayer.NickName);
        gameManager.ReturnToMainMenu();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        base.OnCreateRoomFailed(returnCode, message);
        Debug.LogWarning("Failed to create room: " + message);
        CreateNewRoom();
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        base.OnJoinRoomFailed(returnCode, message);
        Debug.LogWarning("Failed to join room: " + message);
        errorPanel.SetActive(true);
       // CreateNewRoom();
    }

    public void TryJoinExistingRoom()
    {
        if (!roomListUpdated)
        {
            Debug.Log("Room list not updated yet, cannot join existing room.");
            return;
        }

        Debug.Log("Trying to join an existing room. Room list count: " + roomList.Count);
        foreach (RoomInfo room in roomList)
        {
            Debug.Log($"Checking room: {room.Name}, Player Count: {room.PlayerCount}, Max Players: {room.MaxPlayers}");

            // Only join rooms that match the selected player count
            if (room.MaxPlayers == selectedPlayerCount && room.PlayerCount < room.MaxPlayers)
            {
                PhotonNetwork.JoinRoom(room.Name);
                Debug.Log("Joining room: " + room.Name);
                return;
            }
        }

        Debug.Log("No suitable room found, creating a new one.");
        // If no existing room with matching player count is found, create a new room
        CreateNewRoom();
    }


    private string GenerateRoomId()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new System.Random();
        char[] id = new char[roomIdLength];

        for (int i = 0; i < roomIdLength; i++)
        {
            id[i] = chars[random.Next(chars.Length)];
        }

        return new string(id);
    }
    public Transform GetPlayerHandTransform(int actorNumber)
    {
        if (playerHands.ContainsKey(actorNumber))
        {
            Debug.Log($"Player hand found for actor number: {actorNumber}");
            return playerHands[actorNumber].transform;
        }
        else
        {
            Debug.LogWarning($"Player hand not found for actor number: {actorNumber}. Current keys in playerHands: {string.Join(", ", playerHands.Keys)}");
        }
        return null;
    }
}
