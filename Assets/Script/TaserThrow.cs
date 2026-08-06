using UnityEngine;
using static Interfaces; 

public class TaserThrow : MonoBehaviour
{
    [Header("Area of Effect Settings")]
    public float shockRadius = 4f;
    public float shockPower = 10f;
    public GameObject electricalSparksEffect; 

    private bool hasDischarged = false;

    
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player")) return;

        if (hasDischarged) return;

        TriggerAreaShock();
    }

    void TriggerAreaShock()
    {
        hasDischarged = true;

        if (electricalSparksEffect != null)
        {
            GameObject Sparks = Instantiate(electricalSparksEffect, transform.position, Quaternion.identity);

            Destroy(Sparks , 4f);
        }

        Collider[] colliders = Physics.OverlapSphere(transform.position, shockRadius);

        foreach (Collider nearbyObj in colliders)
        {
            
            Ishockable shockable = nearbyObj.GetComponent<Ishockable>();

            if (shockable != null)
            {
                shockable.Recieveshock(shockPower);
                Debug.Log("Environmental Shock hit: " + nearbyObj.gameObject.name);
            }
        }
             Destroy(gameObject);        
    }

    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, shockRadius);
    }
}