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
    private float scrollSpeed = 1.5f;
    private float minScrollPosition = -0.1f;
    private float maxScrollPosition = 0.1f;
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
            playerHands[PhotonNetwork.LocalPlayer.ActorNumber].Remove(cardObject); // Remove from player's hand


            if (cardObject != DeckManager.Instance.topCard)
            {
                Destroy(cardObject);
            }

            // Update the hand layout to remove any gaps left by the removed card
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
            Debug.LogWarning($"Player hand not found for actor {PhotonNetwork.LocalPlayer.ActorNumber}. Aborting swipe handling.");
            return; 
        }

        int cardCount = playerHands[PhotonNetwork.LocalPlayer.ActorNumber].Count; 
        float cardWidth = 1.0f; 
        float totalHandWidth = cardCount * cardWidth; 

        
        float baseMinScrollPosition = -.1f; 
        float baseMaxScrollPosition = .1f; 

        minScrollPosition = baseMinScrollPosition - (totalHandWidth / 2f);
        maxScrollPosition = baseMaxScrollPosition + (totalHandWidth / 2f);

        if (Input.GetMouseButtonDown(0))
        {
            // Capture initial touch position
            initialTouchPosition = Input.mousePosition;
        }
        else if (Input.GetMouseButton(0))
        {
           
            Vector3 currentTouchPosition = Input.mousePosition;
            float swipeDelta = currentTouchPosition.x - initialTouchPosition.x;

          
            Vector3 newPosition = playerHandTransform.localPosition + new Vector3(swipeDelta * scrollSpeed * Time.deltaTime, 0, 0);
            newPosition.x = Mathf.Clamp(newPosition.x, minScrollPosition, maxScrollPosition);

            
            playerHandTransform.localPosition = Vector3.Lerp(playerHandTransform.localPosition, newPosition, 0.1f); // Adjust the third parameter for speed of smoothing

           
            initialTouchPosition = currentTouchPosition;
        }
    }
    public void PlayCard(Card card)
    {
        if (TurnManager.Instance.isPlayerTurn && CanPlayCard(card))
        {

            photonView.RPC("RPC_PlayCard", RpcTarget.AllBuffered, card.shape, card.number, PhotonNetwork.LocalPlayer.ActorNumber);

            if (card.shape == "Whot")
            {
                GameManager.Instance.isShapeSelectionActive = true;
                GameManager.Instance.shapeSelectionPanel.SetActive(true);

                TurnManager.Instance.isPlayerTurn = false;
                photonView.RPC("RPC_BlockOpponentActions", RpcTarget.OthersBuffered, true);
                return;
            }
            // Check for special cards and apply effects
            else if (card.number == 2)
            {
                int opponentActorNumber = TurnManager.Instance.GetOpponentActorNumber(); // Get the opponent's ActorNumber
                photonView.RPC("RPC_PickTwoEffect", RpcTarget.All, opponentActorNumber); // Target the specific opponent


                photonView.RPC("RPC_SkipTurn", RpcTarget.AllBuffered, PhotonNetwork.LocalPlayer.ActorNumber);
            }
            else if (card.number == 14)
            {
                int opponentActorNumber = TurnManager.Instance.GetOpponentActorNumber();
                photonView.RPC("RPC_GeneralMarketEffect", RpcTarget.All, opponentActorNumber); // Apply to all players

                photonView.RPC("RPC_SkipTurn", RpcTarget.AllBuffered, PhotonNetwork.LocalPlayer.ActorNumber);
            }


            RemoveCardFromHand(card);
            photonView.RPC("RPC_UpdateHand", RpcTarget.Others, PhotonNetwork.LocalPlayer.ActorNumber, playerHands[PhotonNetwork.LocalPlayer.ActorNumber].Select(c => c.GetComponent<Card>().shape + "_" + c.GetComponent<Card>().number).ToArray());
        }
    }

    public bool CanPlayCard(Card card)
    {

        if (card.shape == "Whot")
        {
            return true;
        }
        if (DeckManager.Instance.topCard.GetComponent<Card>().shape == "Whot")
        {
            return card.shape == GameManager.Instance.requestedShape;
        }
        // Logic for normal cards (check shape and number)
        if (DeckManager.Instance.topCard != null)
        {
            Card topCardComponent = DeckManager.Instance.topCard.GetComponent<Card>();
            return card.shape == topCardComponent.shape || card.number == topCardComponent.number; // Allow if shapes or numbers match
        }

        return false;
    }
    public void UpdateHandLayout(List<GameObject> hand, Transform handTransform)
    {
        if (handTransform == playerHandTransform) 
        {
            float cardSpacing = 1.5f;
            float startPositionX = -2.45f;

            for (int i = 0; i < hand.Count; i++)
            {
                GameObject card = hand[i];
                card.transform.localPosition = new Vector3(startPositionX + i * cardSpacing, 0, 0); // Reposition the card
                card.GetComponent<Card>().ShowFront(); 
            }
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
        Transform handTransform =   RoomManager.Instance.GetPlayerHandTransform(actorNumber);
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
        TurnManager.Instance.SwitchTurns();
    }
}
#endregion
