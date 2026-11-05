using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Rigidbody))]
public class fpsmovement : MonoBehaviour
{
    [Header("References")]
    public Camera Camera;
    public Transform camHolder;
    public Transform Jumpchecktransform;
    public Transform EnemyInteractPoint;
    public GameObject Interactive_img;
    public Animator anim;
    public Inventorynew inventory;
    public Volume globalvolume;
    public LayerMask Ground;
    public LayerMask Enemy_Layer;

    [Header("Look")]
    public float mousesensitivity = 1.5f;
    public float maxLookUp = 80f;
    public float maxLookDown = 80f;

    [Header("Movement")]
    public float Currentspeed;
    public float walkspeed = 4f;
    public float Sprintspeed = 7f;
    public float Targetspeed;
    public float acceleration = 22f;
    public float deceleration = 28f;
    [Range(0f, 1f)] public float airControl = 0.35f;
    public float aimMoveMultiplier = 0.75f;
    public bool IsSprinting;
    public static bool Canjump;
    public float jumpforce = 5f;
    public float Jumpchecklength = 0.2f;
    public float groundCheckRadius = 0.18f;

    [Header("Camera / Wallrun Tilt")]
    public float Maxtilt = 12f;
    public float Tiltspeed = 8f;

    [Header("Camera Bob")]
    public float bobSpeed = 10f;
    public float bobAmount = 0.035f;
    public float xbobAmount = 0.018f;
    public float Timer;
    public float cameraPositionSmooth = 12f;

    [Header("Camera FOV")]
    public float CurrentFOV = 65f;
    public float SprintFOV = 76f;
    public float AimFOV = 48f;
    public float SprintFOV_Timer = 9f;

    [Header("Vignette")]
    public float SprintVignette = 0.22f;
    public float VignetteSpeed = 7f;

    [Header("Weapon System - Controlled by Inventorynew")]
    public LayerMask ShootLayers = ~0;
    public float recoilRecovery = 12f;
    public float aimTransitionSpeed = 11f;
    [Tooltip("Optional default. Inventorynew assigns its Hand transform during equip.")]
    public Transform weaponRoot;
    public bool IsAiming { get; private set; }
    public bool IsGrappling { get; private set; }

    [Header("Takedown")]
    public float takedownDistance = 2.5f;
    public float takedownRadius = 0.35f;
    public float raylength = 5f;

    [Header("Health")]
    public float MaxHealth = 100f;
    public float CurrentHealth = 100f;

    [Header("Optional Camera Shake")]
    public float JumpshakeDuration = 0.16f;
    public float CameraShakeTimer;
    public float JumpShakeAmount = 0.025f;
    public float JumpShakeSpeed = 26f;

    private EquippableGun equippedGun;
    private PlayerFullBodyAnimator fullBodyAnimation;
    public bool HasEquippedGun => equippedGun != null;
    public bool IsInventoryOpen => inventory != null && inventory.IsOpen;

    private Rigidbody rb;
    private Wallrunning wallrun;
    private Vignette vignette;
    private Vector3 cameraRestPosition;
    private Vector2 moveInput;
    private float yaw;
    private float pitch;
    private float currentTilt;
    private float currentRecoil;
    private float aimWeight;
    private float nextShotTime;
    private float fovKick;
    private float shakeRemaining;
    private bool jumpQueued;
    private bool hadGroundContact;
    private Vector3 weaponHipPosition;
    private Quaternion weaponHipRotation;
    private bool hasSpeedParameter;
    private bool hasIdleParameter;
    private bool hasSprintParameter;
    private bool hasJumpParameter;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        wallrun = GetComponent<Wallrunning>();
        fullBodyAnimation = GetComponent<PlayerFullBodyAnimator>();
        if (inventory == null) inventory = GetComponent<Inventorynew>();
        if (Camera == null) Camera = GetComponentInChildren<Camera>();
        if (camHolder == null && Camera != null) camHolder = Camera.transform.parent != null ? Camera.transform.parent : Camera.transform;
        if (anim == null) anim = GetComponentInChildren<Animator>();

        if (camHolder == null || Camera == null)
        {
            Debug.LogError("FPS controller needs a Camera and camHolder reference.", this);
            enabled = false;
            return;
        }

        cameraRestPosition = camHolder.localPosition;
        if (weaponRoot != null)
        {
            weaponHipPosition = weaponRoot.localPosition;
            weaponHipRotation = weaponRoot.localRotation;
        }
        yaw = transform.eulerAngles.y;
        pitch = Mathf.DeltaAngle(0f, camHolder.localEulerAngles.x);
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        if (globalvolume != null && globalvolume.profile != null)
            globalvolume.profile.TryGet(out vignette);

        if (Interactive_img != null) Interactive_img.SetActive(false);
        if (anim != null)
        {
            anim.applyRootMotion = false;
            foreach (AnimatorControllerParameter p in anim.parameters)
            {
                if (p.name == "Speed" && p.type == AnimatorControllerParameterType.Float) hasSpeedParameter = true;
                if (p.name == "isIdle" && p.type == AnimatorControllerParameterType.Bool) hasIdleParameter = true;
                if (p.name == "isSprinting" && p.type == AnimatorControllerParameterType.Bool) hasSprintParameter = true;
                if (p.name == "isJumping" && p.type == AnimatorControllerParameterType.Bool) hasJumpParameter = true;
            }
        }
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, MaxHealth);
    }

    private void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        UpdateGrounded();
        ReadInput();
        Look();
        UpdateAiming();
        HandleShooting();
        HandleTakedown();
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        if (IsGrappling) return; // GrappleHook moves the Rigidbody while hooked.
        if (wallrun != null && wallrun.IsWallrunning) return; // Wallrunning owns horizontal velocity.

        Vector3 wishDirection = (transform.forward * moveInput.y + transform.right * moveInput.x);
        if (wishDirection.sqrMagnitude > 1f) wishDirection.Normalize();
        float speed = IsSprinting ? Sprintspeed : walkspeed;
        if (IsAiming) speed *= aimMoveMultiplier;
        Targetspeed = moveInput.sqrMagnitude > 0.01f ? speed : 0f;

        Vector3 targetHorizontalVelocity = wishDirection * Targetspeed;
        Vector3 currentHorizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float rate = Targetspeed > 0f ? acceleration : deceleration;
        if (!Canjump) rate *= airControl;

        // Preserve wall-jump launch momentum while the Wallrunning script is exiting.
        if (wallrun == null || !wallrun.exitingWall || Canjump)
        {
            Vector3 newHorizontalVelocity = Vector3.MoveTowards(currentHorizontalVelocity, targetHorizontalVelocity, rate * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector3(newHorizontalVelocity.x, rb.linearVelocity.y, newHorizontalVelocity.z);
        }

        if (jumpQueued)
        {
            jumpQueued = false;
            if (Canjump)
            {
                Vector3 v = rb.linearVelocity;
                rb.linearVelocity = new Vector3(v.x, Mathf.Max(v.y, 0f), v.z);
                rb.AddForce(Vector3.up * jumpforce, ForceMode.Impulse);
                Canjump = false;
            }
        }
    }

    private void LateUpdate()
    {
        if (camHolder == null || Camera == null) return;
        float dt = Time.deltaTime;
        Vector3 planarVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float planarSpeed = planarVelocity.magnitude;
        float movementWeight = Canjump ? Mathf.Clamp01(planarSpeed / Mathf.Max(0.1f, walkspeed)) : 0f;

        Vector3 targetOffset = Vector3.zero;
        if (movementWeight > 0.05f && !IsGrappling)
        {
            Timer += dt * bobSpeed * (IsSprinting ? 1.4f : 1f);
            float bobScale = IsAiming ? 0.3f : 1f;
            targetOffset = new Vector3(Mathf.Cos(Timer * 0.5f) * xbobAmount,
                Mathf.Sin(Timer) * bobAmount, 0f) * movementWeight * bobScale;
        }
        else Timer = 0f;

        if (shakeRemaining > 0f)
        {
            shakeRemaining -= dt;
            CameraShakeTimer += dt;
            targetOffset.y += Mathf.Sin(CameraShakeTimer * JumpShakeSpeed) * JumpShakeAmount * Mathf.Clamp01(shakeRemaining / Mathf.Max(0.01f, JumpshakeDuration));
        }
        camHolder.localPosition = Vector3.Lerp(camHolder.localPosition, cameraRestPosition + targetOffset, 1f - Mathf.Exp(-cameraPositionSmooth * dt));

        float wantedTilt = 0f;
        if (wallrun != null && wallrun.IsWallrunning)
            wantedTilt = wallrun.wallleft ? -Maxtilt : wallrun.wallright ? Maxtilt : 0f;
        currentTilt = Mathf.Lerp(currentTilt, wantedTilt, 1f - Mathf.Exp(-Tiltspeed * dt));
        currentRecoil = Mathf.MoveTowards(currentRecoil, 0f, recoilRecovery * dt);
        camHolder.localRotation = Quaternion.Euler(pitch - currentRecoil, 0f, currentTilt);

        float wantedFOV = IsAiming ? AimFOV : IsSprinting ? SprintFOV : CurrentFOV;
        Camera.fieldOfView = Mathf.Lerp(Camera.fieldOfView, wantedFOV + fovKick, 1f - Mathf.Exp(-SprintFOV_Timer * dt));
        fovKick = Mathf.MoveTowards(fovKick, 0f, 16f * dt);
        if (vignette != null)
        {
            float target = IsSprinting ? SprintVignette : 0f;
            vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, target, 1f - Mathf.Exp(-VignetteSpeed * dt));
        }
    }

    private void UpdateGrounded()
    {
        if (Jumpchecktransform == null) { Canjump = false; return; }
        bool nowGrounded = Physics.CheckSphere(Jumpchecktransform.position, groundCheckRadius, Ground, QueryTriggerInteraction.Ignore);
        if (nowGrounded && !hadGroundContact && rb.linearVelocity.y < -3f)
        {
            shakeRemaining = JumpshakeDuration;
            CameraShakeTimer = 0f;
        }
        hadGroundContact = nowGrounded;
        Canjump = nowGrounded;
    }

    private void ReadInput()
    {
        moveInput = (IsGrappling || IsInventoryOpen) ? Vector2.zero : new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        moveInput = Vector2.ClampMagnitude(moveInput, 1f);
        IsAiming = equippedGun != null && Input.GetMouseButton(1) && !IsGrappling && !IsInventoryOpen;
        IsSprinting = !IsAiming && !IsGrappling && !IsInventoryOpen && Canjump && moveInput.y > 0.1f && Input.GetKey(KeyCode.LeftShift);
        if (Input.GetKeyDown(KeyCode.Space) && Canjump && !IsGrappling && !IsInventoryOpen && (wallrun == null || !wallrun.IsWallrunning))
            jumpQueued = true;

        Currentspeed = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude;
        if (IsSprinting && Currentspeed > 0.3f)
            SoundManager.MakeNoise(transform.position, 5f, gameObject);
    }

    private void Look()
    {
        if (IsInventoryOpen) return;
        yaw += Input.GetAxisRaw("Mouse X") * mousesensitivity;
        pitch = Mathf.Clamp(pitch - Input.GetAxisRaw("Mouse Y") * mousesensitivity, -maxLookUp, maxLookDown);
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    // Inventorynew calls this after equipping, unequipping or dropping an item.
    public void SetEquippedGun(EquippableGun gun, Transform handRoot)
    {
        if (weaponRoot != handRoot)
        {
            weaponRoot = handRoot;
            if (weaponRoot != null)
            {
                weaponHipPosition = weaponRoot.localPosition;
                weaponHipRotation = weaponRoot.localRotation;
            }
        }
        equippedGun = gun;
        if (gun == null) IsAiming = false;
    }

    private void UpdateAiming()
    {
        aimWeight = Mathf.MoveTowards(aimWeight, IsAiming ? 1f : 0f, aimTransitionSpeed * Time.deltaTime);
        if (weaponRoot == null) return;

        Vector3 targetPosition = equippedGun == null
            ? weaponHipPosition
            : weaponHipPosition + equippedGun.adsLocalPositionOffset;
        Quaternion targetRotation = equippedGun == null
            ? weaponHipRotation
            : weaponHipRotation * Quaternion.Euler(equippedGun.adsLocalEulerOffset);
        weaponRoot.localPosition = Vector3.Lerp(weaponHipPosition, targetPosition, aimWeight);
        weaponRoot.localRotation = Quaternion.Slerp(weaponHipRotation, targetRotation, aimWeight);
    }

    private void HandleShooting()
    {
        EquippableGun gun = equippedGun;
        // Wall-running does NOT disable gunfire. Only grappling, inventory UI and
        // missing/unequipped guns prevent shooting.
        if (gun == null || IsGrappling || IsInventoryOpen || Camera == null) return;

        bool wantsShot = gun.automaticFire ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);
        float shotsPerSecond = Mathf.Max(0.01f, gun.shotsPerSecond);
        if (!wantsShot || Time.time < nextShotTime) return;
        nextShotTime = Time.time + 1f / shotsPerSecond;

        float spread = Mathf.Lerp(gun.hipSpread, gun.aimSpread, aimWeight);
        Vector2 randomSpread = Random.insideUnitCircle * spread;
        Vector3 direction = Quaternion.Euler(randomSpread.y, randomSpread.x, 0f) * Camera.transform.forward;
        Ray shot = new Ray(Camera.transform.position, direction);

        if (Physics.Raycast(shot, out RaycastHit hit, gun.range, ShootLayers, QueryTriggerInteraction.Ignore))
        {
            EnemyAI enemy = hit.collider.GetComponentInParent<EnemyAI>();
            if (enemy != null && !enemy.IsDead()) enemy.takeDamage(gun.damage);
            if (gun.impactEffect != null)
                Destroy(Instantiate(gun.impactEffect, hit.point, Quaternion.LookRotation(hit.normal)), 2f);
        }
        if (gun.muzzleFlash != null) gun.muzzleFlash.Play();
        if (gun.fireAudio != null && gun.fireAudio.clip != null)
            gun.fireAudio.PlayOneShot(gun.fireAudio.clip);
        currentRecoil = Mathf.Min(currentRecoil + gun.recoilPerShot * (IsAiming ? 0.65f : 1f), 8f);
        if (fullBodyAnimation != null) fullBodyAnimation.NotifyShot();
        SoundManager.MakeNoise(transform.position, 12f, gameObject);
    }

    private void HandleTakedown()
    {
        if (Interactive_img == null || Camera == null) return;
        if (IsInventoryOpen || (inventory != null && inventory.IsLookingAtPickup))
        {
            Interactive_img.SetActive(false);
            return;
        }
        bool canTakedown = false;
        if (Physics.SphereCast(Camera.transform.position, takedownRadius, Camera.transform.forward,
                out RaycastHit hit, takedownDistance, Enemy_Layer, QueryTriggerInteraction.Ignore))
        {
            EnemyAI enemy = hit.collider.GetComponentInParent<EnemyAI>();
            if (enemy != null && !enemy.IsDead())
            {
                Vector3 towardsPlayer = (transform.position - enemy.transform.position).normalized;
                canTakedown = Vector3.Angle(enemy.transform.forward, towardsPlayer) > 100f;
                if (canTakedown && Input.GetKeyDown(KeyCode.E)) enemy.TakedownEnemy();
            }
        }
        Interactive_img.SetActive(canTakedown);
    }

    private void UpdateAnimation()
    {
        if (anim == null) return;
        bool moving = Currentspeed > 0.12f;
        float speedBlend = 0f;
        if (moving)
        {
            speedBlend = Currentspeed <= walkspeed
                ? Mathf.InverseLerp(0f, Mathf.Max(0.01f, walkspeed), Currentspeed) * 0.5f
                : Mathf.Lerp(0.5f, 1f, Mathf.InverseLerp(walkspeed, Mathf.Max(walkspeed + 0.01f, Sprintspeed), Currentspeed));
        }
        if (hasSpeedParameter)
        {
            anim.SetFloat("Speed", speedBlend, 0.09f, Time.deltaTime);
            if (!moving && anim.GetFloat("Speed") < 0.015f) anim.SetFloat("Speed", 0f);
        }
        if (hasIdleParameter) anim.SetBool("isIdle", !moving && Canjump);
        if (hasSprintParameter) anim.SetBool("isSprinting", IsSprinting && moving);
        if (hasJumpParameter) anim.SetBool("isJumping", !Canjump);
    }

    public void SetGrappleMovementLocked(bool locked)
    {
        IsGrappling = locked;
        if (locked)
        {
            jumpQueued = false;
            IsSprinting = false;
        }
    }

    public void AddFovKick(float amount) { fovKick += amount; }

    public void TakeDamage(float amount)
    {
        CurrentHealth = Mathf.Max(0f, CurrentHealth - Mathf.Max(0f, amount));
        if (CurrentHealth <= 0f) { enabled = false; Debug.Log("Player died", this); }
    }
    public void TakeDamage() { TakeDamage(10f); }

    private void OnDrawGizmosSelected()
    {
        if (Jumpchecktransform != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(Jumpchecktransform.position, groundCheckRadius);
        }
    }
}
