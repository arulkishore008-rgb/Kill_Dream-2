using System.Security.Cryptography.X509Certificates;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using static UnityEngine.UI.Image;

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
    // The "States"
    private float chargeTime = 0f;
    private bool isCharging = false;
    private bool isFlying = false;
    private Vector3 grappleTarget;
    private Vector3 StartPosition;
    public float flyProgress = 0f;
    public float flyDuration = 1f;

    public float MaxFOV;
    public float CurrentFOV;
    public float LerpTime;
    private float CamFOV;


    private void Start()
    {
        if (lr != null)
        {
            lr.enabled = false;
        }
    }
    void Update()
    {
        if (isFlying)
        {
            FlyToTarget();
            
            return;
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            isCharging = true;
            chargeTime = 0f;
        }

        if (Input.GetKey(KeyCode.Q) && isCharging)
        {           
            chargeTime += Time.deltaTime;
            chargeTime = Mathf.Clamp(chargeTime, 0f, maxChargeTime);
        }

        if (Input.GetKeyUp(KeyCode.Q) && isCharging)
        {
            isCharging = false;
            //Camera.main.fieldOfView = 80f;
            if (chargeTime >= 0.1f)
            {
                FireHook();
            }
        }
    }

    void LateUpdate()
    {
        if (isFlying && lr != null && lr.enabled)
        {
            lr.SetPosition(0, GunTip != null ? GunTip.position : transform.position);
            lr.SetPosition(1, grappleTarget);
        }
    }

    void FireHook()
    {
        Ray ray = new Ray(camHolder.position, camHolder.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, maxGrappleDistance, Wall_Layer))
        {
            grappleTarget = hit.point;
            StartPosition = transform.position;
            float distance = Vector3.Distance(StartPosition, grappleTarget);
            flyDuration = distance / flySpeed;
            flyProgress = 0f;

            isFlying = true;

            if (lr != null)
            {
                lr.positionCount = 2;
                lr.SetPosition(0, GunTip != null ? GunTip.position : transform.position);
                lr.SetPosition(1, grappleTarget);
                lr.enabled = true;
            }

            if (playerMovement != null) playerMovement.enabled = false;
        }
        else
        {
            Debug.Log("Grapple missed! Nothing in range.");
        }
    }

    void FlyToTarget()
    {
        flyProgress += Time.deltaTime / flyDuration;
        float t = Mathf.Clamp01(flyProgress);

        Vector3 linearPosition = Vector3.Lerp(StartPosition ,grappleTarget ,t);

        float CurrentArcHeight = Mathf.Sin(t * Mathf.PI) * arcHeight; 

        transform.position = linearPosition + (Vector3.up * CurrentArcHeight);

        if (t >= 1f)
        {
          StopGrapple();        
        }
    }


    void StopGrapple()
    {
        isFlying = false;

        // Turn off line visual upon arrival
        if (lr != null)
        {
            lr.enabled = false;
        }

        float chargePercent = chargeTime / maxChargeTime;
        Camera.main.fieldOfView = Mathf.Lerp(CurrentFOV, MaxFOV, chargePercent);

        if (playerMovement != null) playerMovement.enabled = true;
    }
}
