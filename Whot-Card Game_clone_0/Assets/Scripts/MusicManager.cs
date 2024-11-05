using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    public AudioClip menuMusicClip;  // Music clip for the Main Menu
    public AudioClip gameMusicClip;  // Music clip for the Game Scene

    private AudioSource audioSource;

    private static MusicManager instance;

    private void Awake()
    {
        // Singleton pattern to keep music persistent across scenes
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Persist between scenes
            audioSource = GetComponent<AudioSource>();
        }
        else
        {
            Destroy(gameObject); // Avoid duplicate music managers
        }
    }

    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Start by playing the menu music if in the main menu
        if (SceneManager.GetActiveScene().name == "Menu")
        {
            PlayMusic(menuMusicClip);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Game")
        {
            PlayMusic(gameMusicClip);
        }
        else if (scene.name == "Menu")
        {
            PlayMusic(menuMusicClip);
        }
    }

    private void PlayMusic(AudioClip clip)
    {
        if (audioSource.clip != clip)  // Check if the clip is different
        {
            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.Play();
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
