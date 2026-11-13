using UnityEngine;
using static Interfaces;

public class Taser : MonoBehaviour, Iweapon
{
    [Header("References")]
    private Camera playerCamera;

    [Header("Direct Shock")]
    public float meleeRange = 3f;
    public float shockPower = 10f;
    public float shockCooldown = 0.35f;
    public LayerMask shockLayers = ~0;
    public GameObject directShockEffect;

    [Header("Charged Throw")]
    public bool allowThrow = true;
    public GameObject ProjectilePrerfab;
    public float throwForce = 10f;
    public float MinChargetime = 0.4f;
    public float MaxChargetime = 1.5f;
    public float throwSpin = 2f;

    private float Chargetime;
    private float nextShockTime;

    private void Awake()
    {
        playerCamera = GetComponentInParent<fpsmovement>()?.Camera;
    }

    private void Update()
    {
        if (Inventorynew.instance != null && Inventorynew.instance.IsOpen)
        {
            Chargetime = 0f;
            return;
        }

        if (Input.GetMouseButtonDown(0))
            Taserr();

        if (!allowThrow)
            return;

        if (Input.GetKey(KeyCode.F))
            Chargetime = Mathf.Min(Chargetime + Time.deltaTime, Mathf.Max(MinChargetime, MaxChargetime));

        if (Input.GetKeyUp(KeyCode.F))
        {
            if (Chargetime >= MinChargetime)
                Throwables();

            Chargetime = 0f;
        }
    }

    private void Taserr()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (playerCamera == null || Time.time < nextShockTime)
            return;

        nextShockTime = Time.time + shockCooldown;

        if (!Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward,
                out RaycastHit hit, meleeRange, shockLayers, QueryTriggerInteraction.Collide))
            return;

        Ishockable shockable = hit.collider.GetComponentInParent<Ishockable>();
        Iignitable ignitable = hit.collider.GetComponentInParent<Iignitable>();

        if (shockable != null)
            shockable.Recieveshock(shockPower);

        if (ignitable != null)
        {
            ignitable.Ignite(hit.point);
            Debug.Log("Taser ignited: " + hit.collider.name, this);
        }

        if (directShockEffect != null)
        {
            GameObject effect = Instantiate(directShockEffect,
                hit.point + hit.normal * 0.03f,
                Quaternion.LookRotation(hit.normal));
            Destroy(effect, 2f);
        }
    }

    public void useweapon()
    {
        Taserr();
    }

    public void Throwables()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (playerCamera == null || ProjectilePrerfab == null)
        {
            Debug.LogWarning("Taser requires a Camera and projectile prefab.", this);
            return;
        }

        if (Inventorynew.instance != null && Inventorynew.instance.GetEquippedItemAmount() < 1)
            return;

        Vector3 spawnPosition = playerCamera.transform.position + playerCamera.transform.forward * 0.8f;
        GameObject projectile = Instantiate(ProjectilePrerfab, spawnPosition,
            Quaternion.LookRotation(playerCamera.transform.forward));

        Rigidbody projectileBody = projectile.GetComponent<Rigidbody>();

        if (projectileBody == null)
        {
            Debug.LogWarning("Taser projectile needs a Rigidbody on its root.", projectile);
            Destroy(projectile);
            return;
        }

        float chargePercent = Mathf.Clamp01(Chargetime / Mathf.Max(0.01f, MaxChargetime));
        projectileBody.AddForce(playerCamera.transform.forward * throwForce *
            Mathf.Lerp(0.65f, 1f, chargePercent), ForceMode.Impulse);
        projectileBody.AddTorque(Random.insideUnitSphere * throwSpin, ForceMode.Impulse);

        if (Inventorynew.instance != null)
        {
            Inventorynew.instance.ConsumeEquippedItem(1);
            Inventorynew.instance.Equipping();
        }
    }
}
