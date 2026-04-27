using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    // ── Inspector References ───────────────────────────────────────────────
    [Header("Objetos a vigilar")]
    public ObjectStateMachine[] watchedObjects;

    [Header("Corazones / Vidas")]
    public int maxHearts = 3;
    public Image[] heartIcons;

    [Header("Temporizador - perder corazon si ignoras")]
    [Tooltip("Segundos que tiene el jugador para hacer clic antes de perder un corazon")]
    public float lookTimer = 3f;

    [Header("Pantalla de Game Over")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverText;
    public Button restartButton;
    public Button nextSceneButton;

    [Header("UI (opcional)")]
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI scoreText;

    // ── Estado privado ─────────────────────────────────────────────────────
    private int currentHearts;
    public int currentScore = 0;
    private Coroutine[] penaltyCoroutines;
    private bool isGameOver = false;

    // ─────────────────────────────────────────────────────────────────────────
    void Start()
    {
        currentHearts     = maxHearts;
        currentScore      = 0;
        penaltyCoroutines = new Coroutine[watchedObjects.Length];

        UpdateHeartsUI();
        UpdateScoreUI();

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        for (int i = 0; i < watchedObjects.Length; i++)
            if (watchedObjects[i] != null)
                watchedObjects[i].OnStartedFacingPlayer += HandleObjectFacingPlayer;

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);

        if (nextSceneButton != null)
            nextSceneButton.onClick.AddListener(GoToNextScene);

        StartCoroutine(ScoreIncrement());
    }

    void OnDestroy()
    {
        for (int i = 0; i < watchedObjects.Length; i++)
            if (watchedObjects[i] != null)
                watchedObjects[i].OnStartedFacingPlayer -= HandleObjectFacingPlayer;
    }

    // ── Cuando un objeto empieza a mirarte ────────────────────────────────
    void HandleObjectFacingPlayer()
    {
        if (isGameOver) return;

        SetStatus("! Te esta mirando! Haz clic en el boton!");

        for (int i = 0; i < watchedObjects.Length; i++)
        {
            if (watchedObjects[i] != null && watchedObjects[i].isFacingPlayer)
                if (penaltyCoroutines[i] == null)
                    penaltyCoroutines[i] = StartCoroutine(PenaltyCountdown(i));
        }
    }

    // ── El jugador reacciono a tiempo ─────────────────────────────────────
    public void OnPlayerReactedInTime()
    {
        if (isGameOver) return;

        for (int i = 0; i < penaltyCoroutines.Length; i++)
        {
            if (penaltyCoroutines[i] != null)
            {
                StopCoroutine(penaltyCoroutines[i]);
                penaltyCoroutines[i] = null;
            }
        }

        SetStatus("Bien! Lo alejaste a tiempo.");
    }

    // ── Cuenta regresiva: quita corazon cada segundo mientras te mira ──────
    IEnumerator PenaltyCountdown(int objectIndex)
    {
        yield return new WaitForSeconds(lookTimer);

        while (!isGameOver
               && watchedObjects[objectIndex] != null
               && watchedObjects[objectIndex].isFacingPlayer)
        {
            LoseHeart();
            yield return new WaitForSeconds(1f);
        }

        penaltyCoroutines[objectIndex] = null;
    }

    // ── Score por segundo ─────────────────────────────────────────────────
    IEnumerator ScoreIncrement()
    {
        while (!isGameOver)
        {
            yield return new WaitForSeconds(1f);
            if (!isGameOver)
            {
                currentScore++;
                UpdateScoreUI();
            }
        }
    }

    // ── Logica de corazones ───────────────────────────────────────────────
    void LoseHeart()
    {
        currentHearts = Mathf.Max(0, currentHearts - 1);
        UpdateHeartsUI();
        SetStatus("Perdiste un corazon! Te quedan " + currentHearts + ".");

        if (currentHearts <= 0)
            GameOver();
    }

    void UpdateHeartsUI()
    {
        for (int i = 0; i < heartIcons.Length; i++)
            heartIcons[i].enabled = (i < currentHearts);
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = "Puntos: " + currentScore;
    }

    // ── Game Over ─────────────────────────────────────────────────────────
    void GameOver()
    {
        isGameOver = true;
        Time.timeScale = 0f;
        SetStatus("FIN DEL JUEGO");

        // ★ Guardar score automaticamente en el perfil del usuario activo
        if (UserManager.Instance != null)
            UserManager.Instance.SaveCurrentScore(currentScore, UserManager.MINIJUEGO_1);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (gameOverText != null)
            gameOverText.text = "Fin del juego!\nPuntuacion: " + currentScore;
    }

    void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void GoToNextScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Dia Dos");
    }

    void SetStatus(string msg)
    {
        if (statusText != null)
            statusText.text = msg;
        Debug.Log("[GameManager] " + msg);
    }
}
