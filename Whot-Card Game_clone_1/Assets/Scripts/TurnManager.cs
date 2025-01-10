using UnityEngine;
using Photon.Pun;
using TMPro;

public class TurnManager : MonoBehaviourPun
{
    #region Singleton
    public static TurnManager Instance;
    #endregion

    #region Public Variables
    public bool isPlayerTurn;
    public TextMeshProUGUI turnText;
    public bool isShapeSelectionPending;
    public bool isBlocking = false;
    public int currentPlayerIndex;  // Track whose turn it is based on player index
    #endregion

    #region Unity Callbacks
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        InitializeTurn();
    }

    private void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        InitializeTurn();  // Reinitialize turn system when a new player joins
    }
    #endregion

    #region Initialization
    public void InitializeTurn()
    {
        int totalPlayers = PhotonNetwork.PlayerList.Length;

        if (totalPlayers > 0)  // Check that players are connected
        {
            // Set the first player (MasterClient) as the starting player
            currentPlayerIndex = 0;
            isPlayerTurn = PhotonNetwork.LocalPlayer.ActorNumber == GetPlayerActorNumber(currentPlayerIndex);
            UpdateTurnText();

            Debug.Log($"Turn Initialized. Total Players: {totalPlayers}, Current Player Index: {currentPlayerIndex}");
        }
        else
        {
            Debug.LogError("No players found. Cannot initialize turns.");
        }
    }
    #endregion

    #region Turn Management
    public void SwitchTurns()
    {
        if (isShapeSelectionPending)
        {
            Debug.Log("Switching turns is blocked because shape selection is pending.");
            return;
        }
        if (!PhotonNetwork.IsMasterClient)
            return;

        int totalPlayers = PhotonNetwork.PlayerList.Length;

        if (totalPlayers > 0)
        {
            currentPlayerIndex = (currentPlayerIndex + 1) % totalPlayers;

            // Update turn state and reset shape selection flag
             isPlayerTurn = PhotonNetwork.LocalPlayer.ActorNumber == GetPlayerActorNumber(currentPlayerIndex);
            //isShapeSelectionPending = false; // Reset the flag
            
            photonView.RPC("RPC_UpdateTurn", RpcTarget.All, currentPlayerIndex);
        }
    }


    public void UpdateTurnText()
    {
        if (isShapeSelectionPending)
        {
            turnText.text = "Shape selection in progress...";
        }
        else
        {
            turnText.text = isPlayerTurn ? "Your Turn" : "Opponent's Turn";
        }

    }
    #endregion

    #region Photon RPCs
    [PunRPC]
    void RPC_UpdateTurn(int newPlayerIndex)
    {
        currentPlayerIndex = newPlayerIndex;
        isPlayerTurn = PhotonNetwork.LocalPlayer.ActorNumber == GetPlayerActorNumber(currentPlayerIndex);
        UpdateTurnText(); // Update the UI to reflect the correct turn

        Debug.Log($"Turn Updated via RPC. Current Player Index: {currentPlayerIndex}");
    }

    #region Photon RPCs
    [PunRPC]
    void RPC_SkipTurn(int opponentActorNumber, bool isGeneralMarketCard = false)
    {
        if (isGeneralMarketCard)
        {
            // Retain the current player's turn
            UpdateTurnText();
            return;
        }

        // Skip turn logic
        if (PhotonNetwork.LocalPlayer.ActorNumber != opponentActorNumber)
        {
            // Update the turn index to the next player
            SwitchTurns();
        }
    }


    [PunRPC]
    void RPC_BlockOpponentActions(bool block)
    {
        GameManager.Instance.isShapeSelectionActive = block;
        if (block)
        {
            isBlocking = true;  
            Debug.Log("Actions are blocked for all players.");
        }
        else
        {
            isBlocking = false; 
            Debug.Log("Actions are unblocked.");
        }
    }
    #endregion


    #region Utility Methods

    // Get the ActorNumber of a player based on their index
    public int GetPlayerActorNumber(int playerIndex)
    {
        if (playerIndex >= 0 && playerIndex < PhotonNetwork.PlayerList.Length)
        {
            return PhotonNetwork.PlayerList[playerIndex].ActorNumber;
        }
        else
        {
            Debug.LogError($"Invalid player index: {playerIndex}");
            return -1; // Return invalid if index is out of bounds
        }
    }

    // Get the ActorNumber of the opponent based on current player's index
    public int GetOpponentActorNumber()
    {
        int totalPlayers = PhotonNetwork.PlayerList.Length;
        int nextPlayerIndex = (currentPlayerIndex + 1) % totalPlayers;
        return GetPlayerActorNumber(nextPlayerIndex); // Get the next player in line
    }
    #endregion

    public bool IsPlayerTurn() => isPlayerTurn;
}
#endregion