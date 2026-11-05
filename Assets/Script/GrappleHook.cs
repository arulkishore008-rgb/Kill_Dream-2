using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class GrappleHook : MonoBehaviour
{
    [Header("References")]
    public Transform camHolder;
    public Transform GunTip;
    public LineRenderer lr;
    public LayerMask Wall_Layer;
    public fpsmovement playerMovement;

    [Header("Settings")]
    public float maxChargeTime = 2f;
    public float maxGrappleDistance = 50f;
    public float flySpeed = 30f;
    public float arcHeight = 2f;
    public float maxArrivalDistance = 1.5f;
    public float MaxFOV = 8f; // Temporary bonus FOV after the grapple.

    public bool IsGrappling { get; private set; }
    public float flyProgress { get; private set; }
    public float flyDuration { get; private set; }

    private Rigidbody rb;
    private Wallrunning wallrunning;
    private float chargeTime;
    private bool charging;
    private Vector3 grappleTarget;
    private Vector3 startPosition;
    private bool oldGravity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        wallrunning = GetComponent<Wallrunning>();
        if (playerMovement == null) playerMovement = GetComponent<fpsmovement>();
        if (camHolder == null && playerMovement != null) camHolder = playerMovement.camHolder;
        if (lr != null) { lr.enabled = false; lr.positionCount = 2; }
    }

    private void Update()
    {
        if (IsGrappling) return;

        if (Input.GetKeyDown(KeyCode.Q)) { charging = true; chargeTime = 0f; }

        if (charging && Input.GetKey(KeyCode.Q)) chargeTime = Mathf.Min(maxChargeTime, chargeTime + Time.deltaTime);


        if (charging && Input.GetKeyUp(KeyCode.Q))
        {
            charging = false;
            if (chargeTime >= 0.1f) FireHook();
        }
    }

    private void FixedUpdate()
    {
        if (!IsGrappling) return;
        float duration = Mathf.Max(0.05f, flyDuration);
        flyProgress = Mathf.Clamp01(flyProgress + Time.fixedDeltaTime / duration);
        float t = flyProgress;
        Vector3 destination = Vector3.Lerp(startPosition, grappleTarget, t);
        destination.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
        rb.MovePosition(destination);
        if (t >= 1f) StopGrapple();
    }

    private void LateUpdate()
    {
        if (IsGrappling && lr != null && lr.enabled)
        {
            lr.SetPosition(0, GunTip != null ? GunTip.position : transform.position);
            lr.SetPosition(1, grappleTarget);
        }
    }

    private void FireHook()
    {
        if (camHolder == null || flySpeed <= 0f) return;
        if (!Physics.Raycast(camHolder.position, camHolder.forward, out RaycastHit hit,
                maxGrappleDistance, Wall_Layer, QueryTriggerInteraction.Ignore)) return;

        if (wallrunning != null) wallrunning.CancelWallrun();
        startPosition = rb.position;
        // Stop slightly short of the wall rather than place the player inside it.
        grappleTarget = hit.point - camHolder.forward * maxArrivalDistance;
        flyDuration = Mathf.Max(0.05f, Vector3.Distance(startPosition, grappleTarget) / flySpeed);
        flyProgress = 0f;
        oldGravity = rb.useGravity;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        IsGrappling = true;
        if (playerMovement != null) playerMovement.SetGrappleMovementLocked(true);
        if (lr != null)
        {
            lr.enabled = true;
            lr.SetPosition(0, GunTip != null ? GunTip.position : transform.position);
            lr.SetPosition(1, grappleTarget);
        }
    }

    private void StopGrapple()
    {
        IsGrappling = false;
        rb.useGravity = oldGravity;
        rb.linearVelocity = Vector3.zero;
        if (lr != null) lr.enabled = false;
        if (playerMovement != null)
        {
            playerMovement.SetGrappleMovementLocked(false);
            playerMovement.AddFovKick(MaxFOV * Mathf.Clamp01(chargeTime / Mathf.Max(0.01f, maxChargeTime)));
        }
    }

    private void OnDisable()
    {
        if (!IsGrappling) return;
        IsGrappling = false;
        if (rb != null) rb.useGravity = oldGravity;
        if (lr != null) lr.enabled = false;
        if (playerMovement != null) playerMovement.SetGrappleMovementLocked(false);
    }
}
