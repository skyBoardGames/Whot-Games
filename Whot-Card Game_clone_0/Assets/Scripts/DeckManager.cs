using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;
using System.Linq;


public class DeckManager : MonoBehaviourPun
{
    #region Singleton
    public static DeckManager Instance;
    private void Awake() => Instance = this;
    #endregion

    #region Private Variables
    private List<GameObject> deck = new List<GameObject>();
    #endregion

    #region Public Variables
    public GameObject cardPrefab;
    public Transform deckTransform;
    public Transform discardPileTransform;
    public int startingHandSize = 4;
    public GameObject topCard;
    #endregion

    #region Private Methods
    public void DealCardToPlayer(List<GameObject> hand, Transform handTransform, int actorNumber)
    {
        if (deck.Count > 0)
        {
            GameObject card = deck[0];
            deck.RemoveAt(0);

            // Ensure no duplicates across all hands
            if (PlayerManager.Instance.playerHands.Values.Any(h =>
                h.Any(c => c.GetComponent<Card>().shape == card.GetComponent<Card>().shape &&
                           c.GetComponent<Card>().number == card.GetComponent<Card>().number)))
            {
                Debug.LogWarning("Duplicate card detected across players. Drawing another card.");
                DealCardToPlayer(hand, handTransform, actorNumber);
                return;
            }

            hand.Add(card);
            card.SetActive(true);
            card.transform.SetParent(handTransform);

            // Synchronize hand and deck
            string[] cardData = hand.Select(c => c.GetComponent<Card>().shape + "_" + c.GetComponent<Card>().number).ToArray();
            photonView.RPC("RPC_UpdateHand", RpcTarget.AllBuffered, actorNumber, cardData);
            photonView.RPC("RPC_UpdateDeck", RpcTarget.OthersBuffered, GetDeckCardData());
        }
    }



    // Helper method to get current deck data
    string[] GetDeckCardData()
    {
        return deck.Select(c => c.GetComponent<Card>().shape + "_" + c.GetComponent<Card>().number).ToArray();
    }

    void CreateCard(string shape, int number)
    {
        if (deck.Any(c => c.GetComponent<Card>().shape == shape && c.GetComponent<Card>().number == number))
        {
            Debug.LogWarning($"Duplicate card detected: {shape} {number}. Skipping creation.");
            return;
        }

        GameObject newCard = Instantiate(cardPrefab, deckTransform);
        Card cardComponent = newCard.GetComponent<Card>();
        cardComponent.shapeSprites = GameManager.Instance.shapeSprites.GetShapeSpriteDictionary();
        cardComponent.SetCard(shape, number);

        deck.Add(newCard);
        newCard.SetActive(false);
    }

    #endregion

    #region Public Methods
    public void InitializeGame()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if (deck == null || deck.Count == 0)
            {
                photonView.RPC("RPC_GenerateDeck", RpcTarget.AllBuffered);
                photonView.RPC("RPC_ShuffleDeck", RpcTarget.AllBuffered);
            }
            DisplayTopCardOfDeck(); // Master client displays the top card
        }

        TurnManager.Instance.isPlayerTurn = PhotonNetwork.IsMasterClient;
        TurnManager.Instance.UpdateTurnText();
        GameManager.Instance.shapeSelectionPanel.SetActive(false);
    }
    public void DisplayTopCardOfDeck()
    {
        if (PhotonNetwork.IsMasterClient && topCard == null) // Only allow master to do this and ensure it's done once
        {
            // Ensure there are cards in the deck
            if (deck.Count == 0)
            {
                Debug.LogError("Deck is empty, cannot display top card.");
                return;
            }

            // Loop through the deck until we find a non-Whot card
            while (deck.Count > 0)
            {
                GameObject potentialTopCard = deck[deck.Count - 1]; // Get the top card
                Card cardComponent = potentialTopCard.GetComponent<Card>();

                // Check if the card is not a Whot card
                if (cardComponent.shape != "Whot")
                {
                    // Set the top card
                    potentialTopCard.SetActive(true);
                    potentialTopCard.transform.SetParent(discardPileTransform);
                    potentialTopCard.transform.localPosition = Vector3.zero; // Adjust position to discard pile
                    potentialTopCard.transform.localScale = Vector3.one * 0.4f; // Adjust size for discard pile
                    topCard = potentialTopCard;

                    // Disable collider on the top card
                    topCard.GetComponent<Collider2D>().enabled = false;

                    // Sync the top card's data with all clients
                    photonView.RPC("RPC_SyncTopCard", RpcTarget.OthersBuffered, cardComponent.shape, cardComponent.number);

                    // Remove the card from the deck
                    deck.RemoveAt(deck.Count - 1);
                    break; // Exit the loop when a valid card is found
                }
                else
                {
                    // Move the Whot card to the bottom of the deck
                    deck.RemoveAt(deck.Count - 1);
                    deck.Insert(0, potentialTopCard);
                }
            }

            // If no valid non-Whot card is found, log an error
            if (topCard == null)
            {
                Debug.LogError("No valid non-Whot card found as top card.");
            }
        }
    }

    public void DrawCard()
    {
        if (!TurnManager.Instance.isBlocking)
        {
            if (!GameManager.Instance.isShapeSelectionActive && TurnManager.Instance.isPlayerTurn && deck.Count > 0)
            {
                // Ensure the top card is not drawn
                if (topCard == null || topCard.transform.parent != discardPileTransform)
                {
                    Debug.LogError("Top card is not properly set or misplaced.");
                    return;
                }

                photonView.RPC("RPC_DrawCard", RpcTarget.AllBuffered, PhotonNetwork.LocalPlayer.ActorNumber);
            }
        }
    }

    #endregion

    #region PunRPC Methods
    [PunRPC]
    void RPC_GenerateDeck()
    {
        deck.Clear(); // Clear the deck before regenerating it
        Dictionary<string, int[]> deckStructure = new Dictionary<string, int[]>
    {
        { "Star", new int[] { 1, 2, 3, 4, 5, 7, 8 } },
        { "Cross", new int[] { 1, 2, 3, 5, 7, 10, 11, 13, 14 } },
        { "Circle", new int[] { 1, 2, 3, 4, 5, 7, 8, 10, 11, 12, 13, 14 } },
        { "Triangle", new int[] { 1, 2, 3, 4, 5, 7, 8, 10, 11, 12, 13, 14 } },
        { "Square", new int[] { 1, 2, 3, 5, 7, 10, 11, 13, 14 } }
    };

        foreach (var shape in deckStructure.Keys)
        {
            foreach (var number in deckStructure[shape])
            {
                CreateCard(shape, number);
            }
        }

        for (int i = 0; i < 4; i++) // Add Whot cards
        {
            CreateCard("Whot", 20);
        }

        Debug.Log($"Deck initialized with {deck.Count} cards.");
    }


    [PunRPC]
    void RPC_UpdateDeck(string[] deckData)
    {
        deck.Clear();
        foreach (var cardString in deckData)
        {
            var splitData = cardString.Split('_');
            string shape = splitData[0];
            int number = int.Parse(splitData[1]);

            // Recreate the deck with the remaining cards
            if (!deck.Any(c => c.GetComponent<Card>().shape == shape && c.GetComponent<Card>().number == number))
            {
                CreateCard(shape, number);
            }
        }
    }


    [PunRPC]
    void RPC_DealInitialCards(int actorNumber)
    {
        if (!PlayerManager.Instance.playerHands.ContainsKey(actorNumber))
        {
            PlayerManager.Instance.playerHands[actorNumber] = new List<GameObject>();
        }

        Transform handTransform = RoomManager.Instance.GetPlayerHandTransform(actorNumber);
        if (handTransform != null)
        {
            for (int i = 0; i < startingHandSize; i++)
            {
                DealCardToPlayer(PlayerManager.Instance.playerHands[actorNumber], handTransform, actorNumber);
            }
        }
        else
        {
            Debug.LogWarning($"Hand transform for player {actorNumber} is null.");
        }

        if (PhotonNetwork.LocalPlayer.ActorNumber != actorNumber)
        {
            Transform opponentHandTransform = RoomManager.Instance.GetPlayerHandTransform(actorNumber);
            PlayerManager.Instance.UpdateHandLayout(PlayerManager.Instance.playerHands[actorNumber], opponentHandTransform);
        }
    }


    [PunRPC]
    void RPC_ShuffleDeck()
    {
        for (int i = 0; i < deck.Count; i++)
        {
            GameObject temp = deck[i];
            int randomIndex = Random.Range(i, deck.Count);
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temp;
        }
    }

    [PunRPC]
    void RPC_SyncTopCard(string shape, int number)
    {
        Debug.Log($"Syncing top card: {shape} {number}");

        if (topCard == null) // Only create the top card if it's not already created
        {
            GameObject newTopCard = Instantiate(cardPrefab, discardPileTransform);
            Card card = newTopCard.GetComponent<Card>();
            card.shapeSprites = GameManager.Instance.shapeSprites.GetShapeSpriteDictionary();
            newTopCard.GetComponent<Card>().SetCard(shape, number);
            newTopCard.SetActive(true);
            newTopCard.transform.localPosition = Vector3.zero; // Adjust position to discard pile
            newTopCard.transform.localScale = Vector3.one * 0.4f; // Adjust size for discard pile
            topCard = newTopCard;

            // Disable collider on the top card
            topCard.GetComponent<Collider2D>().enabled = false;
        }
        else
        {
            topCard.GetComponent<Card>().SetCard(shape, number);

            topCard.transform.SetParent(discardPileTransform);
            topCard.transform.localPosition = Vector3.zero;
            topCard.transform.localScale = Vector3.one * 0.4f;
            topCard.SetActive(true);
        }
    }

    [PunRPC]
    void RPC_DrawCard(int drawingActorNumber)
    {
        if (deck.Count == 0)
        {
            Debug.LogWarning("Deck is empty. No card to draw.");
            return;
        }

        if (PhotonNetwork.LocalPlayer.ActorNumber == drawingActorNumber)
        {
            DealCardToPlayer(PlayerManager.Instance.playerHands[drawingActorNumber], PlayerManager.Instance.playerHandTransform, drawingActorNumber);
        }

        GameManager.Instance.CheckForWinCondition();
        TurnManager.Instance.SwitchTurns();
    }
    #endregion
}