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
        penaltyCoroutines = new Coroutine[watchedObjects.Length]; // Crea el arreglo vacío

        UpdateHeartsUI();
        UpdateScoreUI();

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        for (int i = 0; i < watchedObjects.Length; i++)
            if (watchedObjects[i] != null)
                watchedObjects[i].OnStartedFacingPlayer += HandleObjectFacingPlayer; //cuando empieces a mirar al jugador, avísale a HandleObjectFacingPlayer

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);

        if (nextSceneButton != null)
            nextSceneButton.onClick.AddListener(GoToNextScene);

        StartCoroutine(ScoreIncrement());//inicia la funcion que aumenta el score cada segundo
    }

    void OnDestroy()
    {
        for (int i = 0; i < watchedObjects.Length; i++)
            if (watchedObjects[i] != null)
                watchedObjects[i].OnStartedFacingPlayer -= HandleObjectFacingPlayer;// Limpia los eventos para evitar errores si el GameManager se destruye antes que los objetos vigilados, esto por hacer cambio de escena
    }

    // ── Cuando un objeto empieza a mirarte ────────────────────────────────
    void HandleObjectFacingPlayer()
    {
        if (isGameOver) return;

        SetStatus("! Te esta mirando! Haz clic en el Oso!");

        for (int i = 0; i < watchedObjects.Length; i++)
        {
            if (watchedObjects[i] != null && watchedObjects[i].isFacingPlayer)
                if (penaltyCoroutines[i] == null)
                    penaltyCoroutines[i] = StartCoroutine(PenaltyCountdown(i));//Si no hay ya una cuenta regresiva corriendo para ese objeto, inicia una nueva.
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
                penaltyCoroutines[i] = null;//Detiene la cuenta regresiva si el jugador reacciono a tiempo
            }
        }

        SetStatus("Bien! Lo alejaste a tiempo.");
    }

    // ── Cuenta regresiva: quita corazon cada segundo mientras te mira ──────
    IEnumerator PenaltyCountdown(int objectIndex)
    {
        yield return new WaitForSeconds(lookTimer);//Espera el tiempo definido(3 seg) antes de empezar a quitar corazones

        while (!isGameOver
               && watchedObjects[objectIndex] != null
               && watchedObjects[objectIndex].isFacingPlayer)
        {
            LoseHeart();
            yield return new WaitForSeconds(3f);//Después de perder un corazón, espera otros 3 segundos antes de quitar otro, siempre y cuando el objeto siga mirándote
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
        currentHearts = Mathf.Max(0, currentHearts - 1);// Reduce el número de corazones, pero el Mathf no permite que sea menos de 0
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
        Time.timeScale = 0f;// Detiene el tiempo para congelar el juego
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
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);// Recarga la escena actual para reiniciar el juego
    }

    void GoToNextScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Menu");
    }

    void SetStatus(string msg)
    {
        if (statusText != null)
            statusText.text = msg;
        Debug.Log("[GameManager] " + msg);
    }
}
