using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Handles the simple home screen flow.
/// </summary>
public class HomeScreenController : MonoBehaviour
{
    [SerializeField] private string tourSceneName = "AguinaldoShrineARTour";

    /// <summary>
    /// Opens the AR navigation scene.
    /// </summary>
    public void StartTour()
    {
        SceneManager.LoadScene(tourSceneName);
    }

    /// <summary>
    /// Exits the application.
    /// </summary>
    public void ExitApplication()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
