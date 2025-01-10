using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class Menu : MonoBehaviour
{

    public static bool isGamePaused = false;

    #region Public Methods
    public void Play()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Game");
    }

    public void Resume()
    {
        isGamePaused = false;
        Time.timeScale = 1f;
        
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Game");
    }

    public void Pause()
    {
        isGamePaused = true;
        Time.timeScale = 0f;
    }

    public void MainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Menu");
    }

    public void Quit()
    {
        Application.Quit();
    }
    #endregion
}
