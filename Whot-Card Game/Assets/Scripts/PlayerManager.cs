using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;
using System.Linq;

public class PlayerManager : MonoBehaviourPun
{
    #region Singleton
    public static PlayerManager Instance;
    private void Awake() => Instance = this;
    #endregion

    #region Public Variables
    public Dictionary<int, List<GameObject>> playerHands = new Dictionary<int, List<GameObject>>();
    public Transform playerHandTransform;
    #endregion

    #region Private Variables
    private Vector3 initialTouchPosition;
    private float scrollSpeed = 2.5f;
    private float minScrollPosition = -0.3f;
    private float maxScrollPosition = 0.3f;
    #endregion

    #region Unity Methods
    private void Start()
    {
      UpdateHandLayout(playerHands[PhotonNetwork.LocalPlayer.ActorNumber], playerHandTransform);
    }

    private void Update()
    {
        HandleSwipeScrolling();
    }
    #endregion

    #region Private Methods
    void RemoveCardFromHand(Card card)
    {
     
        GameObject cardObject = card.gameObject;
        if (playerHands[PhotonNetwork.LocalPlayer.ActorNumber].Contains(cardObject))
        {
            playerHands[PhotonNetwork.LocalPlayer.ActorNumber].Remove(cardObject);

            if (cardObject != DeckManager.Instance.topCard)
            {
                Destroy(cardObject);
            }

            UpdateHandLayout(playerHands[PhotonNetwork.LocalPlayer.ActorNumber], playerHandTransform);
            GameManager.Instance.CheckForWinCondition();
        }
    }


    #endregion

    #region Public Methods
    public void SetPlayerHandTransform(Transform handTransform)
    {
        playerHandTransform = handTransform;
    }

    public void HandleSwipeScrolling()
    {
        // Ensure the player's hand exists before accessing it
        if (!playerHands.ContainsKey(PhotonNetwork.LocalPlayer.ActorNumber))
        {
           // Debug.LogWarning($"Player hand not found for actor {PhotonNetwork.LocalPlayer.ActorNumber}. Aborting swipe handling.");
            return;
        }

        // Get the local player's hand and the card positions
        List<GameObject> hand = playerHands[PhotonNetwork.LocalPlayer.ActorNumber];
        if (hand.Count == 0) return; // No cards to scroll

        // Calculate the visible bounds of the screen
        float halfScreenWidth = Camera.main.orthographicSize * Screen.width / Screen.height;

        // Find the leftmost and rightmost card positions in the hand
        float leftmostCardX = hand.First().transform.localPosition.x;
        float rightmostCardX = hand.Last().transform.localPosition.x;

        // Adjust the scroll limits based on the card positions
        minScrollPosition = Mathf.Min(leftmostCardX - halfScreenWidth, 0);
        maxScrollPosition = Mathf.Max(rightmostCardX + halfScreenWidth, 0);

        // Handle swipe input
        if (Input.GetMouseButtonDown(0))
        {
            initialTouchPosition = Input.mousePosition;
        }
        else if (Input.GetMouseButton(0))
        {
            Vector3 currentTouchPosition = Input.mousePosition;
            float swipeDelta = (currentTouchPosition.x - initialTouchPosition.x) * scrollSpeed * Time.deltaTime;

            // Move the hand transform within the clamped bounds
            Vector3 newPosition = playerHandTransform.localPosition + new Vector3(swipeDelta, 0, 0);
            newPosition.x = Mathf.Clamp(newPosition.x, minScrollPosition, maxScrollPosition);

            // Apply the new position
            playerHandTransform.localPosition = Vector3.Lerp(playerHandTransform.localPosition, newPosition, 0.1f);

            initialTouchPosition = currentTouchPosition;
        }
    }


    public void PlayCard(Card card)
    {
        Debug.Log($"Additional check > PlayCard: {card.shape} {card.number} invoked by {PhotonNetwork.LocalPlayer.ActorNumber}");
        if (TurnManager.Instance.isPlayerTurn && CanPlayCard(card))
        {

            switch (card.shape)
            {
                case "Whot":
                    if (GameManager.Instance.isShapeSelectionActive)
                    {
                        Debug.LogWarning("Whot card logic already in progress.");
                        return; // Prevent duplicate logic
                    }
                    GameManager.Instance.isShapeSelectionActive = true;
                    GameManager.Instance.shapeSelectionPanel.SetActive(true);
                    TurnManager.Instance.isShapeSelectionPending = true;
                    TurnManager.Instance.isPlayerTurn = false;
                    int nextPlayerActorNumber = TurnManager.Instance.GetOpponentActorNumber();
                   // photonView.RPC("RPC_ShapeSelected", RpcTarget.All, card.shape, PhotonNetwork.LocalPlayer.ActorNumber);
                    photonView.RPC("RPC_BlockOpponentActions", RpcTarget.OthersBuffered, true);
                    TurnManager.Instance.UpdateTurnText();
                    break;

                default:
                    switch (card.number)
                    {
                        case 2:
                            int opponentActorNumber2 = TurnManager.Instance.GetOpponentActorNumber(); // Get the opponent's ActorNumber
                            photonView.RPC("RPC_PickTwoEffect", RpcTarget.All, opponentActorNumber2); // Target the specific opponent

                            photonView.RPC("RPC_SkipTurn", RpcTarget.AllBuffered, opponentActorNumber2,false);
                            break;

                        case 14:
                            int currentPlayerActorNumber = TurnManager.Instance.GetPlayerActorNumber(TurnManager.Instance.currentPlayerIndex);
                            photonView.RPC("RPC_GeneralMarketEffect", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber);// Pass the actor number of the player who played the card
                            photonView.RPC("RPC_SkipTurn", RpcTarget.AllBuffered, currentPlayerActorNumber, true); // Retain current turn
                            break;

                        case 1:
                            int opponentActorNumber1 = TurnManager.Instance.GetOpponentActorNumber();
                            photonView.RPC("RPC_SkipTurn", RpcTarget.AllBuffered, opponentActorNumber1, false);
                            break;
                        default:
                            // Handle non-special cards if needed
                            break;
                    }
                    break;
            }

            photonView.RPC("RPC_PlayCard", RpcTarget.AllBuffered, card.shape, card.number, PhotonNetwork.LocalPlayer.ActorNumber);
            if (GameManager.Instance.requestedShape != null && card.shape == GameManager.Instance.requestedShape)
            {
                GameManager.Instance.requestedShape = null;
            }
            RemoveCardFromHand(card);
            photonView.RPC("RPC_UpdateHand", RpcTarget.Others, PhotonNetwork.LocalPlayer.ActorNumber, playerHands[PhotonNetwork.LocalPlayer.ActorNumber].Select(c => c.GetComponent<Card>().shape + "_" + c.GetComponent<Card>().number).ToArray());
        }
    }

    public bool CanPlayCard(Card card)
    {
        // Allow playing a Whot card at any time
        if (card.shape == "Whot")
        {
            return true;
        }

        // Ensure the top card exists
        if (DeckManager.Instance.topCard.GetComponent<Card>() == null)
        {
            Debug.LogWarning("Top card component is null.");
            return false;
        }

        Card topCardComponent = DeckManager.Instance.topCard.GetComponent<Card>();

        // Check for requested shape if active
        if (GameManager.Instance.requestedShape != null)
        {
            return card.shape == GameManager.Instance.requestedShape;
        }

        // Normal check for matching shape or number
        return card.shape == topCardComponent.shape || card.number == topCardComponent.number;
    }



    public void UpdateHandLayout(List<GameObject> hand, Transform handTransform)
    {
        if (handTransform == playerHandTransform)
        {
            float cardSpacing = 1.3f;
            float startPositionX = -((hand.Count - 1) * cardSpacing) / 2f; // Center the hand

            for (int i = 0; i < hand.Count; i++)
            {
                GameObject card = hand[i];
                card.transform.localPosition = new Vector3(startPositionX + i * cardSpacing, 0, 0);
                card.GetComponent<Card>().ShowFront();
            }

            // Reset scroll limits
            HandleSwipeScrolling();
        }
        else
        {
            float stackOffset = 0.05f;

            for (int i = 0; i < hand.Count; i++)
            {
                GameObject card = hand[i];
                card.transform.localPosition = new Vector3(0, i * stackOffset, 0);
                card.GetComponent<Card>().ShowBack();
            }
        }
    }

    #endregion

    #region PunRPC Methods
    [PunRPC]
    void RPC_UpdateHand(int actorNumber, string[] cardData)
    {
        // Clear and recreate the player's hand from the card data
        if (!playerHands.ContainsKey(actorNumber))
        {
            playerHands[actorNumber] = new List<GameObject>();
        }

        List<GameObject> hand = playerHands[actorNumber];

        // Clear the existing hand
        foreach (var card in hand)
        {
            Destroy(card);
        }
        hand.Clear();

        foreach (var cardString in cardData)
        {
            var splitData = cardString.Split('_');
            string shape = splitData[0];
            int number = int.Parse(splitData[1]);

            GameObject newCard = Instantiate(DeckManager.Instance.cardPrefab);
            Card cardComponent = newCard.GetComponent<Card>();
            cardComponent.shapeSprites = GameManager.Instance.shapeSprites.GetShapeSpriteDictionary();
            newCard.GetComponent<Card>().SetCard(shape, number);
            hand.Add(newCard);

            newCard.transform.SetParent(RoomManager.Instance.GetPlayerHandTransform(actorNumber));
        }

        // Update hand layout and visibility (local player sees front, opponents see back)
        Transform handTransform = RoomManager.Instance.GetPlayerHandTransform(actorNumber);
        UpdateHandLayout(hand, handTransform);
      
    }

    [PunRPC]
    void RPC_RemoveCardFromHand(int actorNumber, string shape, int number)
    {
        GameObject cardToRemove = playerHands[actorNumber].Find(c => c.GetComponent<Card>().shape == shape && c.GetComponent<Card>().number == number);
        if (cardToRemove != null)
        {
            playerHands[actorNumber].Remove(cardToRemove); 
            Destroy(cardToRemove); 
        }

        // Update the hand layout after removing the card
        Transform handTransform = RoomManager.Instance.GetPlayerHandTransform(actorNumber);
        UpdateHandLayout(playerHands[actorNumber], handTransform);

        GameManager.Instance.CheckForWinCondition();
    }

    [PunRPC]
    void RPC_PlayCard(string shape, int number, int actorNumber)
    {

     
        // Deactivate the previous top card if it exists
        if (DeckManager.Instance.topCard != null)
        {
            DeckManager.Instance.topCard.SetActive(false); // Deactivate the old top card to avoid overlap
            DeckManager.Instance.topCard.GetComponent<Collider2D>().enabled = false; // Ensure the collider is disabled
        }

       
        GameObject playedCard = playerHands[PhotonNetwork.LocalPlayer.ActorNumber].Find(
            c => c.GetComponent<Card>().shape == shape && c.GetComponent<Card>().number == number
        );
      

        if (playedCard != null)
        {
           
            playedCard.transform.SetParent(DeckManager.Instance.discardPileTransform);
            playedCard.transform.localPosition = Vector3.zero; // Position the card in the discard pile
            playedCard.transform.localScale = Vector3.one * 0.4f; // Adjust the size for the discard pile
            playedCard.SetActive(true);
            DeckManager.Instance.topCard = playedCard; // Update the top card reference

            photonView.RPC("RPC_SyncTopCard", RpcTarget.AllBuffered, shape, number);
            photonView.RPC("RPC_RemoveCardFromHand", RpcTarget.OthersBuffered, actorNumber, shape, number);
        }

        Debug.Log($"Card Played: {shape} {number}");
        if (number != 14)
        {
            TurnManager.Instance.SwitchTurns();
        }
    }
}
#endregion
