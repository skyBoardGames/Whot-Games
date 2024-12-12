using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
public class SplashScreen : MonoBehaviour
{  
    public float splashDuration = 3.0f;

    void Start()
    {
        
        StartCoroutine(LoadMainMenuAfterDelay(splashDuration));
    }

    // Coroutine to wait for a set duration before switching scenes
    IEnumerator LoadMainMenuAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene("Menu");
    }
}
