using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
public class SplashScreen : MonoBehaviour
{
    // Duration for the splash screen
    public float splashDuration = 3.0f;

    void Start()
    {
        // Start the splash screen timer
        StartCoroutine(LoadMainMenuAfterDelay(splashDuration));
    }

    // Coroutine to wait for a set duration before switching scenes
    IEnumerator LoadMainMenuAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Load the next scene (e.g., main menu)
        SceneManager.LoadScene("Menu");
    }
}
