using UnityEngine;

/// <summary>
/// Lista enlazada simple implementada manualmente.
/// Almacena nodos de tipo UserNode, cada uno con la info de un usuario.
/// </summary>
public class UserLinkedList
{
    private UserNode head;
    private int size;

    public UserLinkedList()
    {
        head = null;
        size = 0;
    }

    // ── Agregar usuario al final de la lista ───────────────────────────────
    public void Add(UserData userData)
    {
        UserNode newNode = new UserNode(userData);

        if (head == null)
        {
            head = newNode;
        }
        else
        {
            UserNode current = head;
            while (current.next != null)// Recorremos hasta el final
                current = current.next;
            current.next = newNode;
        }

        size++;// Incrementamos el tamaño de la lista
    }

    // ── Buscar usuario por nombre ──────────────────────────────────────────
    public UserData Find(string username)
    {
        UserNode current = head;
        while (current != null)
        {
            if (current.data.username == username)
                return current.data;
            current = current.next;
        }
        return null;
    }

    // ── Verificar si un usuario existe ────────────────────────────────────
    public bool Exists(string username)
    {
        return Find(username) != null;
    }

    // ── Eliminar usuario por nombre ───────────────────────────────────────
    public bool Remove(string username)
    {
        if (head == null) return false;

        if (head.data.username == username)
        {
            head = head.next;
            size--;
            return true;
        }

        UserNode current = head;
        while (current.next != null)
        {
            if (current.next.data.username == username)
            {
                current.next = current.next.next;
                size--;
                return true;
            }
            current = current.next;
        }

        return false;
    }

    // ── Obtener todos los usuarios como array (para serializar a JSON) ─────
    public UserData[] ToArray()
    {
        UserData[] arr = new UserData[size];
        UserNode current = head;
        int i = 0;

        while (current != null)
        {
            arr[i] = current.data;
            current = current.next;
            i++;
        }

        return arr;
    }

    // ── Construir lista desde array (al cargar desde JSON) ─────────────────
    public void FromArray(UserData[] arr)// Reinicia la lista y agrega cada usuario del array
    {
        head = null;
        size = 0;
        foreach (var userData in arr)
            Add(userData);
    }

    public int Count() => size;

    // ══════════════════════════════════════════════════════════════════════
    // LEADERBOARD — Top N por score global
    // Bubble sort manual descendente (sin LINQ ni Array.Sort)
    // ══════════════════════════════════════════════════════════════════════
    public UserData[] GetTopByGlobalScore(int top = 10)
    {
        UserData[] arr = ToArray();

        // Bubble sort descendente por globalScore
        for (int i = 0; i < arr.Length - 1; i++)
        {
            for (int j = 0; j < arr.Length - i - 1; j++)
            {
                if (arr[j].globalScore < arr[j + 1].globalScore)
                {
                    UserData temp = arr[j];
                    arr[j]       = arr[j + 1];
                    arr[j + 1]   = temp;
                }
            }
        }

        int limit = Mathf.Min(top, arr.Length);
        UserData[] result = new UserData[limit];
        for (int i = 0; i < limit; i++)
            result[i] = arr[i];

        return result;
    }

    // ══════════════════════════════════════════════════════════════════════
    // LEADERBOARD — Top N por minijuego especifico (score acumulado)
    // ══════════════════════════════════════════════════════════════════════
    public UserData[] GetTopByMiniGame(string minigameName, int top = 10)
    {
        UserData[] arr = ToArray();// Convertimos la lista a array para ordenar

        // Bubble sort descendente por score acumulado del minijuego
        for (int i = 0; i < arr.Length - 1; i++)
        {
            for (int j = 0; j < arr.Length - i - 1; j++)
            {
                int scoreA = arr[j].GetMiniGameScore(minigameName);
                int scoreB = arr[j + 1].GetMiniGameScore(minigameName);

                if (scoreA < scoreB)
                {
                    UserData temp = arr[j];
                    arr[j]       = arr[j + 1];
                    arr[j + 1]   = temp;
                }
            }
        }

        int limit = Mathf.Min(top, arr.Length);
        UserData[] result = new UserData[limit];
        for (int i = 0; i < limit; i++)
            result[i] = arr[i];

        return result;
    }
}
