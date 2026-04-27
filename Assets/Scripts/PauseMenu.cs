using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// Menu de pausa activado con Escape.
/// Permite guardar, continuar o volver al menu principal.
/// Adjuntar a un GameObject en la escena del juego.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("Panel de Pausa")]
    public GameObject pausePanel;

    [Header("Botones")]
    public Button continueButton;
    public Button saveButton;
    public Button mainMenuButton;

    [Header("Feedback")]
    public TextMeshProUGUI feedbackText;
    public TextMeshProUGUI currentScoreText;

    [Header("Referencia al GameManager")]
    public GameManager gameManager;

    private bool isPaused = false;

    // ──────────────────────────────────────────────────────────────────────
    void Start()
    {
        pausePanel.SetActive(false);

        continueButton.onClick.AddListener(Continue);
        saveButton.onClick.AddListener(SaveGame);
        mainMenuButton.onClick.AddListener(GoToMainMenu);
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused)
                Continue();
            else
                Pause();
        }
    }

    // ── Pausar ────────────────────────────────────────────────────────────
    void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        pausePanel.SetActive(true);
        feedbackText.text = "";

        if (currentScoreText != null && gameManager != null)
            currentScoreText.text = "Puntaje actual: " + gameManager.currentScore;
    }

    // ── Continuar ─────────────────────────────────────────────────────────
    void Continue()
    {
        isPaused = false;
        Time.timeScale = 1f;
        pausePanel.SetActive(false);
    }

    // ── Guardar puntaje actual ────────────────────────────────────────────
    void SaveGame()
    {
        if (UserManager.Instance == null || UserManager.Instance.CurrentUser == null)
        {
            feedbackText.text = "Error: no hay usuario activo.";
            return;
        }

        if (gameManager == null)
        {
            feedbackText.text = "Error: no se encontro el GameManager.";
            return;
        }

        UserManager.Instance.SaveCurrentScore(gameManager.currentScore);
        feedbackText.text = "Partida guardada! Puntaje: " + gameManager.currentScore;
    }

    // ── Volver al menu principal ──────────────────────────────────────────
    void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Menu");
    }
}
