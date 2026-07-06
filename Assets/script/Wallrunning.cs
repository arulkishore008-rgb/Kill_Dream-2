using UnityEngine;

public class Wallrunning : MonoBehaviour
{
    [Header("Wallrunning")]
    public LayerMask Iswall;
    public LayerMask Isground;
     
    public float WallrunSpeed;
    public float Wallruntimer;
    public float Wallrunforce;
    public float WallrunTime;
    public float MaxWallruntime;
    public float WallJumpupForce;
    public float WallJumpsideForce;

    [Header("Input")]

    private float horizontalinput;
    private float verticalinput;

    [Header("Detection")]

    public float Wallcheckdistance;
    public float Minjumpheight;
    private RaycastHit Isleftwallhit;
    private RaycastHit Isrightwallhit;
    private bool wallleft;
    private bool wallright;
    public bool IsWallrunning;

    private Rigidbody rb;
    public Transform CamHolder;

    [Header("Exiting")]
    private bool exitingWall;
    public float exitingWalltime;
    public float exitingWallTimer;
    
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        Checkforwall();
        Statemachine();
    }

    private void FixedUpdate()
    {
        if (IsWallrunning)
        {
            WallRunning();
        }
    }

    void Checkforwall()
    {
        wallleft = Physics.Raycast(CamHolder.position, -transform.right, out Isleftwallhit, Wallcheckdistance, Iswall);
        wallright = Physics.Raycast(CamHolder.position, transform.right, out Isrightwallhit, Wallcheckdistance, Iswall);


    }


    void Statemachine()
    {
        horizontalinput = Input.GetAxis("Horizontal");
        verticalinput = Input.GetAxis("Vertical");

        if ((wallleft || wallright ) && verticalinput > 0f && Aboveground() && !exitingWall)
        {
            if (!IsWallrunning)  // 1
            {
                StartWallrun();

            }

                if (Input.GetKeyDown(KeyCode.Space))
                {
                    Walljump();
                }

                if (Wallruntimer > 0)
                    Wallruntimer -= Time.deltaTime;

                if (Wallruntimer <= 0 && IsWallrunning)
                {
                    exitingWall = true;
                    exitingWallTimer = exitingWalltime;
                }

            

            else if (exitingWall)     // 2
            {
                if (IsWallrunning)
                    stopwallrun();

                if (exitingWallTimer > 0)
                    exitingWallTimer -= Time.deltaTime;
                
                if(exitingWalltime <= 0)
                    exitingWall = false;
                
            }

        }

            else    // 3
            {
                if(IsWallrunning)
                 stopwallrun();
            }
    }

    private void WallRunning()
    {
        rb.useGravity = false;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        Vector3 WallNormal = wallright ? Isrightwallhit.normal : Isleftwallhit.normal;
        Vector3 WallForward = Vector3.Cross(WallNormal,transform.up);

        if ((transform.forward - WallForward).magnitude > (transform.forward -  -WallForward).magnitude) 
        {
            WallForward = -WallForward;
        }

        rb.AddForce(WallForward * Wallrunforce, ForceMode.Force);

        if (!(wallleft && horizontalinput > 0) && !(wallright && horizontalinput < 0))
        {
            rb.AddForce(-WallNormal * 100f, ForceMode.Force);
        }

    }
    private bool Aboveground()
    {
        return !Physics.Raycast(transform.position, Vector3.down, Minjumpheight, Isground);
    }
    private void StartWallrun()
    {
        IsWallrunning = true;
        Wallruntimer = MaxWallruntime;
    }
    void stopwallrun()
    {
        IsWallrunning = false;
        rb.useGravity = true;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y, rb.linearVelocity.z);

    }

    void Walljump()
    {
        exitingWall = true;
        exitingWallTimer = exitingWalltime;


        Vector3 Wallnormal = wallright ? Isrightwallhit.normal : Isleftwallhit.normal;

        Vector3 forcetoApply = transform.up * WallJumpupForce + Wallnormal * WallJumpsideForce;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f , rb.linearVelocity.z);
        rb.AddForce(forcetoApply, ForceMode.Impulse);
    }

    private void OnDrawGizmos()
    {
        Debug.DrawRay(CamHolder.position, transform.right * Wallcheckdistance, Color.green);
        Debug.DrawRay(CamHolder.position, -transform.right * Wallcheckdistance, Color.green);
    }
}
