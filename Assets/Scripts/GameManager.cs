using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Optional: List your project scene names here")]
    [SerializeField] private List<string> projectScenes = new List<string>();
    [SerializeField] private string passthroughScene;
    [SerializeField] private string vrScene;
    [SerializeField] private bool isPassthrough;
    [SerializeField] private bool isVisibleTimer;
    [SerializeField] private bool isSlides;
    [SerializeField] private bool isNoteCards;
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
    }

    public void BeginPresentation()
    {
        SceneManager.LoadScene(vrScene);
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
