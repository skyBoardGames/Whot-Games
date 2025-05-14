using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
using TMPro;
using System.Net.Sockets;

public class GameManager : MonoBehaviourPunCallbacks
{
    #region Singleton
    public static GameManager Instance;
    #endregion

    #region Public Variables
    public GameObject shapeSelectionPanel;
    public bool isShapeSelectionActive = false;
    public GameObject winPanel;
    public GameObject losePanel;
    public ShapeSpriteSO shapeSprites;
    public string requestedShape;
    public int playerHandCount;
    bool isCodeSent = false;
    string code = "";
    private SocketIOUnity socket;
    #endregion

    #region Unity Methods
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        TurnManager.Instance.InitializeTurn();
        UIManager.Instance.HideAllPanels();
        DeckManager.Instance.InitializeGame();
        requestedShape = null;
        //StartCoroutine(GetCode());
       //StartCoroutine(WaitLobbyID());
    }

    void Update()
    {
        PlayerManager.Instance.HandleSwipeScrolling();

    }
    #endregion

    private bool GetIsCodeSent()
    {
        return isCodeSent;
    }
    IEnumerator GetCode()
    {
        yield return new WaitForSeconds(3);
        code = "12345";
        isCodeSent = true;
    }

    IEnumerator WaitLobbyID()
    {
        yield return new WaitUntil(GetIsCodeSent);
       // yield return new WaitForSeconds(3);
       
        RoomManager.Instance.JoinOrCreateRoom(code);
    }

    #region Public Methods
    public void CheckForWinCondition()
    {
        foreach (var playerHand in PlayerManager.Instance.playerHands)
        {
            playerHandCount = playerHand.Value.Count;
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

    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene("Menu");
    }
    #endregion

    #region PunRPC Methods
    [PunRPC]
    void ShowLosePanelForOpponent()
    {
        losePanel.SetActive(true);
    }
    #endregion
}
