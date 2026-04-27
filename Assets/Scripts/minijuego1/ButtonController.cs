using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonController : MonoBehaviour
{
    [Header("References")]
    public ObjectStateMachine targetObject;
    public GameManager gameManager;

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(HandleClick);
    }

    void HandleClick()
    {
        if (targetObject == null)
        {
            Debug.LogError("ButtonController: targetObject no está asignado en el Inspector.", this);
            return;
        }
        if (gameManager == null)
        {
            Debug.LogError("ButtonController: gameManager no está asignado en el Inspector.", this);
            return;
        }

        if (!targetObject.isFacingPlayer) return;

        targetObject.OnPlayerClickButton();
        gameManager.OnPlayerReactedInTime();
    }
}