using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenu : MonoBehaviour
{
    [Tooltip("Nombre de la escena del nivel que se cargará al presionar Jugar")]
    public string levelToLoad = "SampleScene"; // cámbialo por el nombre de tu escena del juego

    // Método para el botón Jugar
    public void PlayGame()
    {
        SceneManager.LoadScene(levelToLoad);
    }

    // Método para el botón Salir
    public void QuitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false; // Detiene el juego si estás en el editor
#else
        Application.Quit(); // Cierra el juego compilado
#endif
    }
}
