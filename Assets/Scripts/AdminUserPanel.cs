using UnityEngine;

/// <summary>
/// Panel de administración / debug — SOLO BACKEND, nunca visible al jugador.
/// 
/// CÓMO USAR:
///   1. Adjunta este script a cualquier GameObject vacío en la escena.
///   2. En Play Mode, presiona las teclas de acceso rápido (ver abajo).
///   3. Todo se imprime en la Consola de Unity (Window > General > Console).
///   4. Antes de publicar, deshabilita este GameObject o elimina el script.
///
/// ATAJOS DE TECLADO (solo en el Editor / builds de desarrollo):
///   F1  — Listar TODOS los usuarios con su info completa
///   F2  — Mostrar Top 10 Global
///   F3  — Mostrar Top 10 Minijuego 1
///   F4  — Mostrar historial de partidas del usuario activo
///   F5  — Mostrar resumen rápido (cuántos usuarios hay)
///   F9  — Borrar TODOS los datos (¡cuidado!)
/// </summary>
public class AdminUserPanel : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD

    // ── Colores ANSI para la consola (hacen más legible el log) ───────────
    private const string C_HEADER  = "<color=#00FFAA><b>";
    private const string C_LABEL   = "<color=#FFD700>";
    private const string C_VALUE   = "<color=#FFFFFF>";
    private const string C_WARN    = "<color=#FF6B6B><b>";
    private const string C_RESET   = "</color>";
    private const string C_BRESET  = "</b></color>";

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1)) MostrarTodosLosUsuarios();
        if (Input.GetKeyDown(KeyCode.F2)) MostrarTopGlobal();
        if (Input.GetKeyDown(KeyCode.F3)) MostrarTopMinijuego(UserManager.MINIJUEGO_1);
        if (Input.GetKeyDown(KeyCode.F4)) MostrarHistorialUsuarioActivo();
        if (Input.GetKeyDown(KeyCode.F5)) MostrarResumen();
        if (Input.GetKeyDown(KeyCode.F9)) ConfirmarBorradoTotal();
    }

    // ══════════════════════════════════════════════════════════════════════
    // F1 — TODOS los usuarios con info completa
    // ══════════════════════════════════════════════════════════════════════
    void MostrarTodosLosUsuarios()
    {
        if (!CheckUserManager()) return;

        UserData[] todos = UserManager.Instance.GetTopGlobal(9999); // truco: pide un tope enorme para obtenerlos todos
        // Nota: GetTopGlobal los ordena por score; si quieres sin orden usa GetTopByMiniGame con un nombre inexistente, pero
        // es más limpio añadir un método GetAll() en UserManager (ver comentario al final del archivo).

        var sb = new System.Text.StringBuilder();
        sb.AppendLine(Titulo("═══ ADMIN: LISTA COMPLETA DE USUARIOS ═══"));
        sb.AppendLine(Label("Total de usuarios: ") + Val(todos.Length.ToString()));
        sb.AppendLine(Separador());

        if (todos.Length == 0)
        {
            sb.AppendLine(Warn("No hay usuarios registrados."));
        }
        else
        {
            for (int i = 0; i < todos.Length; i++)
            {
                UserData u = todos[i];
                sb.AppendLine(Titulo($"  [{i + 1}] {u.username}"));
                sb.AppendLine(Label("      highScore    : ") + Val(u.highScore.ToString()));
                sb.AppendLine(Label("      lastScore    : ") + Val(u.lastScore.ToString()));
                sb.AppendLine(Label("      globalScore  : ") + Val(u.globalScore.ToString()));
                sb.AppendLine(Label("      gamesPlayed  : ") + Val(u.gamesPlayed.ToString()));
                sb.AppendLine(Label("      lastPlayed   : ") + Val(u.lastPlayed));

                // Registros por minijuego
                if (u.miniGameRecords != null && u.miniGameRecords.Count > 0)
                {
                    sb.AppendLine(Label("      miniGames:"));
                    foreach (var r in u.miniGameRecords)
                        sb.AppendLine($"          • {r.minigameName}: {r.accumulatedScore} pts acumulados, {r.timesPlayed} veces jugado");
                }
                else
                {
                    sb.AppendLine(Label("      miniGames    : ") + Val("(ninguno aún)"));
                }

                // Historial de pila
                if (u.scoreHistory != null && !u.scoreHistory.IsEmpty())
                {
                    sb.AppendLine(Label("      historial (LIFO):"));
                    ScoreHistoryNode[] hist = u.scoreHistory.ToArray();
                    for (int h = 0; h < hist.Length; h++)
                        sb.AppendLine($"          {h + 1}. {hist[h].minigameName} — {hist[h].score} pts — {hist[h].date}");
                }
                else
                {
                    sb.AppendLine(Label("      historial    : ") + Val("(vacío)"));
                }

                sb.AppendLine(Separador());
            }
        }

        Debug.Log(sb.ToString());
    }

    // ══════════════════════════════════════════════════════════════════════
    // F2 — TOP 10 GLOBAL
    // ══════════════════════════════════════════════════════════════════════
    void MostrarTopGlobal()
    {
        if (!CheckUserManager()) return;

        UserData[] top = UserManager.Instance.GetTopGlobal(10);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(Titulo("═══ ADMIN: TOP 10 GLOBAL ═══"));

        if (top.Length == 0)
        {
            sb.AppendLine(Warn("No hay usuarios registrados."));
        }
        else
        {
            for (int i = 0; i < top.Length; i++)
                sb.AppendLine($"  {Medalla(i + 1)} {Label(top[i].username.PadRight(20))} {Val(top[i].globalScore + " pts")}");
        }

        Debug.Log(sb.ToString());
    }

    // ══════════════════════════════════════════════════════════════════════
    // F3 — TOP 10 por Minijuego
    // ══════════════════════════════════════════════════════════════════════
    void MostrarTopMinijuego(string minijuego)
    {
        if (!CheckUserManager()) return;

        UserData[] top = UserManager.Instance.GetTopByMiniGame(minijuego, 10);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(Titulo($"═══ ADMIN: TOP 10 — {minijuego} ═══"));

        if (top.Length == 0)
        {
            sb.AppendLine(Warn("No hay datos para este minijuego."));
        }
        else
        {
            for (int i = 0; i < top.Length; i++)
            {
                int score = top[i].GetMiniGameScore(minijuego);
                sb.AppendLine($"  {Medalla(i + 1)} {Label(top[i].username.PadRight(20))} {Val(score + " pts acumulados")}");
            }
        }

        Debug.Log(sb.ToString());
    }

    // ══════════════════════════════════════════════════════════════════════
    // F4 — Historial del usuario activo (pila LIFO)
    // ══════════════════════════════════════════════════════════════════════
    void MostrarHistorialUsuarioActivo()
    {
        if (!CheckUserManager()) return;

        UserData user = UserManager.Instance.CurrentUser;

        if (user == null)
        {
            Debug.Log(Warn("No hay usuario logueado actualmente."));
            return;
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine(Titulo($"═══ ADMIN: HISTORIAL DE {user.username.ToUpper()} ═══"));
        sb.AppendLine(Label("globalScore  : ") + Val(user.globalScore.ToString()));
        sb.AppendLine(Label("highScore    : ") + Val(user.highScore.ToString()));
        sb.AppendLine(Label("gamesPlayed  : ") + Val(user.gamesPlayed.ToString()));
        sb.AppendLine(Separador());

        if (user.scoreHistory == null || user.scoreHistory.IsEmpty())
        {
            sb.AppendLine(Warn("La pila de historial está vacía."));
        }
        else
        {
            sb.AppendLine(Label("Pila de partidas (tope = más reciente):"));
            ScoreHistoryNode[] hist = user.scoreHistory.ToArray();
            for (int i = 0; i < hist.Length; i++)
                sb.AppendLine($"  [{i + 1}]  {hist[i].minigameName,-20} {hist[i].score,6} pts   {hist[i].date}");
        }

        Debug.Log(sb.ToString());
    }

    // ══════════════════════════════════════════════════════════════════════
    // F5 — Resumen rápido
    // ══════════════════════════════════════════════════════════════════════
    void MostrarResumen()
    {
        if (!CheckUserManager()) return;

        UserData[] todos = UserManager.Instance.GetTopGlobal(9999);
        string usuarioActivo = UserManager.Instance.CurrentUser != null
            ? UserManager.Instance.CurrentUser.username
            : "(ninguno)";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine(Titulo("═══ ADMIN: RESUMEN ═══"));
        sb.AppendLine(Label("Usuarios registrados : ") + Val(todos.Length.ToString()));
        sb.AppendLine(Label("Usuario activo       : ") + Val(usuarioActivo));
        sb.AppendLine(Label("Archivo JSON en      : ") + Val(GetFilePath()));

        Debug.Log(sb.ToString());
    }

    // ══════════════════════════════════════════════════════════════════════
    // F9 — Borrar todos los datos (con confirmación)
    // ══════════════════════════════════════════════════════════════════════
    private bool esperandoConfirmacion = false;

    void ConfirmarBorradoTotal()
    {
        if (!esperandoConfirmacion)
        {
            esperandoConfirmacion = true;
            Debug.LogWarning("[ADMIN] ⚠ Presiona F9 de nuevo en los próximos 3 segundos para BORRAR TODOS LOS DATOS. Presiona cualquier otra tecla para cancelar.");
            Invoke(nameof(CancelarBorrado), 3f);
        }
        else
        {
            CancelInvoke(nameof(CancelarBorrado));
            esperandoConfirmacion = false;
            BorrarTodosLosDatos();
        }
    }

    void CancelarBorrado()
    {
        esperandoConfirmacion = false;
        Debug.Log("[ADMIN] Borrado cancelado.");
    }

    void BorrarTodosLosDatos()
    {
        string path = GetFilePath();
        if (System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
            Debug.LogWarning("[ADMIN] ✓ Archivo de usuarios ELIMINADO. Reinicia el juego para comenzar limpio.");
        }
        else
        {
            Debug.Log("[ADMIN] No había archivo que borrar.");
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // Helpers internos
    // ══════════════════════════════════════════════════════════════════════

    bool CheckUserManager()
    {
        if (UserManager.Instance == null)
        {
            Debug.LogWarning("[ADMIN] UserManager.Instance es null. ¿Está en la escena?");
            return false;
        }
        return true;
    }

    string GetFilePath()
    {
        return System.IO.Path.Combine(Application.persistentDataPath, "usuarios.json");
    }

    string Titulo(string texto)  => $"{C_HEADER}{texto}{C_BRESET}";
    string Label(string texto)   => $"{C_LABEL}{texto}{C_RESET}";
    string Val(string texto)     => $"{C_VALUE}{texto}{C_RESET}";
    string Warn(string texto)    => $"{C_WARN}{texto}{C_BRESET}";
    string Separador()           => Label("  ─────────────────────────────────────");

    string Medalla(int pos) => pos switch
    {
        1 => "🥇",
        2 => "🥈",
        3 => "🥉",
        _ => $" {pos}."
    };

#endif

    // ──────────────────────────────────────────────────────────────────────
    // NOTA: Para obtener todos los usuarios sin ordenar, puedes añadir este
    // método público en UserManager.cs:
    //
    //   public UserData[] GetAll() => userList.ToArray();
    //
    // y luego usarlo aquí como:
    //   UserData[] todos = UserManager.Instance.GetAll();
    // ──────────────────────────────────────────────────────────────────────
}
