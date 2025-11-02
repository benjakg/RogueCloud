using UnityEngine;

public class InventoryController : MonoBehaviour
{
    public GameObject inventoryPanel;
    
    private bool invActive = false;
    
    void Start()
    {
        if(inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
            Debug.Log("Inventario oculto al inicio");
        }
        else
        {
            Debug.LogError("Inventory Panel no está asignado!");
        }
    }
    
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Tab))
        {
            invActive = !invActive;
            if(inventoryPanel != null)
            {
                inventoryPanel.SetActive(invActive);
                Debug.Log("Inventario " + (invActive ? "abierto" : "cerrado"));
            }
        }
        
        if(invActive)
        {
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}
