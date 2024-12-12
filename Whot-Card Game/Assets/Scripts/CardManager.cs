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
        // Hide the shape selection panel
        GameManager.Instance.shapeSelectionPanel.SetActive(false);

        // Send the selected shape to other players
        photonView.RPC("RPC_ShapeSelected", RpcTarget.AllBuffered, selectedShape, PhotonNetwork.LocalPlayer.ActorNumber);

        // Allow the player to proceed with their turn after selecting the shape
        TurnManager.Instance.isPlayerTurn = true;

        // Update the requestedShape variable
        GameManager.Instance.requestedShape = selectedShape;

        // You may need to update the UI for the local player as well to indicate the selected shape


        TurnManager.Instance.SwitchTurns();
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
    public void RPC_GeneralMarketEffect(int targetPlayerActorNumber)
    {
        if (PhotonNetwork.LocalPlayer.ActorNumber == targetPlayerActorNumber) // Only apply to the player with the matching actor number
        {
            DeckManager.Instance.DealCardToPlayer(PlayerManager.Instance.playerHands[targetPlayerActorNumber], RoomManager.Instance.GetPlayerHandTransform(targetPlayerActorNumber), targetPlayerActorNumber);
        }

    }

    [PunRPC]
    void RPC_ShapeSelected(string selectedShape, int actorNumber)
    {
        // Update the requested shape in the game
        GameManager.Instance.requestedShape = selectedShape;
        photonView.RPC("RPC_BlockOpponentActions", RpcTarget.OthersBuffered, false);
        // Inform other players of the selected shape
        if (PhotonNetwork.LocalPlayer.ActorNumber != actorNumber)
        {
            // Display a message to the opponent
            TurnManager.Instance.turnText.text = $"Opponent selected: {selectedShape}. Provide or draw!";
        }
    }
    #endregion
}
