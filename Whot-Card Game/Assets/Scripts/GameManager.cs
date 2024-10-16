using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.SceneManagement;

using UnityEngine.XR;
public class GameManager : MonoBehaviourPunCallbacks
{
    public GameObject cardPrefab;
    public Transform deckTransform;
    public Transform discardPileTransform;
    public Transform playerHandTransform; // Transform for local player's hand
    public ShapeSpriteSO shapeSprites;
    public Sprite cardBackSprite;
    public TextMeshProUGUI turnText;
    public GameObject shapeSelectionPanel;
    public Button[] shapeButtons;
    public GameObject winPanel;
    public GameObject losePanel;

    private List<GameObject> deck = new List<GameObject>();
    private Dictionary<int, List<GameObject>> playerHands = new Dictionary<int, List<GameObject>>();
    private GameObject topCard;
    private bool isPlayerTurn;
    public string requestedShape;
    private int startingHandSize = 4;
    RoomManager roomManager;
    public bool isShapeSelectionActive = false;

    // For scrolling/swiping
    private float scrollSpeed = 1.5f; // Speed of scrolling/swiping
    private float minScrollPosition = -.1f; // Minimum scroll position (left-most)
    private float maxScrollPosition = .1f; // Maximum scroll position (right-most)
    private Vector3 initialTouchPosition;
    private int visibleCardStartIndex = 0;
    void Start()
    {
        roomManager = FindObjectOfType<RoomManager>();
        minScrollPosition = -.1f; // Adjust as needed
        maxScrollPosition = .1f; // Adjust as needed
        UpdateHandLayout(playerHands[PhotonNetwork.LocalPlayer.ActorNumber], playerHandTransform);
    }

    void Update()
    {
        HandleSwipeScrolling();
    }
    public void SetPlayerHandTransform(Transform handTransform)
    {
        playerHandTransform = handTransform;
    }

    void HandleSwipeScrolling()
    {
        // Ensure the player's hand exists before accessing it
        if (!playerHands.ContainsKey(PhotonNetwork.LocalPlayer.ActorNumber))
        {
            Debug.LogWarning($"Player hand not found for actor {PhotonNetwork.LocalPlayer.ActorNumber}. Aborting swipe handling.");
            return; // Early return if the player's hand is not available
        }

        int cardCount = playerHands[PhotonNetwork.LocalPlayer.ActorNumber].Count; // Get current number of cards
        float cardWidth = 1.0f; // Approximate width of each card in units
        float totalHandWidth = cardCount * cardWidth; // Calculate total hand width

        // Define base minimum and maximum positions for scroll bounds
        float baseMinScrollPosition = -.1f; // Adjusted minimum scroll position
        float baseMaxScrollPosition = .1f; // Adjusted maximum scroll position

        // Dynamically adjust min and max scroll positions based on card count
        minScrollPosition = baseMinScrollPosition - (totalHandWidth / 2f);
        maxScrollPosition = baseMaxScrollPosition + (totalHandWidth / 2f);

        if (Input.GetMouseButtonDown(0))
        {
            // Capture initial touch position
            initialTouchPosition = Input.mousePosition;
        }
        else if (Input.GetMouseButton(0))
        {
            // Calculate swipe distance
            Vector3 currentTouchPosition = Input.mousePosition;
            float swipeDelta = currentTouchPosition.x - initialTouchPosition.x;

            // Calculate the new position with clamping
            Vector3 newPosition = playerHandTransform.localPosition + new Vector3(swipeDelta * scrollSpeed * Time.deltaTime, 0, 0);
            newPosition.x = Mathf.Clamp(newPosition.x, minScrollPosition, maxScrollPosition);

            // Smoothly interpolate the player's hand position
            playerHandTransform.localPosition = Vector3.Lerp(playerHandTransform.localPosition, newPosition, 0.1f); // Adjust the third parameter for speed of smoothing

            // Update the initial touch position
            initialTouchPosition = currentTouchPosition;
        }
    }





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

        isPlayerTurn = PhotonNetwork.IsMasterClient;
        UpdateTurnText();
        shapeSelectionPanel.SetActive(false);
    }

    [PunRPC]
    void RPC_DealInitialCards(int actorNumber)
    {
        if (!playerHands.ContainsKey(actorNumber))
        {
            playerHands[actorNumber] = new List<GameObject>();
        }

        Transform handTransform = roomManager.GetPlayerHandTransform(actorNumber);
        if (handTransform != null)
        {
            for (int i = 0; i < startingHandSize; i++)
            {
                DealCardToPlayer(playerHands[actorNumber], handTransform, actorNumber);
            }
        }
        else
        {
            Debug.LogWarning($"Hand transform for player {actorNumber} is null.");
        }

        if (PhotonNetwork.LocalPlayer.ActorNumber != actorNumber)
        {
            Transform opponentHandTransform = roomManager.GetPlayerHandTransform(actorNumber);
            UpdateHandLayout(playerHands[actorNumber], opponentHandTransform);
        }
    }

    [PunRPC]
    void RPC_GenerateDeck()
    {
        string[] shapes = { "Circle", "Square", "Cross", "Star", "Triangle"}; // Whot excluded initially
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

    void DealCardToPlayer(List<GameObject> hand, Transform handTransform, int actorNumber)
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

    // Synchronize the deck state with other players
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

            // Instantiate a new card and add it to the hand
            GameObject newCard = Instantiate(cardPrefab);
            Card cardComponent = newCard.GetComponent<Card>();
            cardComponent.shapeSprites = shapeSprites.GetShapeSpriteDictionary();
            newCard.GetComponent<Card>().SetCard(shape, number);
            hand.Add(newCard);

            // Ensure the new card is parented to the correct player hand
            newCard.transform.SetParent(roomManager.GetPlayerHandTransform(actorNumber));
        }

        // Update hand layout and visibility (local player sees front, opponents see back)
        Transform handTransform = roomManager.GetPlayerHandTransform(actorNumber);
        UpdateHandLayout(hand, handTransform);
    }


    void UpdateHandLayout(List<GameObject> hand, Transform handTransform)
    {
        if (handTransform == playerHandTransform) // If this is the local player's hand
        {
            float cardSpacing = 1.5f; // Fixed space between cards
            float startPositionX = -2.45f;  // Initial X position (could be 0 or some offset)

            for (int i = 0; i < hand.Count; i++)
            {
                GameObject card = hand[i];
                Card cardComponent = card.GetComponent<Card>();

                // Only position the card if it hasn't been positioned yet
                if (!cardComponent.isPositioned)
                {
                    float positionX = startPositionX + i * cardSpacing;
                    card.transform.localPosition = new Vector3(positionX, 0, 0); // Set the position of the new card
                    cardComponent.isPositioned = true; // Mark this card as positioned
                }

                // Show the front of the card for the local player
                card.GetComponent<Card>().ShowFront();
            }
        }
        else
        {
            // Stack opponent's cards on top of each other to save space
            float stackOffset = 0.05f; // Minimal offset to create a stack appearance

            for (int i = 0; i < hand.Count; i++)
            {
                GameObject card = hand[i];
                Card cardComponent = card.GetComponent<Card>();

                // Position all cards at the same spot with a slight vertical offset
                card.transform.localPosition = new Vector3(0, i * stackOffset, 0);

                // Ensure that the back of the card is shown for opponents
                card.GetComponent<Card>().ShowBack();
            }
        }
    }



    public void PlayCard(Card card)
    {
        if (isPlayerTurn && CanPlayCard(card))
        {

            photonView.RPC("RPC_PlayCard", RpcTarget.AllBuffered, card.shape, card.number, PhotonNetwork.LocalPlayer.ActorNumber);

            if (card.shape == "Whot")
            {
                isShapeSelectionActive = true;
                // Show the shape selection panel if a Whot card is played
                shapeSelectionPanel.SetActive(true);
                
                isPlayerTurn = false;
                photonView.RPC("RPC_BlockOpponentActions", RpcTarget.OthersBuffered, true);
                return;
            }
            // Check for special cards and apply effects
            else if (card.number == 2) // Assuming "PickTwo" is a valid shape
            {
                int opponentActorNumber = GetOpponentActorNumber(); // Get the opponent's ActorNumber
                photonView.RPC("RPC_PickTwoEffect", RpcTarget.All, opponentActorNumber); // Target the specific opponent

                // Skip opponent's turn after "Pick Two" effect
                photonView.RPC("RPC_SkipTurn", RpcTarget.AllBuffered, PhotonNetwork.LocalPlayer.ActorNumber);
            }
            else if (card.number == 14) // Assuming "General Market" is card 14
            {
                int opponentActorNumber = GetOpponentActorNumber();
                photonView.RPC("RPC_GeneralMarketEffect", RpcTarget.All, opponentActorNumber); // Apply to all players

                // Skip opponent's turn after "General Market" effect
                photonView.RPC("RPC_SkipTurn", RpcTarget.AllBuffered, PhotonNetwork.LocalPlayer.ActorNumber);
            }
           

            RemoveCardFromHand(card);
            photonView.RPC("RPC_UpdateHand", RpcTarget.Others, PhotonNetwork.LocalPlayer.ActorNumber, playerHands[PhotonNetwork.LocalPlayer.ActorNumber].Select(c => c.GetComponent<Card>().shape + "_" + c.GetComponent<Card>().number).ToArray());
        }
    }
    [PunRPC]
    void RPC_BlockOpponentActions(bool block)
    {
        isShapeSelectionActive = block;
    }

    [PunRPC]
    void RPC_SkipTurn(int currentPlayerActorNumber)
    {
        // Ensure it's still the current player's turn
        isPlayerTurn = PhotonNetwork.LocalPlayer.ActorNumber == currentPlayerActorNumber;
        UpdateTurnText(); // Update the UI to reflect the correct turn
    }
    public void OnShapeSelected(string selectedShape)
    {
        // Hide the shape selection panel
        shapeSelectionPanel.SetActive(false);

        // Send the selected shape to other players
        photonView.RPC("RPC_ShapeSelected", RpcTarget.AllBuffered, selectedShape, PhotonNetwork.LocalPlayer.ActorNumber);

        // Allow the player to proceed with their turn after selecting the shape
        isPlayerTurn = true;

        // Update the requestedShape variable
        requestedShape = selectedShape;

        // You may need to update the UI for the local player as well to indicate the selected shape
        

        SwitchTurns();
    }


    [PunRPC]
    void RPC_ShapeSelected(string selectedShape, int actorNumber)
    {
        // Update the requested shape in the game
        requestedShape = selectedShape;
        photonView.RPC("RPC_BlockOpponentActions", RpcTarget.OthersBuffered, false);
        // Inform other players of the selected shape
        if (PhotonNetwork.LocalPlayer.ActorNumber != actorNumber)
        {
            // Display a message to the opponent
            turnText.text = $"Opponent selected: {selectedShape}. Provide or draw!";
        }
    }

    void RemoveCardFromHand(Card card)
    {
        GameObject cardObject = card.gameObject;

        if (playerHands[PhotonNetwork.LocalPlayer.ActorNumber].Contains(cardObject))
        {
            playerHands[PhotonNetwork.LocalPlayer.ActorNumber].Remove(cardObject); // Remove the card from the player's hand

            // Ensure we always have 4 visible cards if there are more cards in the hand
            if (playerHands[PhotonNetwork.LocalPlayer.ActorNumber].Count > 4)
            {
                // Check if the current visible section is less than 4 after removing a card
                if (playerHands[PhotonNetwork.LocalPlayer.ActorNumber].Count - visibleCardStartIndex < 4)
                {
                    // Decrease visibleCardStartIndex to bring another card into the visible section
                    visibleCardStartIndex = Mathf.Max(visibleCardStartIndex - 1, 0);
                }
            }

            // Update the hand layout to reflect the removed card and adjust the visible section
            UpdateHandLayout(playerHands[PhotonNetwork.LocalPlayer.ActorNumber], playerHandTransform);

            CheckForWinCondition();
        }
    }



    [PunRPC]
    void RPC_PlayCard(string shape, int number, int actorNumber)
    {
        if (topCard != null)
        {
            topCard.SetActive(false);
            topCard.GetComponent<Collider2D>().enabled = false; // Disable collider on the old top card
        }

        // Find the card that matches the shape and number
        GameObject playedCard = playerHands[PhotonNetwork.LocalPlayer.ActorNumber].Find(c => c.GetComponent<Card>().shape == shape && c.GetComponent<Card>().number == number);
        if (playedCard != null)
        {
            // Move the played card to the discard pile and set it as the new top card
            playedCard.transform.SetParent(discardPileTransform);
            playedCard.transform.localPosition = Vector3.zero; // Position the card at the center of the discard pile
            playedCard.transform.localScale = Vector3.one * 0.4f; // Adjust the size for the discard pile
            playedCard.SetActive(true);

            topCard.GetComponent<Collider2D>().enabled = false; // Disable the collider on the new top card
            topCard = playedCard;

            photonView.RPC("RPC_SyncTopCard", RpcTarget.AllBuffered, shape, number);
            photonView.RPC("RPC_RemoveCardFromHand", RpcTarget.OthersBuffered, actorNumber, shape, number);


        }

        Debug.Log($"Card Played: {shape} {number}");

        SwitchTurns();
    }

    public void DrawCard()
    {
        if (!isShapeSelectionActive && isPlayerTurn && deck.Count > 0)
        {
            photonView.RPC("RPC_DrawCard", RpcTarget.AllBuffered, PhotonNetwork.LocalPlayer.ActorNumber);
        }
    }

    [PunRPC]
    void RPC_DrawCard(int drawingActorNumber)
    {
        if (PhotonNetwork.LocalPlayer.ActorNumber == drawingActorNumber)
        {
            DealCardToPlayer(playerHands[drawingActorNumber], playerHandTransform, drawingActorNumber);
        }

        CheckForWinCondition();
        SwitchTurns();
    }

    void SwitchTurns()
    {
        isPlayerTurn = !isPlayerTurn;
        UpdateTurnText();
    }

    void CreateCard(string shape, int number)
    {
        GameObject newCard = Instantiate(cardPrefab, deckTransform);
        Card cardComponent = newCard.GetComponent<Card>();

        // Ensure that the shapeSprites dictionary is set before the card is used
        cardComponent.shapeSprites = shapeSprites.GetShapeSpriteDictionary();

        // Debugging to ensure dictionary is populated


        // Now set the card shape and number
        cardComponent.SetCard(shape, number);

        deck.Add(newCard);
        newCard.SetActive(false);
    }

    void UpdateTurnText()
    {
        if (isPlayerTurn)
        {
            turnText.text = "Your Turn";
        }
        else
        {
            turnText.text = "Opponent's Turn";
        }
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



    [PunRPC]
    void RPC_SyncTopCard(string shape, int number)
    {
        Debug.Log($"Syncing top card: {shape} {number}");

        if (topCard == null) // Only create the top card if it's not already created
        {
            GameObject newTopCard = Instantiate(cardPrefab, discardPileTransform);
            Card card = newTopCard.GetComponent<Card>();
            card.shapeSprites = shapeSprites.GetShapeSpriteDictionary();
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
    void RPC_RemoveCardFromHand(int actorNumber, string shape, int number)
    {
        GameObject cardToRemove = playerHands[actorNumber].Find(c => c.GetComponent<Card>().shape == shape && c.GetComponent<Card>().number == number);
        if (cardToRemove != null)
        {
            playerHands[actorNumber].Remove(cardToRemove); // Remove the card from the player's hand
            Destroy(cardToRemove); // Optionally destroy the card
        }

        // Update the hand layout after removing the card
        Transform handTransform = roomManager.GetPlayerHandTransform(actorNumber);
        UpdateHandLayout(playerHands[actorNumber], handTransform);

        CheckForWinCondition();
    }
    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene("Menu");
    }

    void CheckForWinCondition()
    {
        foreach (var playerHand in playerHands)
        {
            if (playerHand.Value.Count == 0) // Check if any player's hand is empty
            {
                if (playerHand.Key == PhotonNetwork.LocalPlayer.ActorNumber)
                {
                    // Local player won
                    winPanel.SetActive(true);
                    photonView.RPC("ShowLosePanelForOpponent", RpcTarget.Others); // Notify the opponent
                }
                else
                {
                    // Opponent won
                    losePanel.SetActive(true);
                }
            }
        }
    }
    int GetOpponentActorNumber()
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
    [PunRPC]
    void RPC_PickTwoEffect(int targetPlayerActorNumber)
    {
        if (PhotonNetwork.LocalPlayer.ActorNumber == targetPlayerActorNumber) // Only apply to the player with the matching actor number
        {
            // The target player draws two cards
            for (int i = 0; i < 2; i++)
            {
                DealCardToPlayer(playerHands[targetPlayerActorNumber], roomManager.GetPlayerHandTransform(targetPlayerActorNumber), targetPlayerActorNumber);
            }
        }

      
    }

    [PunRPC]
    void RPC_GeneralMarketEffect(int targetPlayerActorNumber)
    {
        if (PhotonNetwork.LocalPlayer.ActorNumber == targetPlayerActorNumber) // Only apply to the player with the matching actor number
        {

           DealCardToPlayer(playerHands[targetPlayerActorNumber], roomManager.GetPlayerHandTransform(targetPlayerActorNumber), targetPlayerActorNumber);

            
        }

        
    }

    

    [PunRPC]
    void ShowLosePanelForOpponent()
    {
        losePanel.SetActive(true);
    }

    public bool CanPlayCard(Card card)
    {

        if (card.shape == "Whot") // Check if the card is a Whot card
        {
            return true; // Whot card can always be played
        }
        if (topCard.GetComponent<Card>().shape == "Whot")
        {
            return card.shape == requestedShape;
        }
        // Logic for normal cards (check shape and number)
        if (topCard != null)
        {
            Card topCardComponent = topCard.GetComponent<Card>();
            return card.shape == topCardComponent.shape || card.number == topCardComponent.number; // Allow if shapes or numbers match
        }

        return false;


    }
}



