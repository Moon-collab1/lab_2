using UnityEngine;
using System.IO;

// ══════════════════════════════════════════════════════════════════════════════
// TABLA HASH MANUAL DE USUARIOS
// Estructura de datos para acceso O(1) promedio por nombre de usuario.
//
// USOS EN EL JUEGO:
//   1. Busqueda rapida de usuarios al hacer Login (Search en O(1) vs O(n) lista).
//   2. Sincronizacion automatica con la lista enlazada al guardar/cargar.
//   3. Volcado de snapshot en "hashtable_debug.json" para inspeccion externa.
//
// Integracion:
//   - Adjunta este componente al mismo GameObject que UserManager, o a uno
//     separado con DontDestroyOnLoad.
//   - UserManager llama Rebuild() cada vez que la lista de usuarios cambia.
// ══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Entrada individual de la tabla hash (lista de colisiones por encadenamiento).
/// </summary>
public class HashEntry
{
    public string    username;
    public UserData  userData;
    public HashEntry next;   // encadenamiento para colisiones

    public HashEntry(string username, UserData userData)
    {
        this.username = username;
        this.userData = userData;
        this.next     = null;
    }
}

/// <summary>
/// Tabla hash de capacidad fija con encadenamiento de colisiones.
/// Capacidad por defecto: 64 cubetas.
/// </summary>
public class UserHashTable : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────────────────────
    public static UserHashTable Instance { get; private set; }

    // ── Configuracion ──────────────────────────────────────────────────────
    [Tooltip("Numero de cubetas de la tabla hash (potencia de 2 recomendada)")]
    public int capacity = 64;

    // ── Tabla interna ──────────────────────────────────────────────────────
    private HashEntry[] buckets;
    private int         count;

    // ── Ruta del archivo JSON de debug ─────────────────────────────────────
    private string filePath;

    // ──────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        filePath = Path.Combine("C:/Users/samue/proyectos unity/Experimentode lab2/Assets/Datos", "hashtable_debug.json");
        InitTable();
    }

    // ── Inicializar cubetas vacias ─────────────────────────────────────────
    private void InitTable()
    {
        buckets = new HashEntry[capacity];
        for (int i = 0; i < capacity; i++) buckets[i] = null;
        count = 0;
    }

    // ══════════════════════════════════════════════════════════════════════
    // API PUBLICA
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Reconstruye la tabla desde un array de UserData y vuelca el JSON.
    /// Llama a esto cada vez que la lista de usuarios cambia.
    /// </summary>
    public void Rebuild(UserData[] users)
    {
        InitTable();
        if (users == null) return;

        foreach (var u in users)
            Insert(u.username, u);

        DumpToJson();
        Debug.Log($"[HashTable] Tabla reconstruida: {count} usuarios, {capacity} cubetas. JSON en: {filePath}");
    }

    /// <summary>Inserta o actualiza un usuario en la tabla hash.</summary>
    public void Insert(string username, UserData userData)
    {
        int index = GetBucket(username);

        // Buscar si ya existe para actualizar
        HashEntry current = buckets[index];
        while (current != null)
        {
            if (current.username == username) { current.userData = userData; return; }
            current = current.next;
        }

        // Nuevo nodo al frente de la cubeta (encadenamiento)
        HashEntry newEntry = new HashEntry(username, userData);
        newEntry.next  = buckets[index];
        buckets[index] = newEntry;
        count++;
    }

    /// <summary>
    /// Busca un usuario por nombre. O(1) promedio.
    /// Retorna null si no existe.
    /// </summary>
    public UserData Search(string username)
    {
        int index = GetBucket(username);
        HashEntry current = buckets[index];
        while (current != null)
        {
            if (current.username == username) return current.userData;
            current = current.next;
        }
        return null;
    }

    /// <summary>Verifica si un usuario existe en la tabla. O(1) promedio.</summary>
    public bool Exists(string username)
    {
        return Search(username) != null;
    }

    /// <summary>Elimina un usuario de la tabla hash.</summary>
    public bool Delete(string username)
    {
        int index = GetBucket(username);
        HashEntry current = buckets[index];
        HashEntry prev    = null;

        while (current != null)
        {
            if (current.username == username)
            {
                if (prev == null) buckets[index] = current.next;
                else              prev.next       = current.next;
                count--;
                return true;
            }
            prev    = current;
            current = current.next;
        }
        return false;
    }

    public int Count()    => count;
    public int Capacity() => capacity;

    // ══════════════════════════════════════════════════════════════════════
    // FUNCION HASH — djb2
    // Distribucion uniforme para nombres cortos de usuario.
    // ══════════════════════════════════════════════════════════════════════
    private int GetBucket(string key)
    {
        uint hash = 5381;
        foreach (char c in key)
            hash = ((hash << 5) + hash) + (uint)c;  // hash * 33 + c
        return (int)(hash % (uint)capacity);
    }

    // ══════════════════════════════════════════════════════════════════════
    // VOLCADO JSON — snapshot legible para inspeccion externa
    // ══════════════════════════════════════════════════════════════════════
    private void DumpToJson()
    {
        HashTableSnapshot snapshot = new HashTableSnapshot
        {
            capacity   = this.capacity,
            count      = this.count,
            loadFactor = (float)this.count / this.capacity,
            buckets    = new BucketSnapshot[this.capacity]
        };

        int usedBuckets      = 0;
        int maxChainLength   = 0;
        int totalChainLength = 0;

        for (int i = 0; i < capacity; i++)
        {
            BucketSnapshot bs = new BucketSnapshot { index = i, chain = new EntrySnapshot[0] };

            if (buckets[i] != null)
            {
                usedBuckets++;
                var chain = new System.Collections.Generic.List<EntrySnapshot>();

                HashEntry current = buckets[i];
                while (current != null)
                {
                    chain.Add(new EntrySnapshot
                    {
                        username    = current.userData.username,
                        highScore   = current.userData.highScore,
                        globalScore = current.userData.globalScore,
                        gamesPlayed = current.userData.gamesPlayed,
                        lastPlayed  = current.userData.lastPlayed,
                        hashValue   = GetBucket(current.username)
                    });
                    current = current.next;
                }

                bs.chain = chain.ToArray();
                if (chain.Count > maxChainLength) maxChainLength = chain.Count;
                totalChainLength += chain.Count;
            }

            snapshot.buckets[i] = bs;
        }

        snapshot.usedBuckets    = usedBuckets;
        snapshot.emptyBuckets   = capacity - usedBuckets;
        snapshot.maxChainLength = maxChainLength;
        snapshot.avgChainLength = usedBuckets > 0 ? (float)totalChainLength / usedBuckets : 0f;

        string json = JsonUtility.ToJson(snapshot, prettyPrint: true);
        File.WriteAllText(filePath, json);
    }

    // ── Clases serializables para el JSON de debug ─────────────────────────
    [System.Serializable] private class EntrySnapshot
    {
        public string username;
        public int    highScore;
        public int    globalScore;
        public int    gamesPlayed;
        public string lastPlayed;
        public int    hashValue;
    }

    [System.Serializable] private class BucketSnapshot
    {
        public int             index;
        public EntrySnapshot[] chain;
    }

    [System.Serializable] private class HashTableSnapshot
    {
        public int              capacity;
        public int              count;
        public float            loadFactor;
        public int              usedBuckets;
        public int              emptyBuckets;
        public int              maxChainLength;
        public float            avgChainLength;
        public BucketSnapshot[] buckets;
    }
}
