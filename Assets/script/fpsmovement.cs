using System.ComponentModel;
using UnityEngine;
using UnityEngine.EventSystems;
using static Interfaces;

public class fpsmovement : MonoBehaviour 
{
    Rigidbody rb;
    private Wallrunning Wallrunscript;
    public float mousesensitivity;

    public float raylength = 5f;
    public float jumpforce = 5f;

    float mousex;
    float mousey;

    private bool Isjumping;

    
    public float Currentspeed;
    public float walkspeed = 3f;
    public float Sprintspeed = 10f;
    public float Targetspeed = 1f;
    public float acceleration = 5f;


    Vector3 movedirection;
    public Transform Jumpchecktransform;
    public float Jumpchecklength;
    public Transform camHolder;

    public LayerMask Ground;


   

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Wallrunscript = GetComponent<Wallrunning>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        transform.rotation = Quaternion.identity;
        
        
    }

    void Update()
    {

        if (Input.GetKeyDown(KeyCode.E))
        {
            Taser();
        }

        if (Input.GetKeyDown(KeyCode.Space))  // JUMP
        {
            Jump();
        }

        Sprint();

    }

    void FixedUpdate()
    {

        if (Wallrunscript != null && Wallrunscript.IsWallrunning)
        {
            return;
        }
       Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        
        float z = Input.GetAxis("Vertical");
        float x = Input.GetAxis("Horizontal");

        mousex += Input.GetAxis("Mouse X") * mousesensitivity;
        mousey -= Input.GetAxis("Mouse Y") * mousesensitivity;

        

        movedirection = transform.forward * z + transform.right * x;
       rb.linearVelocity = new Vector3(movedirection.x * Currentspeed , rb.linearVelocity.y, movedirection.z * Currentspeed );  
       transform.rotation = Quaternion.Euler(mousey, mousex , transform.rotation.z);

    }


    private void Taser()
    {
        RaycastHit hit;

       if (Physics.Raycast(camHolder.position, camHolder.forward, out hit, raylength))
        {
            Ishockable shockable = hit.collider.GetComponent <Ishockable>();

            if(shockable != null)
            {
                shockable.Recieveshock(10f);
            }

        }

    }


    private void Jump()
    
        {
            if (Physics.Raycast(Jumpchecktransform.position, Vector3.down, Jumpchecklength , Ground))
            {
                Isjumping = false;
                rb.AddForce(Vector3.up * jumpforce, ForceMode.Impulse);
            }

            else
            {
                Isjumping = true; 
            }
        }



    void Sprint()
    {
        Targetspeed = Input.GetKey(KeyCode.LeftShift) ? Sprintspeed : walkspeed;

        Currentspeed = Mathf.Lerp(Currentspeed, Targetspeed, acceleration * Time.deltaTime);
        

    }

    private void OnDrawGizmos()
    {
        Debug.DrawRay(camHolder.position, camHolder.forward * raylength, Color.red);
        Debug.DrawRay(Jumpchecktransform.position, Vector3.down * Jumpchecklength, Color.blue);

    }

}
