using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour, IInteractable
{
    public Sprite itemIcon;
    public int amount;

    public void Interact(LogicaPersonaje1 player)
    {
        // Sistema de inventario eliminado - este método está vacío por ahora
        // Puedes implementar otra funcionalidad aquí si es necesario
    }
}
