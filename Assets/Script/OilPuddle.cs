
using UnityEngine;
using static Interfaces;

public class OilPuddle : MonoBehaviour, Iignitable
{
    [Header("Oil Spreading")]
    public float startRadius = 0.2f;
    public float maxRadius = 2.5f;
    public float spreadSpeed = 0.5f;
    public float thickness = 0.01f;

    [Header("Fire")]
    public GameObject fireEffectPrefab;
    public float burnDuration = 8f;

    public bool IsBurning { get; private set; }

    private float currentRadius;
    private float burnTimer;
    private GameObject activeFire;

    private void Awake()
    {
        currentRadius = Mathf.Clamp(startRadius, 0.01f, maxRadius);
        UpdateSize();
    }

    private void Update()
    {
        currentRadius = Mathf.MoveTowards(
            currentRadius,
            maxRadius,
            spreadSpeed * Time.deltaTime
        );

        UpdateSize();

        if (IsBurning)
        {
            burnTimer -= Time.deltaTime;

            if (burnTimer <= 0f)
                BurnOut();
        }
    }

    private void UpdateSize()
    {
        transform.localScale = new Vector3(
            currentRadius * 2f,
            thickness,
            currentRadius * 2f
        );
    }

    public void Ignite(Vector3 ignitionPoint)
    {
        if (IsBurning)
            return;

        IsBurning = true;
        burnTimer = burnDuration;

        if (fireEffectPrefab != null)
        {
            activeFire = Instantiate(
                fireEffectPrefab,
                ignitionPoint + Vector3.up * 0.05f,
                Quaternion.identity
            );
        }

        Debug.Log("Oil has caught fire!", this);
    }

    private void BurnOut()
    {
        IsBurning = false;

        if (activeFire != null)
            Destroy(activeFire);

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (activeFire != null)
            Destroy(activeFire);
    }
}
