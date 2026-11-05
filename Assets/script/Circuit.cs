using UnityEditor.Rendering;
using UnityEngine;
using static Interfaces;

public class Circuit : MonoBehaviour , Iburst 
{
    public void Recieveburst(float burstpower)
    {
        Debug.Log(" Circuitttttt ");
        Destroy(gameObject);
    }

}
