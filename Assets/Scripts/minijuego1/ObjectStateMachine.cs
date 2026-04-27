using System.Collections;
using UnityEngine;

public class ObjectStateMachine : MonoBehaviour
{
    // ── State ──────────────────────────────────────────────────────────────
    public bool isFacingPlayer { get; private set; } = false;

    [Header("Rotation Settings")]
    [Tooltip("Seconds it takes to rotate 180°")]
    public float rotationDuration = 0.5f;

    [Header("Random Check Interval")]
    [Tooltip("How often (seconds) the object rolls to decide whether to face the player")]
    public float checkInterval = 2f;

    private bool isRotating = false;
    private Quaternion facingRotation;     // 180° → looking at player
    private Quaternion originalRotation;   // 0°  → looking away

    // ── Events (listened to by GameManager) ───────────────────────────────
    public event System.Action OnStartedFacingPlayer;

    // ──────────────────────────────────────────────────────────────────────
    void Start()
    {
        originalRotation = transform.rotation;
        facingRotation   = transform.rotation * Quaternion.Euler(0f, 180f, 0f);

        StartCoroutine(RandomCheckLoop());
    }

    // ── Random loop ────────────────────────────────────────────────────────
    IEnumerator RandomCheckLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkInterval);

            // Only try to face the player if currently looking away
            if (!isFacingPlayer && !isRotating)
            {
                int roll = Random.Range(1, 11); // 1–10 inclusive
                bool isEven = (roll % 2 == 0);

                if (isEven)
                {
                    // Even → face the player
                    StartCoroutine(RotateTo(facingRotation, becomesFacing: true));
                }
                // Odd  → stay the same (do nothing)
            }
        }
    }

    // ── Called by the canvas button ────────────────────────────────────────
    /// <summary>
    /// The player clicked the button. If the object is facing the player,
    /// rotate it back to the original (safe) position.
    /// </summary>
    public void OnPlayerClickButton()
    {
        if (isFacingPlayer && !isRotating)
        {
            StartCoroutine(RotateTo(originalRotation, becomesFacing: false));
        }
    }

    // ── Smooth rotation coroutine ──────────────────────────────────────────
    IEnumerator RotateTo(Quaternion target, bool becomesFacing)
    {
        isRotating = true;
        Quaternion start = transform.rotation;
        float elapsed = 0f;

        while (elapsed < rotationDuration)
        {
            elapsed += Time.deltaTime;
            transform.rotation = Quaternion.Slerp(start, target, elapsed / rotationDuration);
            yield return null;
        }

        transform.rotation = target;
        isFacingPlayer     = becomesFacing;
        isRotating         = false;

        if (isFacingPlayer)
            OnStartedFacingPlayer?.Invoke();
    }
}
