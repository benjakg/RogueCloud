using UnityEngine;

[System.Serializable]
public class PlayerSetup : MonoBehaviour
{
    [Header("Player Configuration")]
    public bool autoSetupPlayer = true;
    public bool addCapsuleCollider = true;
    public bool addRigidbody = true;
    public bool addPlayerMovement = true;
    
    [Header("Player Properties")]
    public float playerHeight = 2f;
    public float playerRadius = 0.5f;
    public float playerMass = 1f;
    
    void Start()
    {
        if (autoSetupPlayer)
        {
            SetupPlayer();
        }
    }
    
    [ContextMenu("Setup Player")]
    public void SetupPlayer()
    {
        // Verificar si este objeto se llama "Player" o "player"
        if (!gameObject.name.ToLower().Contains("player"))
        {
            Debug.LogWarning("Este script debe estar en un objeto llamado 'Player' o 'player'");
            return;
        }
        
        // Configurar CapsuleCollider
        if (addCapsuleCollider)
        {
            CapsuleCollider capsuleCollider = GetComponent<CapsuleCollider>();
            if (capsuleCollider == null)
            {
                capsuleCollider = gameObject.AddComponent<CapsuleCollider>();
            }
            
            capsuleCollider.height = playerHeight;
            capsuleCollider.radius = playerRadius;
            capsuleCollider.center = new Vector3(0, playerHeight / 2, 0);
        }
        
        // Configurar Rigidbody
        if (addRigidbody)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            
            rb.mass = playerMass;
            rb.freezeRotation = true;
            rb.drag = 5f;
        }
        
        // Agregar PlayerMovement
        if (addPlayerMovement)
        {
            if (GetComponent<PlayerMovement>() == null)
            {
                gameObject.AddComponent<PlayerMovement>();
            }
        }
        
        
        Debug.Log("Player setup completed successfully!");
    }
    
    // Método para resetear la configuración
    [ContextMenu("Reset Player Setup")]
    public void ResetPlayerSetup()
    {
        // Remover componentes agregados
        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null)
        {
            DestroyImmediate(movement);
        }
        
        
        Debug.Log("Player setup reset completed!");
    }
}
