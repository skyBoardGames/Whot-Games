using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class Card : MonoBehaviour
{
    public string shape;
    public int number;
    public TextMeshProUGUI numberText;
    public SpriteRenderer spriteRenderer;
    public GameObject cardBack; // Reference to the CardBack GameObject

    public Dictionary<string, Sprite> shapeSprites;

    private GameManager gameManager;
    private Collider2D cardCollider; // Reference to the card's collider
    public bool isPositioned = false;
    private void Awake()
    {
        cardCollider = GetComponent<Collider2D>();
    }

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
    }

    public void SetCard(string shape, int number)
    {
       
        this.shape = shape;
        this.number = number;
        UpdateCard(); // Update the visuals based on the shape and number
    }

    void UpdateCard()
    {
        numberText.text = number.ToString();
        if (shapeSprites == null)
        {
            Debug.LogError("ShapeSprites dictionary is null in Card.");
            return;
        }
        if (shapeSprites.ContainsKey(shape))
        {
            spriteRenderer.sprite = shapeSprites[shape];
        }
        else
        {
            Debug.LogWarning($"Shape {shape} not found in shapeSprites dictionary");
        }
    }

    public void ShowFront()
    {
        spriteRenderer.enabled = true;
        numberText.enabled = true;
        cardBack.SetActive(false);
        cardCollider.enabled = true; // Enable collider so player can interact
    }

    public void ShowBack()
    {
        spriteRenderer.enabled = false;
        numberText.enabled = false;
        cardBack.SetActive(true);
        cardCollider.enabled = false; // Disable collider for non-playable cards
    }

    private void OnMouseDown()
    {
        if (gameManager.CanPlayCard(this) && !Menu.isGamePaused)
        {
            gameManager.PlayCard(this);
            Debug.Log("Played");

            // If the card is played and a shape is requested, reset the requested shape
            if (!string.IsNullOrEmpty(gameManager.requestedShape))
            {
                //  gameManager.RequestShape(""); // Reset requested shape
            }
        }
    }
}

