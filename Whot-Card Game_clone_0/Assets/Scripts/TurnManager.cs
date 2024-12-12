using UnityEngine;
using Photon.Pun;
using TMPro;

public class TurnManager : MonoBehaviour
{
    #region Singleton
    public static TurnManager Instance;
    #endregion

    #region Public Variables
    public bool isPlayerTurn;
    public TextMeshProUGUI turnText;
    #endregion

    #region Unity Callbacks
    private void Awake()
    {
        Instance = this;
    }
    #endregion

    #region Initialization
    public void InitializeTurn()
    {
        isPlayerTurn = PhotonNetwork.IsMasterClient;
        UpdateTurnText();
    }
    #endregion

    #region Turn Management
    public void SwitchTurns()
    {
        isPlayerTurn = !isPlayerTurn;
        UpdateTurnText();
    }

    public void UpdateTurnText()
    {
       turnText.text = isPlayerTurn ? "Your Turn" : "Opponent's Turn";
    }
    #endregion

    #region Photon RPCs
    [PunRPC]
    void RPC_SkipTurn(int currentPlayerActorNumber)
    {
        // Ensure it's still the current player's turn
        isPlayerTurn = PhotonNetwork.LocalPlayer.ActorNumber == currentPlayerActorNumber;
        UpdateTurnText(); // Update the UI to reflect the correct turn
    }

    [PunRPC]
    void RPC_BlockOpponentActions(bool block)
    {
        GameManager.Instance.isShapeSelectionActive = block;
    }
    #endregion

    #region Utility Methods
    public int GetOpponentActorNumber()
    {
        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (player.ActorNumber != PhotonNetwork.LocalPlayer.ActorNumber)
            {
                return player.ActorNumber; // Return the opponent's ActorNumber
            }
        }
        return -1; // Return invalid if no opponent found (shouldn't happen in a 2-player game)
    }
    #endregion
    public bool IsPlayerTurn() => isPlayerTurn;
}
