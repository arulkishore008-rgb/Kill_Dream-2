using UnityEngine;

public class GrappleHook : MonoBehaviour
{
    [Header("References")]
    public Transform camHolder;
    public LayerMask Wall_Layer;
    public fpsmovement playerMovement; 

    [Header("Settings")]
    public float maxChargeTime = 2f;
    public float maxGrappleDistance = 50f;
    public float flySpeed = 30f; 
    // The "States"
    private float chargeTime = 0f;
    private bool isCharging = false;
    private bool isFlying = false;
    private Vector3 grappleTarget;

    public float MaxFOV;
    public float CurrentFOV;
    public float LerpTime;
    private float CamFOV;
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

    void FireHook()
    {
        Ray ray = new Ray(camHolder.position, camHolder.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, maxGrappleDistance, Wall_Layer))
        {
            grappleTarget = hit.point;
            isFlying = true; 
    
            if (playerMovement != null) playerMovement.enabled = false;
        }
        else
        {
            Debug.Log("Grapple missed! Nothing in range.");
        }
    }

    void FlyToTarget()
    {
        transform.position = Vector3.Lerp(transform.position, grappleTarget, flySpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, grappleTarget) < 1.5f)
        {
            isFlying = false;

            float chargePercent = chargeTime / maxChargeTime;

            Camera.main.fieldOfView = Mathf.Lerp(CurrentFOV, MaxFOV, chargePercent);


            if (playerMovement != null) playerMovement.enabled = true;

            Debug.Log("Grapple Complete!");
        }
    }
}
