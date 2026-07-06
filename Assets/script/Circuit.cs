using UnityEditor.Rendering;
using UnityEngine;
using static Interfaces;

public class Circuit : MonoBehaviour , Ishockable
{
    public void Recieveshock(float shockpower)
    {
        Debug.Log(" Circuitttttt ");
        Destroy(gameObject);
    }

   
}
