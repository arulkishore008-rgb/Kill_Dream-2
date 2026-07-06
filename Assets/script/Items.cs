using UnityEngine;

[CreateAssetMenu(fileName = "Items", menuName = "Scriptable Objects/Items")]
public class Items : ScriptableObject
{
    public string Weapon_names;
    public float weapon_Damage;
    public float weapon_Range;
    public GameObject weapon;
    public bool IsStackable;
    public int maxStacksize;
    public Sprite icon;
}
