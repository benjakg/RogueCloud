using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenu : MonoBehaviour
{
    [Tooltip("Nombre de la escena del nivel que se cargará al presionar Jugar")]
    public string levelToLoad = "LevelOne"; // Nombre de la escena del juego

    // Método para el botón Jugar
    public void PlayGame()
    {
        Debug.Log("Cargando escena: " + levelToLoad);
        SceneManager.LoadScene(levelToLoad);
    }

    // Método para el botón Salir
    public void QuitGame()
    {
        Debug.Log("Botón Salir presionado!");
        
#if UNITY_EDITOR
        // Detiene el juego si estás en el editor
        Debug.Log("Deteniendo juego en el editor...");
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // Cierra el juego compilado
        Debug.Log("Cerrando aplicación...");
        Application.Quit();
        
        // Forzar cierre en caso de que Application.Quit() no funcione en algunas plataformas
        #if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
            System.Environment.Exit(0);
        #endif
#endif
    }
}
