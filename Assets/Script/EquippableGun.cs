using UnityEngine;

public class EquippableGun : MonoBehaviour
{
    [Header("Hitscan Weapon")]
    public float damage = 20f;
    public float shotsPerSecond = 8f;
    public float range = 100f;
    public bool automaticFire = true;
    [Min(0f)] public float hipSpread = 1.5f;
    [Min(0f)] public float aimSpread = 0.15f;
    public float recoilPerShot = 1.2f;

    [Header("Aim-Down-Sights / Hand Holder Offset")]
    [Tooltip("Added to the Hand transform's initial local position while aiming.")]
    public Vector3 adsLocalPositionOffset = new Vector3(0f, -0.10f, 0.12f);
    [Tooltip("Rotation offset applied to the Hand transform's initial local rotation.")]
    public Vector3 adsLocalEulerOffset;

    [Header("Weapon-only Visuals and Audio")]
    public Transform muzzle;
    public ParticleSystem muzzleFlash;
    public GameObject impactEffect;
    public AudioSource fireAudio;

    private void OnValidate()
    {
        shotsPerSecond = Mathf.Max(0.1f, shotsPerSecond);
        range = Mathf.Max(1f, range);
        hipSpread = Mathf.Max(0f, hipSpread);
        aimSpread = Mathf.Max(0f, aimSpread);
    }
}
