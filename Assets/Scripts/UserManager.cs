using UnityEngine;
using System.IO;

/// <summary>
/// Singleton que maneja la lista enlazada de usuarios y la persistencia en JSON.
/// Persiste entre escenas con DontDestroyOnLoad.
/// </summary>
public class UserManager : MonoBehaviour
{
    public static UserManager Instance { get; private set; }

    // ── Nombres de minijuegos (constantes para usar en GameManager) ────────
    public const string MINIJUEGO_1 = "Minijuego1";
    // public const string MINIJUEGO_2 = "Minijuego2"; // agrega mas cuando los tengas

    // ── Lista enlazada de usuarios ─────────────────────────────────────────
    private UserLinkedList userList = new UserLinkedList();

    // ── Usuario actualmente logueado ───────────────────────────────────────
    public UserData CurrentUser { get; private set; }

    // ── Ruta del archivo JSON ──────────────────────────────────────────────
    private string filePath;

    // ──────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        filePath = Path.Combine(Application.persistentDataPath, "usuarios.json");
        LoadFromFile();
    }

    // ── Login o registro ──────────────────────────────────────────────────
    /// <summary>
    /// Intenta loguear un usuario existente o crea uno nuevo.
    /// Retorna true si el usuario ya existia, false si es nuevo.
    /// </summary>
    public bool LoginOrRegister(string username)
    {
        username = username.Trim();
        if (string.IsNullOrEmpty(username)) return false;

        if (userList.Exists(username))
        {
            CurrentUser = userList.Find(username);
            Debug.Log("[UserManager] Usuario existente cargado: " + CurrentUser.GetSummary());
            return true;
        }
        else
        {
            UserData newUser = new UserData(username);
            userList.Add(newUser);
            CurrentUser = newUser;
            SaveToFile();
            Debug.Log("[UserManager] Nuevo usuario registrado: " + username);
            return false;
        }
    }

    // ── Guardar puntaje del usuario actual ────────────────────────────────
    /// <summary>
    /// Guarda el score de la partida actual.
    /// minigameName: usa UserManager.MINIJUEGO_1 (o la constante del minijuego).
    /// </summary>
    public void SaveCurrentScore(int score, string minigameName = MINIJUEGO_1)
    {
        if (CurrentUser == null) return;

        CurrentUser.SaveScore(score, minigameName);
        SaveToFile();
        Debug.Log($"[UserManager] Puntaje guardado: {score} en {minigameName} para {CurrentUser.username}");

        // Log de las ultimas partidas desde la pila
        LogScoreHistory(CurrentUser);
    }

    // ── Cerrar sesion ─────────────────────────────────────────────────────
    public void Logout()
    {
        CurrentUser = null;
    }

    // ── Leaderboard: top global ───────────────────────────────────────────
    public UserData[] GetTopGlobal(int top = 10)
    {
        return userList.GetTopByGlobalScore(top);
    }

    // ── Leaderboard: top por minijuego ────────────────────────────────────
    public UserData[] GetTopByMiniGame(string minigameName, int top = 10)
    {
        return userList.GetTopByMiniGame(minigameName, top);
    }

    // ── Guardar lista en JSON ─────────────────────────────────────────────
    public void SaveToFile()
    {
        UserDataSerializable[] serializableUsers = ToSerializableArray();
        UserListWrapper wrapper = new UserListWrapper { users = serializableUsers };
        string json = JsonUtility.ToJson(wrapper, prettyPrint: true);
        File.WriteAllText(filePath, json);
        Debug.Log("[UserManager] Guardado en: " + filePath);
    }

    // ── Cargar lista desde JSON ───────────────────────────────────────────
    public void LoadFromFile()
    {
        if (!File.Exists(filePath))
        {
            Debug.Log("[UserManager] No existe archivo de usuarios. Comenzando vacio.");
            return;
        }

        string json = File.ReadAllText(filePath);
        UserListWrapper wrapper = JsonUtility.FromJson<UserListWrapper>(json);

        if (wrapper != null && wrapper.users != null)
        {
            userList = new UserLinkedList();
            foreach (var u in wrapper.users)
            {
                UserData userData       = new UserData(u.username);
                userData.highScore      = u.highScore;
                userData.lastScore      = u.lastScore;
                userData.gamesPlayed    = u.gamesPlayed;
                userData.lastPlayed     = u.lastPlayed;
                userData.globalScore    = u.globalScore;

                // Cargar registros de minijuegos
                if (u.miniGameRecords != null)
                {
                    foreach (var r in u.miniGameRecords)
                    {
                        MiniGameRecord record = new MiniGameRecord(r.minigameName);
                        record.accumulatedScore = r.accumulatedScore;
                        record.timesPlayed      = r.timesPlayed;
                        userData.miniGameRecords.Add(record);
                    }
                }

                // Reconstruir la pila de historial desde el JSON
                if (u.scoreHistory != null)
                {
                    userData.scoreHistory = new ScoreStack(10);
                    // Los elementos vienen del tope al fondo; los insertamos al reves
                    // para que la pila quede en el orden correcto.
                    for (int i = u.scoreHistory.Length - 1; i >= 0; i--)
                    {
                        var entry = u.scoreHistory[i];
                        userData.scoreHistory.Push(entry.score, entry.minigameName, entry.date);
                    }
                }

                userList.Add(userData);
            }
            Debug.Log("[UserManager] " + userList.Count() + " usuarios cargados.");
        }
    }

    // ── Helper de debug: imprime el historial de la pila ─────────────────
    private void LogScoreHistory(UserData user)
    {
        if (user.scoreHistory == null || user.scoreHistory.IsEmpty()) return;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"[UserManager] Historial de partidas de {user.username} (tope primero):");

        ScoreHistoryNode[] history = user.scoreHistory.ToArray();
        for (int i = 0; i < history.Length; i++)
            sb.AppendLine($"  {i + 1}. {history[i].minigameName} — {history[i].score} pts — {history[i].date}");

        Debug.Log(sb.ToString());
    }

    // ── Clases serializables para JsonUtility ─────────────────────────────
    [System.Serializable]
    private class ScoreHistoryEntrySerializable
    {
        public int    score;
        public string minigameName;
        public string date;
    }

    [System.Serializable]
    private class MiniGameRecordSerializable
    {
        public string minigameName;
        public int    accumulatedScore;
        public int    timesPlayed;
    }

    [System.Serializable]
    private class UserDataSerializable
    {
        public string username;
        public int    highScore;
        public int    lastScore;
        public int    gamesPlayed;
        public string lastPlayed;
        public int    globalScore;
        public MiniGameRecordSerializable[]    miniGameRecords;
        public ScoreHistoryEntrySerializable[] scoreHistory;   // pila serializada
    }

    [System.Serializable]
    private class UserListWrapper
    {
        public UserDataSerializable[] users;
    }

    // ── Helper de serializacion ───────────────────────────────────────────
    private UserDataSerializable[] ToSerializableArray()
    {
        UserData[] arr    = userList.ToArray();
        UserDataSerializable[] result = new UserDataSerializable[arr.Length];

        for (int i = 0; i < arr.Length; i++)
        {
            // Serializar registros de minijuegos
            var mgRecords = new MiniGameRecordSerializable[arr[i].miniGameRecords.Count];
            for (int j = 0; j < arr[i].miniGameRecords.Count; j++)
            {
                mgRecords[j] = new MiniGameRecordSerializable
                {
                    minigameName     = arr[i].miniGameRecords[j].minigameName,
                    accumulatedScore = arr[i].miniGameRecords[j].accumulatedScore,
                    timesPlayed      = arr[i].miniGameRecords[j].timesPlayed
                };
            }

            // Serializar la pila de historial (tope primero)
            ScoreHistoryEntrySerializable[] historyArr = null;
            if (arr[i].scoreHistory != null)
            {
                ScoreHistoryNode[] nodes = arr[i].scoreHistory.ToArray();
                historyArr = new ScoreHistoryEntrySerializable[nodes.Length];
                for (int k = 0; k < nodes.Length; k++)
                {
                    historyArr[k] = new ScoreHistoryEntrySerializable
                    {
                        score        = nodes[k].score,
                        minigameName = nodes[k].minigameName,
                        date         = nodes[k].date
                    };
                }
            }

            result[i] = new UserDataSerializable
            {
                username        = arr[i].username,
                highScore       = arr[i].highScore,
                lastScore       = arr[i].lastScore,
                gamesPlayed     = arr[i].gamesPlayed,
                lastPlayed      = arr[i].lastPlayed,
                globalScore     = arr[i].globalScore,
                miniGameRecords = mgRecords,
                scoreHistory    = historyArr
            };
        }

        return result;
    }
}
