using UnityEditor.Rendering;
using UnityEngine;
using static Interfaces;

public class Circuit : MonoBehaviour , Ishockable , Iweapon
{
    public void Recieveshock(float shockpower)
    {
        Debug.Log(" Circuitttttt ");
        Destroy(gameObject);
    }

   public void useweapon()
    {
        Debug.Log("weapon");

    }
}
