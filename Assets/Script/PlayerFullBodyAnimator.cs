using UnityEngine;

[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
public class PlayerFullBodyAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody playerBody;
    [SerializeField] private fpsmovement movement;
    [SerializeField] private Inventorynew inventory;
    [SerializeField] private Wallrunning wallrunning;

    [Header("Blending")]
    [Min(0f)][SerializeField] private float movementDamping = 0.12f;
    [Min(0f)][SerializeField] private float weaponLayerBlendSpeed = 7f;
    [SerializeField] private string weaponLayerName = "Weapon UpperBody";

    private int weaponLayerIndex = -1;

    private static readonly int MoveX = Animator.StringToHash("MoveX");
    private static readonly int MoveY = Animator.StringToHash("MoveY");
    private static readonly int Grounded = Animator.StringToHash("IsGrounded");
    private static readonly int WallRunning = Animator.StringToHash("IsWallRunning");
    private static readonly int Grappling = Animator.StringToHash("IsGrappling");
    private static readonly int Aiming = Animator.StringToHash("IsAiming");
    private static readonly int Sprinting = Animator.StringToHash("IsSprinting");
    private static readonly int WeaponType = Animator.StringToHash("WeaponType");
    private static readonly int Fire = Animator.StringToHash("Fire");

    private void Awake()
    {
        if (movement == null) movement = GetComponent<fpsmovement>();
        if (inventory == null) inventory = GetComponent<Inventorynew>();
        if (wallrunning == null) wallrunning = GetComponent<Wallrunning>();
        if (playerBody == null) playerBody = GetComponent<Rigidbody>();
        if (animator == null && movement != null) animator = movement.anim;
        if (animator == null) animator = GetComponentInChildren<Animator>(true);

        if (animator == null || playerBody == null || movement == null)
        {
            Debug.LogError("PlayerFullBodyAnimator needs an Animator, Rigidbody and fpsmovement.", this);
            enabled = false;
            return;
        }

        weaponLayerIndex = animator.GetLayerIndex(weaponLayerName);
        if (weaponLayerIndex < 0)
            Debug.LogWarning("Animator layer '" + weaponLayerName + "' was not found. Add an Override layer with an upper-body Avatar Mask.", this);

        animator.applyRootMotion = false;
        if (weaponLayerIndex >= 0) animator.SetLayerWeight(weaponLayerIndex, 0f);
    }

    private void Update()
    {
        bool grappling = movement.IsGrappling;
        bool wallRunning = wallrunning != null && wallrunning.IsWallrunning && !grappling;
        bool grounded = fpsmovement.Canjump && !wallRunning && !grappling;

        // Translate actual horizontal Rigidbody velocity into local strafe/forward values.
        // Our 2D tree uses 0 = idle, +/-0.5 = walking, +1 = forward sprinting.
        Vector3 horizontalVelocity = playerBody.linearVelocity;
        horizontalVelocity.y = 0f;
        Vector3 localVelocity = transform.InverseTransformDirection(horizontalVelocity);
        float walkSpeed = Mathf.Max(0.01f, movement.walkspeed);
        float sprintSpeed = Mathf.Max(walkSpeed + 0.01f, movement.Sprintspeed);

        float x = Mathf.Clamp(localVelocity.x / walkSpeed * 0.5f, -0.5f, 0.5f);
        float z = localVelocity.z <= 0f
            ? Mathf.Clamp(localVelocity.z / walkSpeed * 0.5f, -0.5f, 0f)
            : localVelocity.z <= walkSpeed
                ? Mathf.InverseLerp(0f, walkSpeed, localVelocity.z) * 0.5f
                : Mathf.Lerp(0.5f, 1f, Mathf.InverseLerp(walkSpeed, sprintSpeed, localVelocity.z));

        if (horizontalVelocity.sqrMagnitude < 0.01f || wallRunning || grappling)
        {
            x = 0f;
            z = 0f;
        }

        animator.SetFloat(MoveX, x, movementDamping, Time.deltaTime);
        animator.SetFloat(MoveY, z, movementDamping, Time.deltaTime);
        animator.SetBool(Grounded, grounded);
        animator.SetBool(WallRunning, wallRunning);
        animator.SetBool(Grappling, grappling);
        animator.SetBool(Aiming, movement.IsAiming);
        animator.SetBool(Sprinting, movement.IsSprinting);

        // The inventory creates a weapon hand prefab on equip. Read its style, not
        // the hotbar index: an item can be moved to any slot or picked up later.
        bool hasGun = movement.HasEquippedGun;
        WeaponAnimationStyle style = WeaponAnimationStyle.Unarmed;
        if (hasGun)
        {
            GameObject held = inventory != null ? inventory.CurrentHeldObject : null;
            WeaponAnimationProfile profile = held != null ? held.GetComponentInChildren<WeaponAnimationProfile>(true) : null;
            style = profile != null ? profile.style : WeaponAnimationStyle.Rifle;
        }
        animator.SetInteger(WeaponType, (int)style);

        if (weaponLayerIndex >= 0)
        {
            // During grappling, reveal the base-layer full-body grapple animation.
            float targetWeight = hasGun && !grappling ? 1f : 0f;
            float weight = Mathf.MoveTowards(
                animator.GetLayerWeight(weaponLayerIndex),
                targetWeight,
                weaponLayerBlendSpeed * Time.deltaTime);
            animator.SetLayerWeight(weaponLayerIndex, weight);
        }
    }

    // Called only after fpsmovement actually fires a shot.
    public void NotifyShot()
    {
        if (animator == null || movement == null || !movement.HasEquippedGun || movement.IsGrappling)
            return;

        animator.SetTrigger(Fire);
    }
}
