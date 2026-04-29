using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PauseMenuWalk : MonoBehaviour
{
    [Header("Panel de Pausa")]
    public GameObject pausePanel;

    [Header("Botones")]
    public Button continueButton;
    public Button mainMenuButton;

    private bool isPaused = false;

    void Start()
    {
        pausePanel.SetActive(false);
        continueButton.onClick.AddListener(Continue);
        mainMenuButton.onClick.AddListener(GoToMainMenu);
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused) Continue();
            else Pause();
        }
    }

    void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        pausePanel.SetActive(true);
    }

    void Continue()
    {
        isPaused = false;
        Time.timeScale = 1f;
        pausePanel.SetActive(false);
    }

    void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Menu");
    }
}