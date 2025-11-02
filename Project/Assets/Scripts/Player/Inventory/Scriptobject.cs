using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "New Scriptable Object", menuName = "Inventory objects/Create New Item")]
public class Scriptobject : ScriptableObject
{
    public string itemName;
    public Sprite itemIcon;
    public int itemValue;
}
