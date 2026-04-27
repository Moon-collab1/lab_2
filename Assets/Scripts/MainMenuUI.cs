using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Controla el menu principal: login/registro de usuario y navegacion a escenas.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("Panel de Login")]
    public GameObject loginPanel;
    public TMP_InputField usernameInput;
    public Button loginButton;
    public TextMeshProUGUI loginFeedbackText;

    [Header("Panel de Bienvenida")]
    public GameObject welcomePanel;
    public TextMeshProUGUI welcomeText;
    public TextMeshProUGUI statsText;
    public Button playButton;
    public Button logoutButton;

    [Header("Escena a cargar al jugar")]
    public string gameSceneName = "Dia Dos";

    // ──────────────────────────────────────────────────────────────────────
    void Start()
    {
        ShowLoginPanel();

        loginButton.onClick.AddListener(HandleLogin);
        playButton.onClick.AddListener(StartGame);
        logoutButton.onClick.AddListener(HandleLogout);

        // Permitir Enter para loguear
        usernameInput.onSubmit.AddListener(_ => HandleLogin());
    }

    // ── Mostrar panel de login ────────────────────────────────────────────
    void ShowLoginPanel()
    {
        loginPanel.SetActive(true);
        welcomePanel.SetActive(false);
        usernameInput.text = "";
        loginFeedbackText.text = "";
    }

    // ── Mostrar panel de bienvenida ───────────────────────────────────────
    void ShowWelcomePanel()
    {
        loginPanel.SetActive(false);
        welcomePanel.SetActive(true);

        UserData user = UserManager.Instance.CurrentUser;

        if (user.gamesPlayed == 0)
        {
            welcomeText.text = "Bienvenido, " + user.username + "!";
            statsText.text   = "Primera vez jugando. Buena suerte!";
        }
        else
        {
            welcomeText.text = "Bienvenido de nuevo, " + user.username + "!";
            statsText.text   = "Mejor puntaje: " + user.highScore +
                               "\nUltima partida: " + user.lastScore +
                               "\nPartidas jugadas: " + user.gamesPlayed +
                               "\nUltima vez: " + user.lastPlayed;
        }
    }

    // ── Logica de login ───────────────────────────────────────────────────
    void HandleLogin()
    {
        string username = usernameInput.text.Trim();

        if (string.IsNullOrEmpty(username))
        {
            loginFeedbackText.text = "Por favor ingresa un nombre de usuario.";
            return;
        }

        if (username.Length > 20)
        {
            loginFeedbackText.text = "El nombre no puede tener mas de 20 caracteres.";
            return;
        }

        bool existingUser = UserManager.Instance.LoginOrRegister(username);

        if (existingUser)
            loginFeedbackText.text = "Usuario encontrado. Cargando datos...";
        else
            loginFeedbackText.text = "Nuevo usuario registrado!";

        Invoke(nameof(ShowWelcomePanel), 0.8f);
    }

    // ── Iniciar juego ─────────────────────────────────────────────────────
    void StartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    // ── Cerrar sesion ─────────────────────────────────────────────────────
    void HandleLogout()
    {
        UserManager.Instance.Logout();
        ShowLoginPanel();
    }
}
