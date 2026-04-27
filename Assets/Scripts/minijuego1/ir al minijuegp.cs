using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTrigger : MonoBehaviour
{
    [Header("Escena a cargar")]
    public string nextSceneName = "minijuego1";

    void OnTriggerEnter(Collider other)
    {
        // Verifica si el que entro es el jugador
        if (other.CompareTag("Player"))
        {
            LoadNextScene();
        }
    }

    void LoadNextScene()
    {
        SceneManager.LoadScene(nextSceneName);
    }
}