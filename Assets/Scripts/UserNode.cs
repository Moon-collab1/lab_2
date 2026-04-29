using System.Collections.Generic;

// ══════════════════════════════════════════════════════════════════════════════
// PILA MANUAL DE HISTORIAL DE SCORES
// Estructura LIFO: el ultimo puntaje guardado es el primero en consultarse.
// Implementada con nodos enlazados (sin usar Stack<T> de C#).
// ══════════════════════════════════════════════════════════════════════════════
public class ScoreHistoryNode
{
    public int   score;
    public string minigameName;
    public string date;
    public ScoreHistoryNode next;   // apunta al elemento debajo en la pila

    public ScoreHistoryNode(int score, string minigameName, string date)
    {
        this.score        = score;
        this.minigameName = minigameName;
        this.date         = date;
        this.next         = null;
    }
}

/// <summary>
/// Pila (Stack) manual de historial de partidas de un usuario.
/// Operaciones: Push, Pop, Peek, IsEmpty, ToArray.
/// Capacidad maxima configurable (por defecto 10 partidas).
/// </summary>
public class ScoreStack
{
    private ScoreHistoryNode top;   // tope de la pila
    private int size;
    private int maxSize;

    public ScoreStack(int maxSize = 10)
    {
        top           = null;
        size          = 0;
        this.maxSize  = maxSize;
    }

    /// <summary>Apila un nuevo score. Si llego al limite descarta el mas antiguo.</summary>
    public void Push(int score, string minigameName, string date)
    {
        ScoreHistoryNode newNode = new ScoreHistoryNode(score, minigameName, date);
        newNode.next = top;
        top          = newNode;
        size++;

        // Si supera el maximo, recortamos el nodo del fondo
        if (size > maxSize)
            RemoveBottom();
    }

    /// <summary>Extrae y retorna el elemento del tope. Retorna null si esta vacia.</summary>
    public ScoreHistoryNode Pop()
    {
        if (top == null) return null;

        ScoreHistoryNode popped = top;
        top  = top.next;
        size--;
        return popped;
    }

    /// <summary>Consulta el tope sin extraerlo. Retorna null si esta vacia.</summary>
    public ScoreHistoryNode Peek()
    {
        return top;
    }

    public bool IsEmpty() => size == 0;
    public int  Count()   => size;

    /// <summary>Retorna todos los elementos como array (tope primero).</summary>
    public ScoreHistoryNode[] ToArray()
    {
        ScoreHistoryNode[] arr     = new ScoreHistoryNode[size];
        ScoreHistoryNode   current = top;
        int i = 0;
        while (current != null)
        {
            arr[i] = current;
            current = current.next;
            i++;
        }
        return arr;
    }

    // ── Helper privado ─────────────────────────────────────────────────────
    private void RemoveBottom()
    {
        if (top == null) return;
        if (top.next == null) { top = null; size = 0; return; }

        ScoreHistoryNode current = top;
        while (current.next != null && current.next.next != null)
            current = current.next;

        current.next = null;
        size--;
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// TAD Usuario
// ══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// TAD Usuario — Define el tipo y sus operaciones basicas.
/// Cada nodo de la lista enlazada contiene un objeto de este tipo.
/// Incluye una pila de historial de partidas (ScoreStack).
/// </summary>
[System.Serializable]
public class UserData
{
    // ── Atributos basicos ──────────────────────────────────────────────────
    public string username;
    public int highScore;       // puntuacion mas alta registrada
    public int lastScore;       // puntuacion de la ultima partida guardada
    public int gamesPlayed;
    public string lastPlayed;   // fecha de ultima partida

    // ── Score global acumulado (suma de todas las partidas de todos los minijuegos) ──
    public int globalScore;

    // ── Registros por minijuego (para el leaderboard) ─────────────────────
    public List<MiniGameRecord> miniGameRecords;

    // ── Pila de historial de partidas (estructura de datos #2) ────────────
    // Almacena las ultimas 10 partidas; la mas reciente esta al tope.
    [System.NonSerialized]
    public ScoreStack scoreHistory;

    // ── Constructor ────────────────────────────────────────────────────────
    public UserData(string username)
    {
        this.username        = username;
        this.highScore       = 0;
        this.lastScore       = 0;
        this.gamesPlayed     = 0;
        this.globalScore     = 0;
        this.lastPlayed      = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        this.miniGameRecords = new List<MiniGameRecord>();
        this.scoreHistory    = new ScoreStack(10);
    }

    // ── Operaciones del TAD ────────────────────────────────────────────────

    /// <summary>
    /// Guarda la puntuacion de una partida.
    /// - Actualiza highScore si el nuevo score es mayor.
    /// - Acumula en el minijuego correspondiente.
    /// - Suma al score global.
    /// - Apila el score en el historial (LIFO).
    /// </summary>
    public void SaveScore(int score, string minigameName = "Minijuego1")
    {
        lastScore    = score;
        gamesPlayed++;
        globalScore += score;
        lastPlayed   = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");

        if (score > highScore)
            highScore = score;

        // Acumular en el registro del minijuego
        MiniGameRecord record = GetOrCreateRecord(minigameName);
        record.accumulatedScore += score;
        record.timesPlayed++;

        // Apilar en el historial (estructura LIFO)
        if (scoreHistory == null) scoreHistory = new ScoreStack(10);
        scoreHistory.Push(score, minigameName, lastPlayed);
    }

    /// <summary>Retorna el score acumulado de un minijuego especifico.</summary>
    public int GetMiniGameScore(string minigameName)
    {
        foreach (var r in miniGameRecords)
            if (r.minigameName == minigameName)
                return r.accumulatedScore;
        return 0;
    }

    /// <summary>Retorna el puntaje de la partida mas reciente (tope de la pila).</summary>
    public int GetLastScoreFromHistory()
    {
        if (scoreHistory == null || scoreHistory.IsEmpty()) return 0;
        return scoreHistory.Peek().score;
    }

    /// <summary>Retorna un resumen del usuario como string.</summary>
    public string GetSummary()
    {
        return $"Usuario: {username} | Mejor puntaje: {highScore} | Ultima partida: {lastScore} | Partidas jugadas: {gamesPlayed}";
    }

    // ── Helper privado ─────────────────────────────────────────────────────
    private MiniGameRecord GetOrCreateRecord(string minigameName)
    {
        foreach (var r in miniGameRecords)
            if (r.minigameName == minigameName)
                return r;

        MiniGameRecord newRecord = new MiniGameRecord(minigameName);
        miniGameRecords.Add(newRecord);
        return newRecord;
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// Registro individual de un minijuego
// ══════════════════════════════════════════════════════════════════════════════
[System.Serializable]
public class MiniGameRecord
{
    public string minigameName;
    public int accumulatedScore;   // suma de TODAS las partidas de ese minijuego
    public int timesPlayed;

    public MiniGameRecord(string name)
    {
        minigameName     = name;
        accumulatedScore = 0;
        timesPlayed      = 0;
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// Nodo de la lista enlazada
// ══════════════════════════════════════════════════════════════════════════════
public class UserNode
{
    public UserData data;
    public UserNode next;

    public UserNode(UserData data)
    {
        this.data = data;
        this.next = null;
    }
}
