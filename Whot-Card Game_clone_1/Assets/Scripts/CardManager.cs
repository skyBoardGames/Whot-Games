using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;


public class CardManager : MonoBehaviourPun
{
    #region Singleton
    public static CardManager Instance;
    private void Awake() => Instance = this;
    #endregion

    #region Public Methods
    public void OnShapeSelected(string selectedShape)
    {
        // Update GameManager state
        GameManager.Instance.shapeSelectionPanel.SetActive(false);
        GameManager.Instance.isShapeSelectionActive = false;
        GameManager.Instance.requestedShape = selectedShape;

        // Sync with other players
        photonView.RPC("RPC_ShapeSelected", RpcTarget.AllBuffered, selectedShape, PhotonNetwork.LocalPlayer.ActorNumber);

        // Reset shape selection and ensure the player retains their turn
        TurnManager.Instance.isShapeSelectionPending = false;
       // TurnManager.Instance.isPlayerTurn = true;

        // Update UI
        TurnManager.Instance.UpdateTurnText();
    }


    #endregion

    #region PunRPC Methods
    [PunRPC]
    void RPC_PickTwoEffect(int targetPlayerActorNumber)
    {
        if (PhotonNetwork.LocalPlayer.ActorNumber == targetPlayerActorNumber) // Only apply to the player with the matching actor number
        {
            // The target player draws two cards
            for (int i = 0; i < 2; i++)
            {
                DeckManager.Instance.DealCardToPlayer(PlayerManager.Instance.playerHands[targetPlayerActorNumber], RoomManager.Instance.GetPlayerHandTransform(targetPlayerActorNumber), targetPlayerActorNumber);
            }
        }
    }

    [PunRPC]
    public void RPC_GeneralMarketEffect(int playerWhoPlayedCardActorNumber)
    {
        int localPlayerActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;

        // Skip the player who played the card
        if (localPlayerActorNumber != playerWhoPlayedCardActorNumber)
        {
            // Deal one card to the player
            DeckManager.Instance.DealCardToPlayer(
                PlayerManager.Instance.playerHands[localPlayerActorNumber],
                RoomManager.Instance.GetPlayerHandTransform(localPlayerActorNumber),
                localPlayerActorNumber
            );

        }
    }
    [PunRPC]
    void RPC_ShapeSelected(string selectedShape, int actorNumber)
    {
        GameManager.Instance.requestedShape = selectedShape;

        // Update the UI only for the player whose turn is active
        int currentPlayerActorNumber = TurnManager.Instance.GetPlayerActorNumber(TurnManager.Instance.currentPlayerIndex);
        if (PhotonNetwork.LocalPlayer.ActorNumber == currentPlayerActorNumber)
        {
            TurnManager.Instance.turnText.text = $"Opponent selected: {selectedShape}. Provide or draw!";
            TurnManager.Instance.isPlayerTurn = true;
        }

       // GameManager.Instance.isShapeSelectionActive = false;
      //  TurnManager.Instance.isShapeSelectionPending = false;
        // Ensure only the next player is blocked
      //  GameManager.Instance.isShapeSelectionActive = PhotonNetwork.LocalPlayer.ActorNumber == currentPlayerActorNumber;

        // Reset blocking actions for others
        photonView.RPC("RPC_BlockOpponentActions", RpcTarget.OthersBuffered, false);
    }


    #endregion
}
