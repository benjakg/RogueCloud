using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    
    [Header("Input Settings")]
    public KeyCode forwardKey = KeyCode.W;    // Adelante
    public KeyCode backwardKey = KeyCode.S;    // Atrás
    public KeyCode leftKey = KeyCode.A;        // Izquierda (strafe)
    public KeyCode rightKey = KeyCode.D;       // Derecha (strafe)
    public KeyCode jumpKey = KeyCode.Space;    // Salto
    
    private Rigidbody rb;
    private Vector3 movement;
    private bool isGrounded;
    
    void Start()
    {
        // Obtener el componente Rigidbody
        rb = GetComponent<Rigidbody>();
        
        // Si no hay Rigidbody, agregar uno
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        // Configurar el Rigidbody para movimiento suave
        rb.freezeRotation = true;
        rb.drag = 5f;
    }
    
    void Update()
    {
        // Verificar si está en el suelo
        CheckGrounded();
        
        // Obtener input del teclado
        HandleInput();
    }
    
    void FixedUpdate()
    {
        // Aplicar movimiento en FixedUpdate para física consistente
        MovePlayer();
    }
    
    void HandleInput()
    {
        movement = Vector3.zero;
        
        // Movimiento hacia adelante/atrás
        if (Input.GetKey(forwardKey))
        {
            movement += transform.forward;
        }
        if (Input.GetKey(backwardKey))
        {
            movement -= transform.forward;
        }
        
        // Movimiento lateral (strafe) izquierda/derecha
        if (Input.GetKey(leftKey))
        {
            movement -= transform.right;
        }
        if (Input.GetKey(rightKey))
        {
            movement += transform.right;
        }
        
        // Salto
        if (Input.GetKeyDown(jumpKey) && isGrounded)
        {
            Jump();
        }
        
        // Normalizar el vector de movimiento para velocidad consistente
        movement = movement.normalized;
    }
    
    void MovePlayer()
    {
        // Aplicar movimiento usando Rigidbody
        Vector3 moveVelocity = movement * moveSpeed;
        rb.velocity = new Vector3(moveVelocity.x, rb.velocity.y, moveVelocity.z);
    }
    
    void Jump()
    {
        // Aplicar fuerza de salto hacia arriba
        rb.velocity = new Vector3(rb.velocity.x, jumpForce, rb.velocity.z);
    }
    
    void CheckGrounded()
    {
        // Método 1: Raycast hacia abajo
        float rayDistance = 1.2f;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
        bool raycastGrounded = Physics.Raycast(rayOrigin, Vector3.down, rayDistance);
        
        // Método 2: Verificar velocidad Y del Rigidbody (más confiable)
        bool velocityGrounded = Mathf.Abs(rb.velocity.y) < 0.1f;
        
        // Combinar ambos métodos para mayor precisión
        isGrounded = raycastGrounded && velocityGrounded;
        
        // Debug ray para visualizar en Scene view
        Debug.DrawRay(rayOrigin, Vector3.down * rayDistance, raycastGrounded ? Color.green : Color.red);
        
        // Debug en consola para verificar
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log($"Intentando saltar. Raycast: {raycastGrounded}, Velocidad: {velocityGrounded}, Final: {isGrounded}");
        }
    }
    
    // Método público para cambiar la velocidad de movimiento
    public void SetMoveSpeed(float newSpeed)
    {
        moveSpeed = newSpeed;
    }
}
