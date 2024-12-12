using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadConnecting : MonoBehaviour
{
    #region Public Variables
    public bool isPlayRandom = false;
    public bool isCreateRoom = false;
    public bool isJoinRoom = false;
    public TextMeshProUGUI roomIDText;
    #endregion

    #region Unity Methods
    void Start()
    {
        isCreateRoom = false;
        isJoinRoom = false;
        isPlayRandom = false;
    }
    #endregion

    #region Public Methods
    public void EnterCreateRoom()
    {
        isCreateRoom = true;
        isJoinRoom = false;
        isPlayRandom = false;
    }

    public void EnterJoinRoom()
    {
        isJoinRoom = true;
        isCreateRoom = false;
        isPlayRandom = false;
    }

    public void EnterMultiPlayer()
    {
        isPlayRandom = true;
        isJoinRoom = false;
        isCreateRoom = false;
        roomIDText.text = "";
    }
    #endregion
}
