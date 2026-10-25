using UnityEngine;
using UnityEngine.SceneManagement;

public class GameFlowManager : MonoBehaviour
{
    public GameObject paradoxPanel;

    // We don't even need to disable the player script anymore
    // This LateUpdate method is strong enough to overpower it.
    private bool isGameOver = false;

    // LATE UPDATE: Runs AFTER the player script tries to hide the mouse
    void LateUpdate()
    {
        if (isGameOver)
        {
            // Force the cursor to be free every single frame, at the very end of the frame
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void TriggerParadox()
    {
        isGameOver = true;

        // 1. Show the Menu
        if (paradoxPanel != null)
            paradoxPanel.SetActive(true);

        // 2. Stop Time
        Time.timeScale = 0f;
    }

    public void RestartLevel()
    {
        isGameOver = false;

        // 1. Unpause Time
        Time.timeScale = 1f;

        // 2. Reload the scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}