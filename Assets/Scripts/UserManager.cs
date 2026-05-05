using UnityEngine;
using System.IO;

/// <summary>
/// Singleton que maneja la lista enlazada de usuarios y la persistencia en JSON.
/// Persiste entre escenas con DontDestroyOnLoad.
///
/// TABLA HASH:
///   - Se usa para Login/Registro: busqueda O(1) en vez de O(n) con la lista.
///   - Se sincroniza automaticamente al guardar y al cargar.
///   - Requiere que UserHashTable este presente en la escena (mismo GameObject
///     o uno separado). Si no existe, el Login cae en fallback a la lista enlazada.
/// </summary>
public class UserManager : MonoBehaviour
{
    public static UserManager Instance { get; private set; }

    // ── Nombres de minijuegos (constantes para usar en GameManager) ────────
    public const string MINIJUEGO_1 = "Minijuego1";
    // public const string MINIJUEGO_2 = "Minijuego2";

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

        filePath = Path.Combine("C:/Users/samue/proyectos unity/Experimentode lab2/Assets/Datos", "usuarios.json");
        LoadFromFile();
    }

    // ══════════════════════════════════════════════════════════════════════
    // LOGIN / REGISTRO — usa la tabla hash para busqueda O(1)
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Intenta loguear un usuario existente o crea uno nuevo.
    /// Retorna true si el usuario ya existia, false si es nuevo.
    ///
    /// Busqueda: primero intenta O(1) en la tabla hash;
    /// si la tabla no esta disponible cae en O(n) en la lista enlazada.
    /// </summary>
    public bool LoginOrRegister(string username)
    {
        username = username.Trim();
        if (string.IsNullOrEmpty(username)) return false;

        // ── Busqueda en tabla hash O(1) ────────────────────────────────────
        UserData found = HashSearch(username);

        if (found != null)
        {
            CurrentUser = found;
            Debug.Log("[UserManager] (HashTable hit) Usuario cargado: " + CurrentUser.GetSummary());
            return true;
        }

        // ── Fallback: busqueda en lista enlazada O(n) ──────────────────────
        if (userList.Exists(username))
        {
            CurrentUser = userList.Find(username);
            Debug.Log("[UserManager] (LinkedList fallback) Usuario cargado: " + CurrentUser.GetSummary());
            return true;
        }

        // ── Usuario nuevo ──────────────────────────────────────────────────
        UserData newUser = new UserData(username);
        userList.Add(newUser);
        CurrentUser = newUser;

        // Insertar directamente en la tabla hash sin reconstruirla entera
        if (UserHashTable.Instance != null)
            UserHashTable.Instance.Insert(username, newUser);

        SaveToFile();
        Debug.Log("[UserManager] Nuevo usuario registrado: " + username);
        return false;
    }

    // ── Busqueda auxiliar en tabla hash ────────────────────────────────────
    private UserData HashSearch(string username)
    {
        if (UserHashTable.Instance == null) return null;
        return UserHashTable.Instance.Search(username);
    }

    // ══════════════════════════════════════════════════════════════════════
    // GUARDAR PUNTAJE
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Guarda el score de la partida actual.
    /// minigameName: usa UserManager.MINIJUEGO_1 (o la constante del minijuego).
    /// </summary>
    public void SaveCurrentScore(int score, string minigameName = MINIJUEGO_1)
    {
        if (CurrentUser == null) return;

        CurrentUser.SaveScore(score, minigameName);

        // Actualizar entrada en la tabla hash (el objeto ya esta referenciado,
        // pero llamamos Insert para asegurar que el puntero es el correcto)
        if (UserHashTable.Instance != null)
            UserHashTable.Instance.Insert(CurrentUser.username, CurrentUser);

        SaveToFile();
        Debug.Log($"[UserManager] Puntaje guardado: {score} en {minigameName} para {CurrentUser.username}");
        LogScoreHistory(CurrentUser);
    }

    // ══════════════════════════════════════════════════════════════════════
    // CERRAR SESION
    // ══════════════════════════════════════════════════════════════════════

    public void Logout()
    {
        CurrentUser = null;
    }

    // ══════════════════════════════════════════════════════════════════════
    // LEADERBOARD
    // ══════════════════════════════════════════════════════════════════════

    public UserData[] GetTopGlobal(int top = 10)
    {
        return userList.GetTopByGlobalScore(top);
    }

    public UserData[] GetTopByMiniGame(string minigameName, int top = 10)
    {
        return userList.GetTopByMiniGame(minigameName, top);
    }

    // ══════════════════════════════════════════════════════════════════════
    // PERSISTENCIA JSON
    // ══════════════════════════════════════════════════════════════════════

    public void SaveToFile()
    {
        UserDataSerializable[] serializableUsers = ToSerializableArray();
        UserListWrapper wrapper = new UserListWrapper { users = serializableUsers };
        string json = JsonUtility.ToJson(wrapper, prettyPrint: true);
        File.WriteAllText(filePath, json);
        Debug.Log("[UserManager] Guardado en: " + filePath);

        // Reconstruir tabla hash completa al guardar
        if (UserHashTable.Instance != null)
            UserHashTable.Instance.Rebuild(userList.ToArray());
    }

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
                UserData userData    = new UserData(u.username);
                userData.highScore   = u.highScore;
                userData.lastScore   = u.lastScore;
                userData.gamesPlayed = u.gamesPlayed;
                userData.lastPlayed  = u.lastPlayed;
                userData.globalScore = u.globalScore;

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

                // Reconstruir pila de historial desde JSON
                if (u.scoreHistory != null)
                {
                    userData.scoreHistory = new ScoreStack(10);
                    for (int i = u.scoreHistory.Length - 1; i >= 0; i--)
                    {
                        var entry = u.scoreHistory[i];
                        userData.scoreHistory.Push(entry.score, entry.minigameName, entry.date);
                    }
                }

                userList.Add(userData);
            }
            Debug.Log("[UserManager] " + userList.Count() + " usuarios cargados.");

            // Sincronizar tabla hash al cargar
            if (UserHashTable.Instance != null)
                UserHashTable.Instance.Rebuild(userList.ToArray());
        }
    }

    // ── Debug: imprime historial de la pila ────────────────────────────────
    private void LogScoreHistory(UserData user)
    {
        if (user.scoreHistory == null || user.scoreHistory.IsEmpty()) return;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"[UserManager] Historial de {user.username} (tope primero):");

        ScoreHistoryNode[] history = user.scoreHistory.ToArray();
        for (int i = 0; i < history.Length; i++)
            sb.AppendLine($"  {i + 1}. {history[i].minigameName} — {history[i].score} pts — {history[i].date}");

        Debug.Log(sb.ToString());
    }

    // ══════════════════════════════════════════════════════════════════════
    // CLASES SERIALIZABLES
    // ══════════════════════════════════════════════════════════════════════

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
        public string                          username;
        public int                             highScore;
        public int                             lastScore;
        public int                             gamesPlayed;
        public string                          lastPlayed;
        public int                             globalScore;
        public MiniGameRecordSerializable[]    miniGameRecords;
        public ScoreHistoryEntrySerializable[] scoreHistory;
    }

    [System.Serializable]
    private class UserListWrapper
    {
        public UserDataSerializable[] users;
    }

    // ── Helper de serializacion ────────────────────────────────────────────
    private UserDataSerializable[] ToSerializableArray()
    {
        UserData[] arr    = userList.ToArray();
        var result        = new UserDataSerializable[arr.Length];

        for (int i = 0; i < arr.Length; i++)
        {
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
