using UnityEngine;

[CreateAssetMenu(fileName = "Items", menuName = "Scriptable Objects/Items")]
public class Items : ScriptableObject
{
    public string Weapon_names;
    public GameObject Prefab;
    //public bool IsStackable;
    public int maxStacksize;
    public Sprite icon;
    public GameObject handPrefab;
}
