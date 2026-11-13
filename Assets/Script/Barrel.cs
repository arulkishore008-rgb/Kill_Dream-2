
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Barrel : MonoBehaviour
{
    [Header("Push Settings")]
    public float pushForce = 20f;
    public float pushHeight = 0.6f;

    [Header("Barrel State")]
    public float tippingAngle = 60f;
    public bool isLeaking;

    private Rigidbody rb;

    [Header("Oil Spillage")]
    public GameObject oilPuddlePrefab;
    public Transform spillPoint;
    public LayerMask floorLayer;
    public float spillRayDistance = 5f;

    private bool hasSpawnedOil;


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void KnockOver(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        direction.Normalize();

        Vector3 forcePoint =
            rb.worldCenterOfMass + Vector3.up * pushHeight;

        rb.AddForceAtPosition(
            direction * pushForce,
            forcePoint,
            ForceMode.Impulse
        );
    }

    private void Update()
    {
        CheckBarrelTilt();

        if (isLeaking && !hasSpawnedOil)
        {
            TrySpawnOil();
        }

    }

    private void CheckBarrelTilt()
    {
        if (isLeaking)
            return;

        float angle = Vector3.Angle(
            transform.up,
            Vector3.up
        );

        if (angle >= tippingAngle)
        {
            isLeaking = true;
            Debug.Log("Barrel is leaking");
        }
    }


    private void TrySpawnOil()
    {
        if (hasSpawnedOil || oilPuddlePrefab == null || spillPoint == null)
            return;

        Vector3 origin = spillPoint.position + Vector3.up;

        if (Physics.Raycast(
            origin,
            Vector3.down,
            out RaycastHit hit,
            spillRayDistance,
            floorLayer,
            QueryTriggerInteraction.Ignore))
        {
            Vector3 spawnPosition = hit.point + Vector3.up * 0.02f;

            Instantiate(
                oilPuddlePrefab,
                spawnPosition,
                Quaternion.identity
            );

            hasSpawnedOil = true;
        }
    }

}
