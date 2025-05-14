using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.SceneManagement;

public class RoomManager : MonoBehaviourPunCallbacks
{
    #region Singleton
    public static RoomManager Instance;
    private void Awake() => Instance = this;
    #endregion

    #region Public Variables
    public int selectedPlayerCount = 2;
    public GameObject twoPlayersButton;
    public GameObject threePlayersButton;
    public GameObject playerHandPrefab;
    public GameObject roomPanel;
    public GameObject errorPanel;
    public TMP_InputField joinRoomInput;
    public int roomIdLength = 6;
    public static string currentRoomName = "RoomID";
    public bool isMax = false;
    public GameObject settingGameScreen;
    public GameObject loadingScreen, roomMenuScreen, errorMenu, createRoomMenu, joinRoomMenu;
    #endregion

    #region Private Variables
    private string receivedRoomId;
    private Vector2 downSpawnPoint;
    private Vector2 upSpawnPoint;
    private Vector2 middleSpawnPoint1, middleSpawnPoint2;
    private Dictionary<int, GameObject> playerHands = new Dictionary<int, GameObject>();
    private string lastCreatedRoomId;
    private const string RoomIdProperty = "RoomID";
    public bool isConnectedToLobby = false;
    private bool roomListUpdated = false;
    private List<RoomInfo> roomList = new List<RoomInfo>();
    private LoadConnecting loadConnecting;
    private float roomJoinTimeout = 30f; // 30 seconds timeout
    private Coroutine roomJoinTimeoutCoroutine;

    #endregion

    #region Private Methods
    private void Start()
    {
        PhotonNetwork.ConnectUsingSettings();
        downSpawnPoint = new Vector2(0.1f, -3f);
        upSpawnPoint = new Vector2(0.1f, 3f);
        middleSpawnPoint1 = new Vector2(-1f, 3f);
        middleSpawnPoint2 = new Vector2(1f, 3f);
        loadConnecting = FindObjectOfType<LoadConnecting>();
     
          selectedPlayerCount = 2;
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
    #endregion

    #region Public Methods

   

    public void JoinOrCreateRoom(string roomId)
    {
        receivedRoomId = roomId;
        if (isConnectedToLobby)
        {
            Debug.Log("This means it sent and works");
            AttemptJoinOrCreate();
        }
        else
        {
            Debug.Log("Room ID received. Waiting for lobby connection...");
        }
    }

    private void AttemptJoinOrCreate()
    {
        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = (byte)selectedPlayerCount, // Use selected player count
            CustomRoomProperties = new ExitGames.Client.Photon.Hashtable { { RoomIdProperty, receivedRoomId } },
            CustomRoomPropertiesForLobby = new string[] { RoomIdProperty }
        };

        Debug.Log($"Attempting to join or create room: {receivedRoomId}");
        Debug.Log("THis is where it actually joins or creates the room");
        PhotonNetwork.JoinOrCreateRoom(receivedRoomId, roomOptions, TypedLobby.Default);
    }


    public void OnClickTwoPlayers()
    {
        selectedPlayerCount = 2;
        Debug.Log("Selected player count: 2");
    }


    public void OnClickThreePlayers()
    {
        selectedPlayerCount = 3;
        Debug.Log("Selected player count: 3");
    }

    public void OnClickCreate()
    {
        lastCreatedRoomId = GenerateRoomId();
        RoomOptions roomOptions = new RoomOptions { MaxPlayers = (byte)selectedPlayerCount };
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
        //roomMenuScreen.SetActive(true);
        // TryJoinExistingRoom();
       
    }

    public override void OnRoomListUpdate(List<RoomInfo> updatedRoomList)
    {
        base.OnRoomListUpdate(updatedRoomList);
        roomList.Clear();
        roomList.AddRange(updatedRoomList);
        roomListUpdated = true;
        Debug.Log("Room list updated with " + roomList.Count + " rooms.");

        //if (loadConnecting.isPlayRandom)
            //TryJoinExistingRoom();
    }

    public void CheckRoomList()
    {
        TryJoinExistingRoom();
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined room: " + PhotonNetwork.CurrentRoom.Name);

        if (PhotonNetwork.IsMasterClient)
        {
            DeckManager.Instance.InitializeGame();
        }

        photonView.RPC("RPC_InstantiatePlayerHand", RpcTarget.AllBuffered, PhotonNetwork.LocalPlayer.ActorNumber);

        // Start timeout for waiting for players
        if (roomJoinTimeoutCoroutine != null)
        {
            StopCoroutine(roomJoinTimeoutCoroutine);
        }
        roomJoinTimeoutCoroutine = StartCoroutine(RoomJoinTimeout());
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

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        photonView.RPC("RPC_InstantiatePlayerHand", RpcTarget.AllBuffered, newPlayer.ActorNumber);
        loadingScreen.SetActive(false);

        if (PhotonNetwork.IsMasterClient)
        {
            int currentPlayerCount = PhotonNetwork.PlayerList.Length;

            if (currentPlayerCount == selectedPlayerCount)
            {
                StartCoroutine(DelayRetrieval());

                // Stop timeout since game can start
                if (roomJoinTimeoutCoroutine != null)
                {
                    StopCoroutine(roomJoinTimeoutCoroutine);
                    roomJoinTimeoutCoroutine = null;
                }
            }
            else
            {
                Debug.Log("Waiting for more players. Current count: " + currentPlayerCount);
            }
        }
    }
    private IEnumerator RoomJoinTimeout()
    {
        Debug.Log("Waiting for players to join... Timeout set for " + roomJoinTimeout + " seconds.");
        yield return new WaitForSeconds(roomJoinTimeout);

        if (PhotonNetwork.PlayerList.Length < selectedPlayerCount)
        {
            Debug.LogWarning("Timeout: Not enough players joined the room.");
            ShowTimeoutError();
            PhotonNetwork.LeaveRoom();
        }
    }
    private void ShowTimeoutError()
    {
        errorMenu.SetActive(true); // Display error message
        errorMenu.GetComponentInChildren<TMP_Text>().text = "Timeout: Not enough players joined.";
       // Invoke(nameof(ReturnToMainMenu), 3f); // Return after 3 seconds
    }

    public void CreateNewRoom(string roomId)
    {
        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = 2, // Fixed for 2 players
            CustomRoomProperties = new ExitGames.Client.Photon.Hashtable
        {
            { RoomIdProperty, roomId }
        },
            CustomRoomPropertiesForLobby = new[] { RoomIdProperty }
        };

        PhotonNetwork.CreateRoom(roomId, roomOptions);
        Debug.Log($"Creating room with ID: {roomId}");
        loadConnecting.roomIDText.text = roomId;
    }
    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);
        Debug.Log("Player left the room: " + otherPlayer.NickName);
        PhotonNetwork.Disconnect();
         errorPanel.SetActive(true);
        GameManager.Instance.ReturnToMainMenu();
       
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        base.OnDisconnected(cause);
        Debug.Log("Disconnected from Photon: " + cause);

        // Show UI notification for the player
        errorPanel.SetActive(true);
       // errorPanel.GetComponentInChildren<TMP_Text>().text = "Connection lost: " + cause;

        isConnectedToLobby = false;

       // GameManager.Instance.ReturnToMainMenu();
    }


    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        base.OnCreateRoomFailed(returnCode, message);
        Debug.LogWarning("Failed to create room: " + message);
        CreateNewRoom(receivedRoomId);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"Failed to join room {receivedRoomId}: {message}");

        if (returnCode == ErrorCode.GameFull)
        {
            Debug.LogError($"Room {receivedRoomId} is full.");
        }
        else
        {
            Debug.LogError($"Unexpected room join failure: {message}");
        }
    }

    public void MainMenu()
    {
        Time.timeScale = 1f;


        PhotonNetwork.Disconnect();


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

       // CreateNewRoom(receivedRoomId);
    }
    #endregion

    #region PunRPC Methods
    [PunRPC]
    void RPC_InstantiatePlayerHand(int actorNumber)
    {
        if (!playerHands.ContainsKey(actorNumber))
        {

            Vector2 spawnPoint;

            if (selectedPlayerCount == 2)
            {
                spawnPoint = (actorNumber == PhotonNetwork.LocalPlayer.ActorNumber) ? downSpawnPoint : upSpawnPoint;
            }
            else
            {
                if (actorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
                    spawnPoint = downSpawnPoint;
                else if (playerHands.Count == 1)
                    spawnPoint = middleSpawnPoint1;
                else
                    spawnPoint = middleSpawnPoint2;
            }

            GameObject playerHandObject = PhotonNetwork.Instantiate(playerHandPrefab.name, spawnPoint, Quaternion.identity);
            playerHands[actorNumber] = playerHandObject;

            if (actorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                PlayerManager.Instance.SetPlayerHandTransform(playerHandObject.transform);
            }

            Debug.Log($"Player {actorNumber}'s hand instantiated at {(actorNumber == PhotonNetwork.LocalPlayer.ActorNumber ? "bottom" : (playerHands.Count == 1 ? "middle" : "top"))}.");
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
    #endregion
}