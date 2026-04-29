using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Optional: List your project scene names here")]
    [SerializeField] private List<string> projectScenes = new List<string>();
    [SerializeField] private string passthroughScene;
    [SerializeField] private string vrScene;
    [SerializeField] private string homeScene;
    [SerializeField] private bool isPassthrough;
    [SerializeField] private bool isVisibleTimer;
    [SerializeField] private bool isSlides;
    [SerializeField] private bool isNoteCards;
    [SerializeField] private bool isPaused=false;
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject startButton;
    [SerializeField] private GameObject quitButton;

    [Header("Placement Settings")]
    [SerializeField] private float distanceFromPlayer = 1.5f;
    [SerializeField] private float verticalOffset = -0.1f; // slightly below eye level
    [SerializeField] private float horizontalOffset = -0.1f; // slightly below eye level

    private Transform GetCamera()
    {
        // Works across scene loads - always finds the active camera
        Camera cam = Camera.main;
        return cam != null ? cam.transform : null;
    }
    public void SummonMenu()
    {
        Vector3 spawnPosition = GetMenuPosition();
        Quaternion spawnRotation = GetMenuRotation(spawnPosition);
        pauseMenu.transform.position = spawnPosition;
        pauseMenu.transform.rotation = spawnRotation;
        
    }

    private Vector3 GetMenuPosition()
    {
        // Use only the horizontal (Y) rotation of the camera - ignore tilt
        Transform cam = GetCamera();
        Vector3 flatForward = cam.forward;
        flatForward.y = 0f;
        flatForward.Normalize();

        Vector3 position = Camera.main.transform.position
            + flatForward * distanceFromPlayer
            + Vector3.up * verticalOffset;
            

        return position;
    }

    private Quaternion GetMenuRotation(Vector3 menuPosition)
    {
        Transform cam = GetCamera();
        if (cam == null) return Quaternion.identity;

        // Direction from menu TO player, flattened so menu stays upright
        Vector3 directionToPlayer = cam.position - menuPosition;
        directionToPlayer.y = 0f;

        if (directionToPlayer == Vector3.zero)
            return Quaternion.identity;

        return Quaternion.LookRotation(-directionToPlayer);
    }
    private void Awake()
    {
        // Enforce singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Invoke(nameof(SummonMenu), 0.1f);
    }
    
    private void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.Start))
        {
            menuPress();
        }
        if (OVRInput.GetDown(OVRInput.Button.One))
        {
            JumpToTime(360f);
        }
    }
    public void menuPress()
    {
        if (SceneManager.GetActiveScene().name == homeScene)
        {
            SummonMenu();
            return;
        }
        if (isPaused)
        {
            pauseMenu.SetActive(false);
            isPaused = false;
            ResumeVideoPlayerInScene();
        }
        else
        {
            SummonMenu();
            pauseMenu.SetActive(true);
            isPaused = true;
            PauseVideoPlayerInScene();
        }
    }
    public void PauseVideoPlayerInScene()
    {
        VideoPlayer vp = FindObjectOfType<VideoPlayer>();

        if (vp != null)
        {
            vp.Pause();
            Debug.Log("Video paused.");
        }
        else
        {
            Debug.LogWarning("No VideoPlayer found in the scene.");
        }
    }
    public void ResumeVideoPlayerInScene()
    {
        VideoPlayer vp = FindObjectOfType<VideoPlayer>();

        if (vp != null)
        {
            vp.Play(); // resumes if paused
            Debug.Log("Video resumed.");
        }
        else
        {
            Debug.LogWarning("No VideoPlayer found in the scene.");
        }
    }
    public void JumpToTime(double timeInSeconds)
    {
        VideoPlayer videoPlayer = FindObjectOfType<VideoPlayer>();
        if (videoPlayer == null)
        {
            Debug.LogWarning("VideoPlayer not assigned.");
            return;
        }

        if (!videoPlayer.isPrepared)
        {
            Debug.LogWarning("VideoPlayer not ready yet.");
            return;
        }

        videoPlayer.time = timeInSeconds;
        Debug.Log($"Jumped to {timeInSeconds} seconds.");
    }
    public void goHome()
    {
        startButton.SetActive(true);
        quitButton.SetActive(false);
        isPaused = false;
        LoadScene(homeScene);
        Invoke(nameof(SummonMenu), 0.1f);
    }
    public void BeginPresentation()
    {
        if(isPassthrough)
            SceneManager.LoadScene(passthroughScene);
        else
            SceneManager.LoadScene(vrScene);
        startButton.SetActive(false);
        quitButton.SetActive(true);
        pauseMenu.SetActive(false);
        
    }
    public void togglePassthrough()
    {
        isPassthrough = !isPassthrough;
    }
    public void toggleTimer()
    {
        isVisibleTimer = !isVisibleTimer;
    }
    public void toggleSlides()
    {
        isSlides = !isSlides;
    }
    public void toggleNotes()
    {
        isNoteCards = !isNoteCards;
    }
    /// <summary>
    /// Loads a scene by its exact name.
    /// </summary>
    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("LoadScene failed: scene name is null or empty.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Reloads the currently active scene.
    /// </summary>
    public void ReloadCurrentScene()
    {
        menuPress();
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    /// <summary>
    /// Returns true if the scene name exists in the optional projectScenes list.
    /// </summary>
    public bool HasScene(string sceneName)
    {
        return projectScenes.Contains(sceneName);
    }

    /// <summary>
    /// Returns the optional list of project scene names.
    /// </summary>
    public List<string> GetAllSceneNames()
    {
        return new List<string>(projectScenes);
    }

    /// <summary>
    /// Quits the game. Stops play mode in the editor.
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("Quitting game...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
