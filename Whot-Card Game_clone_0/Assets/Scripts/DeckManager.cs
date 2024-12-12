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
            deck.RemoveAt(0); // Remove the dealt card from the deck so it can't be dealt again

            hand.Add(card);
            card.SetActive(true);
            card.transform.SetParent(handTransform);

            // Synchronize the dealt card with all other players so they are aware of the updated deck and hands
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
        GameObject newCard = Instantiate(cardPrefab, deckTransform);
        Card cardComponent = newCard.GetComponent<Card>();

        // Ensure that the shapeSprites dictionary is set before the card is used
        cardComponent.shapeSprites = GameManager.Instance.shapeSprites.GetShapeSpriteDictionary();

        // Debugging to ensure dictionary is populated


        // Now set the card shape and number
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
        if (!GameManager.Instance.isShapeSelectionActive && TurnManager.Instance.isPlayerTurn && deck.Count > 0)
        {
            photonView.RPC("RPC_DrawCard", RpcTarget.AllBuffered, PhotonNetwork.LocalPlayer.ActorNumber);
        }
    }
    #endregion

    #region PunRPC Methods
    [PunRPC]
    void RPC_GenerateDeck()
    {
        string[] shapes = { "Circle", "Square", "Cross", "Star", "Triangle" }; // Whot excluded initially
        int[] numbers = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14 };

        // Generate the normal cards
        foreach (string shape in shapes)
        {
            foreach (int number in numbers)
            {
                CreateCard(shape, number);
            }
        }

        for (int i = 0; i < 4; i++) // Adjust the number of Whot cards as needed
        {
            CreateCard("Whot", 20); // "Whot" card with a special number (e.g., 20)
        }

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
            CreateCard(shape, number);
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
        if (PhotonNetwork.LocalPlayer.ActorNumber == drawingActorNumber)
        {
            DealCardToPlayer(PlayerManager.Instance.playerHands[drawingActorNumber], PlayerManager.Instance.playerHandTransform, drawingActorNumber);
        }

        GameManager.Instance.CheckForWinCondition();
        TurnManager.Instance.SwitchTurns();
    }
    #endregion
}
