using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LogicaPersonaje1 : MonoBehaviour
{
    public float velocidadMovimiento = 5.0f;
    public float velocidadRotacion = 200.0f;
    private Animator anim;
    public float x, y;
    
    // Sistema de inventario
    public Image[] inventorySlots; // Slots del inventario en la jerarquía
    public int[] slotAmounts; // Cantidades de cada slot
    
    private List<Item> inventory;

    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();
        
        // Inicializar inventario
        inventory = new List<Item>();
        
        // Debug de configuración
        Debug.Log("LogicaPersonaje1 iniciado");
        Debug.Log("InventorySlots configurados: " + (inventorySlots != null ? inventorySlots.Length : 0));
        Debug.Log("SlotAmounts configurados: " + (slotAmounts != null ? slotAmounts.Length : 0));
    }

    // Update is called once per frame
    void Update()
    {
        x = Input.GetAxis("Horizontal");
        y = Input.GetAxis("Vertical");
        transform.Rotate(0, x * Time.deltaTime * velocidadRotacion, 0);
        transform.Translate(transform.forward * y * Time.deltaTime * velocidadMovimiento, Space.World);
        anim.SetFloat("VelX", x);
        anim.SetFloat("VelY", y);
    }
    
    public void AddToInventory(Item item)
    {
        Debug.Log("AddToInventory llamado con item: " + (item != null ? item.name : "null"));
        
        if(item == null)
        {
            Debug.LogError("Item es null!");
            return;
        }
        
        if(item.itemIcon == null)
        {
            Debug.LogError("Item.itemIcon es null!");
            return;
        }
        
        if(inventorySlots == null || inventorySlots.Length == 0)
        {
            Debug.LogError("InventorySlots no está configurado!");
            return;
        }
        
        inventory.Add(item);
        
        // Buscar un slot vacío en el inventario de la jerarquía
        for(int i = 0; i < inventorySlots.Length; i++)
        {
            if(inventorySlots[i] == null)
            {
                Debug.LogError("InventorySlots[" + i + "] es null!");
                continue;
            }
            
            if(inventorySlots[i].sprite == null)
            {
                inventorySlots[i].sprite = item.itemIcon;
                inventorySlots[i].enabled = true;
                slotAmounts[i]++;
                Debug.Log("¡Llave agregada al slot " + i + " del inventario!");
                return;
            }
        }
        
        Debug.LogWarning("No hay slots vacíos disponibles!");
    }
    
    void OnTriggerEnter(Collider other)
    {
        Debug.Log("=== TRIGGER DETECTADO ===");
        Debug.Log("Objeto: " + other.name);
        Debug.Log("Tag: " + other.tag);
        
        Item item = other.GetComponent<Item>();
        if(item != null)
        {
            Debug.Log("¡Item encontrado: " + other.name + "!");
            Debug.Log("ItemIcon: " + (item.itemIcon != null ? item.itemIcon.name : "null"));
            AddToInventory(item);
            
            // Destruir la llave después de recogerla
            Debug.Log("Destruyendo la llave: " + other.name);
            Destroy(other.gameObject);
        }
        else
        {
            Debug.LogWarning("No se encontró componente Item en: " + other.name);
        }
    }
}
