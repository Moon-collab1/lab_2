using System.Collections.Generic;

/// <summary>
/// TAD Usuario — Define el tipo y sus operaciones basicas.
/// Cada nodo de la lista enlazada contiene un objeto de este tipo.
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
    }

    // ── Operaciones del TAD ────────────────────────────────────────────────

    /// <summary>
    /// Guarda la puntuacion de una partida.
    /// - Actualiza highScore si el nuevo score es mayor.
    /// - Acumula en el minijuego correspondiente.
    /// - Suma al score global.
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
    }

    /// <summary>Retorna el score acumulado de un minijuego especifico.</summary>
    public int GetMiniGameScore(string minigameName)
    {
        foreach (var r in miniGameRecords)
            if (r.minigameName == minigameName)
                return r.accumulatedScore;
        return 0;
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
