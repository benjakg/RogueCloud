using UnityEngine;

public class InventorySystem : MonoBehaviour
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
    
    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger detectado: " + other.name);
        
        Item item = other.GetComponent<Item>();
        if(item != null)
        {
            Debug.Log("Item encontrado: " + other.name);
            LogicaPersonaje1 player = GetComponent<LogicaPersonaje1>();
            if(player != null)
            {
                item.Interact(player);
                Destroy(other.gameObject);
            }
        }
    }
}