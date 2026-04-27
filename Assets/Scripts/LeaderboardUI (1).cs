using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Muestra el top 10 global y por minijuego en el Menu Principal.
/// Agrega este script a un panel de Canvas en la escena MainMenu.
/// </summary>
public class LeaderboardUI : MonoBehaviour
{
    [Header("Panel principal del leaderboard")]
    public GameObject leaderboardPanel;

    [Header("Botones de pestanas")]
    public Button btnGlobal;
    public Button btnMinijuego1;
    // Agrega mas botones aqui cuando tengas mas minijuegos

    [Header("Contenedor del ScrollView")]
    [Tooltip("Arrastra aqui el objeto 'Content' que esta dentro del ScrollView > Viewport")]
    public Transform contenedor;

    [Header("Prefab de una entrada del ranking")]
    [Tooltip("Un GameObject simple con un TextMeshProUGUI adentro")]
    public GameObject entradaPrefab;

    [Header("Boton para cerrar el leaderboard")]
    public Button btnCerrar;

    // ─────────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(false);

        if (btnGlobal != null)
            btnGlobal.onClick.AddListener(MostrarGlobal);

        if (btnMinijuego1 != null)
            btnMinijuego1.onClick.AddListener(() => MostrarMinijuego(UserManager.MINIJUEGO_1));

        if (btnCerrar != null)
            btnCerrar.onClick.AddListener(Cerrar);
    }

    // ── Abre el leaderboard y muestra el global por defecto ───────────────
    public void Abrir()
    {
        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(true);

        MostrarGlobal();
    }

    public void Cerrar()
    {
        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(false);
    }

    // ── Muestra top 10 por score global ───────────────────────────────────
    public void MostrarGlobal()
    {
        if (UserManager.Instance == null) return;
        UserData[] top = UserManager.Instance.GetTopGlobal(10);
        MostrarEntradas(top, "global");
    }

    // ── Muestra top 10 de un minijuego especifico ─────────────────────────
    public void MostrarMinijuego(string minigameName)
    {
        if (UserManager.Instance == null) return;
        UserData[] top = UserManager.Instance.GetTopByMiniGame(minigameName, 10);
        MostrarEntradas(top, minigameName);
    }

    // ── Limpia el contenedor y genera las entradas ────────────────────────
    private void MostrarEntradas(UserData[] usuarios, string modo)
    {
        // Limpiar entradas anteriores
        foreach (Transform hijo in contenedor)
            Destroy(hijo.gameObject);

        if (usuarios == null || usuarios.Length == 0)
        {
            CrearEntrada("No hay datos aun.");
            return;
        }

        for (int i = 0; i < usuarios.Length; i++)
        {
            int score = (modo == "global")
                ? usuarios[i].globalScore
                : usuarios[i].GetMiniGameScore(modo);

            string linea = $"{i + 1}.  {usuarios[i].username}  —  {score} pts";
            CrearEntrada(linea);
        }
    }

    // ── Instancia un prefab de entrada con el texto dado ──────────────────
    private void CrearEntrada(string texto)
    {
        if (entradaPrefab == null || contenedor == null)
        {
            Debug.LogWarning("[LeaderboardUI] Falta asignar entradaPrefab o contenedor en el Inspector.");
            return;
        }

        GameObject entrada = Instantiate(entradaPrefab, contenedor);
        TextMeshProUGUI tmp = entrada.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
            tmp.text = texto;
    }
}
