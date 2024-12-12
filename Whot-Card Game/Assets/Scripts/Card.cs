using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class Card : MonoBehaviour
{
    #region Fields
    private Collider2D cardCollider;
    public bool isPositioned = false;

    [SerializeField] private TextMeshProUGUI numberText;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private GameObject cardBack;

    public string shape;
    public int number;
    public Dictionary<string, Sprite> shapeSprites;
    #endregion

    #region Unity Methods
    private void Awake()
    {
        cardCollider = GetComponent<Collider2D>();
    }

    private void OnMouseDown()
    {
        if (PlayerManager.Instance.CanPlayCard(this) && !Menu.isGamePaused)
        {
            PlayerManager.Instance.PlayCard(this);
            Debug.Log("Played");

            // If the card is played and a shape is requested, reset the requested shape
            if (!string.IsNullOrEmpty(GameManager.Instance.requestedShape))
            {
                //  gameManager.RequestShape(""); // Reset requested shape
            }
        }
    }
    #endregion

    #region Public Methods
    public void SetCard(string shape, int number)
    {
       
        this.shape = shape;
        this.number = number;
        UpdateCard(); // Update the visuals based on the shape and number
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
    #endregion

    #region Private Methods
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
    #endregion
}

