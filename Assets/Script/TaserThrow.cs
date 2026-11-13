using System.Collections.Generic;
using UnityEngine;
using static Interfaces;

public class TaserThrow : MonoBehaviour
{
    [Header("Area Shock")]
    public float shockRadius = 4f;
    public float shockPower = 10f;
    public LayerMask affectedLayers = ~0;
    public GameObject electricalSparksEffect;

    private bool hasDischarged;

    private void OnCollisionEnter(Collision collision)
    {
        if (hasDischarged || collision.gameObject.CompareTag("Player"))
            return;

        TriggerAreaShock();
    }

    private void TriggerAreaShock()
    {
        hasDischarged = true;

        if (electricalSparksEffect != null)
        {
            GameObject sparks = Instantiate(electricalSparksEffect,
                transform.position, Quaternion.identity);
            Destroy(sparks, 4f);
        }

        Collider[] colliders = Physics.OverlapSphere(transform.position,
            shockRadius, affectedLayers, QueryTriggerInteraction.Collide);

        HashSet<Ishockable> shockedObjects = new HashSet<Ishockable>();
        HashSet<Iignitable> ignitedObjects = new HashSet<Iignitable>();

        foreach (Collider nearbyCollider in colliders)
        {
            Ishockable shockable = nearbyCollider.GetComponentInParent<Ishockable>();
            if (shockable != null && shockedObjects.Add(shockable))
                shockable.Recieveshock(shockPower);

            Iignitable ignitable = nearbyCollider.GetComponentInParent<Iignitable>();
            if (ignitable != null && ignitedObjects.Add(ignitable))
            {
                ignitable.Ignite(nearbyCollider.ClosestPoint(transform.position));
                Debug.Log("Thrown taser ignited: " + nearbyCollider.name, this);
            }
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, shockRadius);
    }
}
