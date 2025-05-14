using SocketIOClient;
using SocketIOClient.Newtonsoft.Json;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Newtonsoft.Json.Linq;
using Photon.Pun;
using TMPro;


public class SocketManager : MonoBehaviour
{
    public static SocketManager Instance { get; private set; }
    private SocketIOUnity socket;
    private bool isConnected = false;
    InitGameData roomData;
    public static string lobbyCode;
     public TextMeshProUGUI lobbyCodeText;
   
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
            InitializeSocket();
        }
    }

    void InitializeSocket()
    {
        var uri = new Uri("wss://game-service-uny2.onrender.com"); // Changed to wss://
        socket = new SocketIOUnity(uri, new SocketIOOptions
        {
            Query = new System.Collections.Generic.Dictionary<string, string>
            {
                {"token", "UNITY"}
            },
            Transport = SocketIOClient.Transport.TransportProtocol.WebSocket,
            Reconnection = true,
            ExtraHeaders = new System.Collections.Generic.Dictionary<string, string>
            {
                {"Origin", "https://your-unity-client-origin.com"}
            }
        });

        socket.JsonSerializer = new NewtonsoftJsonSerializer();

        // Add connection status handlers
        socket.OnConnected += (sender, e) =>
        {
            isConnected = true;
            Debug.Log("Socket.IO Connected!");
            // Send initial message after connection
            SendPhotonRoomId("connection-test");
        };

        socket.OnDisconnected += (sender, e) =>
        {
            isConnected = false;
            Debug.Log("Socket.IO Disconnected!");
        };

        socket.OnError += (sender, e) =>
        {
            Debug.LogError($"Socket.IO Error: {e}");
        };

        // Main message handler
        socket.On("game-message-channel", response =>
        {
            Debug.Log("Received a message on game-message-channel");
            try
            {
                Debug.Log($"Raw Response: {response.ToString()}");
                if (response.Count < 2)
                {
                    Debug.LogError("Invalid response format, expected at least two parameters.");
                    return;
                }
                string messageName = response.GetValue<string>(0);
                JToken messageData = response.GetValue<JToken>(1);

                Debug.Log($"Parsed Message Name: {messageName}");
                Debug.Log($"Parsed Message Data: {messageData}");

                switch (messageName)
                {
                    case "hi":
                        HandleWelcomeMessage(messageData);
                        break;
                    case "init-game":
                        HandleInitGameMessage(messageData);
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Message handling error: {ex}");
            }
        });

        // Start connection
        socket.Connect();
    }
    private void HandleWelcomeMessage(JToken messageData)
    {
        try
        {
            TestSocket data = messageData.ToObject<TestSocket>();
            Debug.Log($"Welcome message: {data.text}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing welcome message: {e.Message}");
        }
    }

    private void HandlePhotonId(JToken messageData)
    {
        try
        {
            //string photonId = messageData.ToObject<string>();
            Debug.Log($"Received Photon ID: {roomData.lobbyCode}");
            //RoomManager.Instance.JoinOrCreateRoom(roomData.lobbyCode);
            // Handle Photon ID logic here
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing Photon ID: {e.Message}");
        }
    }

    private void HandleInitGameMessage(JToken messageData)
    {
        Debug.Log($"Received init-game event. Raw Data: {messageData.ToString()}");
        try
        {
            roomData = messageData.ToObject<InitGameData>();

            Debug.Log($"Received Init Game Data: Game ID: {roomData.gameId}, " +
                      $"Player ID: {roomData.playerId}, Opponent ID: {roomData.opponentId}, " +
                      $"Stake Amount: {roomData.stakeAmount}, Tournament ID: {roomData.tournamentId}, " +
                      $"Game Name: {roomData.gameName}");
            HandlePhotonId(messageData);
            // You can now use these values in your game logic.
            // Example: StartGame(roomData);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error handling Init Game Message: {e.Message}");
        }
    }


    public void SetLobbyCode(string code)
    {
        Debug.Log("Received Lobby Code from Browser: " + code);
        Debug.Log("This is a test debug that shows the lobby code is being sent from browser");
        roomData.lobbyCode = code;
        lobbyCodeText.text = "LobbyID: " + code;
       
        StartCoroutine(WaitLobbyID(code));
    }

  

    IEnumerator WaitLobbyID(string lobbyID)
    {
        Debug.Log("This is before the delay");
        yield return new WaitUntil(() => RoomManager.Instance.isConnectedToLobby);

        yield return new WaitForSeconds(1f);

        Debug.Log("I sent the roommanager the lobbyid after a long 10 seconds delay");
        RoomManager.Instance.JoinOrCreateRoom(lobbyID);
    }

    private void Update()
    {

        // SendPhotonRoomId(PhotonNetwork.CurrentRoom.Name);

    }

    public void SendPhotonRoomId(string roomId)
    {
        if (!isConnected)
        {
            Debug.LogWarning("Trying to send message before connection!");
            return;
        }

        try
        {
            // Emit with acknowledgement handler
            socket.Emit("game-message-channel", response =>
            {
                // This handles server acknowledgement
                Debug.Log($"Server acknowledged photon-id: {response.GetValue<string>()}");
            }, "photon-id", roomId);

            Debug.Log($"Sent Photon ID: {roomId}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Emission error: {e}");
        }
    }

    void OnDestroy()
    {
        if (socket != null && socket.Connected)
        {
            socket.Disconnect();
        }
    }
}

[System.Serializable]
public class TestSocket
{
    public string text;
}

[System.Serializable]
public class InitGameData
{
    public string gameId;
    public string playerId;
    public string opponentId;
    public string stakeAmount;
    public string tournamentId;
    public string gameName;
    public string lobbyCode;
}