using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LeaderboardUI : MonoBehaviour
{
    [Header("Panel principal del leaderboard")]
    public GameObject leaderboardPanel;

    [Header("Botones de pestanas")]
    public Button btnGlobal;
    public Button btnMinijuego1;

    [Header("Contenedor del ScrollView")]
    [Tooltip("Arrastra aqui el objeto 'Content' que esta dentro del ScrollView > Viewport")]
    public Transform contenedor;

    [Header("Prefab de una entrada del ranking")]
    [Tooltip("Un GameObject simple con un TextMeshProUGUI adentro")]
    public GameObject entradaPrefab;

    [Header("Boton para cerrar el leaderboard")]
    public Button btnCerrar;

    void Start()
    {
        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(false);
        else
            Debug.LogWarning("[Leaderboard] leaderboardPanel es NULL");

        if (btnGlobal != null)
            btnGlobal.onClick.AddListener(MostrarGlobal);
        else
            Debug.LogWarning("[Leaderboard] btnGlobal es NULL - no asignado en Inspector");

        if (btnMinijuego1 != null)
            btnMinijuego1.onClick.AddListener(() => MostrarMinijuego(UserManager.MINIJUEGO_1));// 
        else
            Debug.LogWarning("[Leaderboard] btnMinijuego1 es NULL - no asignado en Inspector");

        if (btnCerrar != null)
            btnCerrar.onClick.AddListener(Cerrar);
        else
            Debug.LogWarning("[Leaderboard] btnCerrar es NULL - no asignado en Inspector");

        if (contenedor == null)
            Debug.LogWarning("[Leaderboard] contenedor es NULL - arrastra el Content del ScrollView");

        if (entradaPrefab == null)
            Debug.LogWarning("[Leaderboard] entradaPrefab es NULL - arrastra el prefab de entrada");
    }

    public void Abrir()
    {
        Debug.Log("[Leaderboard] Abrir() llamado");
        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(true);
        MostrarGlobal();
    }

    public void Cerrar()
    {
        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(false);
    }

    public void MostrarGlobal()
    {
        Debug.Log("[Leaderboard] MostrarGlobal() llamado");

        if (UserManager.Instance == null)
        {
            Debug.LogWarning("[Leaderboard] UserManager.Instance es NULL");
            return;
        }

        UserData[] top = UserManager.Instance.GetTopGlobal(10);
        Debug.Log("[Leaderboard] Usuarios en top global: " + top.Length);
        MostrarEntradas(top, "global");
    }

    public void MostrarMinijuego(string minigameName)
    {
        Debug.Log("[Leaderboard] MostrarMinijuego() llamado para: " + minigameName);

        if (UserManager.Instance == null)
        {
            Debug.LogWarning("[Leaderboard] UserManager.Instance es NULL");
            return;
        }

        UserData[] top = UserManager.Instance.GetTopByMiniGame(minigameName, 10);
        Debug.Log("[Leaderboard] Usuarios en top minijuego: " + top.Length);
        MostrarEntradas(top, minigameName);
    }

    private void MostrarEntradas(UserData[] usuarios, string modo)
    {
        Debug.Log("[Leaderboard] MostrarEntradas() modo: " + modo);

        if (contenedor == null)
        {
            Debug.LogWarning("[Leaderboard] contenedor es NULL, no se pueden mostrar entradas");
            return;
        }

        foreach (Transform hijo in contenedor)
            Destroy(hijo.gameObject);// Limpia entradas anteriores para que siempre esten top 10

        if (usuarios == null || usuarios.Length == 0)
        {
            Debug.Log("[Leaderboard] No hay usuarios para mostrar");
            CrearEntrada("No hay datos aun.");
            return;
        }

        for (int i = 0; i < usuarios.Length; i++)
        {
            int score = (modo == "global")// el ? es para comparar si es el modo global o por minijuego
                ? usuarios[i].globalScore// Si es global, muestra el globalScore
                : usuarios[i].GetMiniGameScore(modo);// Si es por minijuego, muestra el score acumulado para ese minijuego

            string linea = $"{i + 1}.  {usuarios[i].username}  —  {score} pts";// el $ es para usar interpolación de strings( las variables dentro de {} se convierten automáticamente a texto), lo que hace más fácil construir la linea con variables
            Debug.Log("[Leaderboard] Entrada: " + linea);
            CrearEntrada(linea);// Crea una nueva entrada en el UI para cada usuario en el top, mostrando su posición, nombre y puntaje.
        }
    }

    private void CrearEntrada(string texto)
    {
        if (entradaPrefab == null || contenedor == null)
        {
            Debug.LogWarning("[Leaderboard] Falta entradaPrefab o contenedor");
            return;
        }

        GameObject entrada = Instantiate(entradaPrefab, contenedor);// Crea una nueva instancia del prefab como hijo del contenedor
        TextMeshProUGUI tmp = entrada.GetComponentInChildren<TextMeshProUGUI>();// Busca el componente de texto dentro del prefab para asignarle el texto de la entrada
        if (tmp != null)
            tmp.text = texto;// Asigna el texto a mostrar en la entrada
        else
            Debug.LogWarning("[Leaderboard] El prefab no tiene TextMeshProUGUI");
    }
}