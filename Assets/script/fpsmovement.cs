using System.ComponentModel;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
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

    private bool Canjump;

    [Header("Movement")]
    
    public float Currentspeed;
    public float walkspeed = 3f;
    public float Sprintspeed = 10f;
    public float Targetspeed = 1f;
    public float acceleration = 5f;


    Vector3 movedirection;
    public Transform Jumpchecktransform;
    public float Jumpchecklength;


    [Header("Camera Tilt")]

    public Transform camHolder;
    public float Maxtilt = 15f;
    public float Tiltspeed = 5f;
    private float CurrentTilt = 0f;

    [Header("GrappleHook")]

    public float GrappleCheckLength;
    public LayerMask Wall_Layer;
    public float ellapsedtime;
    public float Time_Taken = 4f;


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

        if (Input.GetKeyDown(KeyCode.Space) && Canjump == false)  // JUMP
        {
            if (Wallrunscript != null || !Wallrunscript.IsWallrunning)
            {
                Jump();
            }

        }

        Sprint();
      //  GrapppleHook();
         
       

    }

    void FixedUpdate()
    {

       Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        
        float z = Input.GetAxis("Vertical");
        float x = Input.GetAxis("Horizontal");

        mousex += Input.GetAxis("Mouse X") * mousesensitivity;
        mousey -= Input.GetAxis("Mouse Y") * mousesensitivity;

        mousey = Mathf.Clamp(mousey,-90f,40f);
        transform.rotation = Quaternion.Euler(mousey,mousex,0f);

        movedirection = transform.forward * z + transform.right * x;

        float TargetTilt = 0f;

        if (Wallrunscript != null && Wallrunscript.IsWallrunning)
        {
            if(Wallrunscript.wallleft) TargetTilt = -Maxtilt;
            else if (Wallrunscript.wallright) TargetTilt = Maxtilt;
        }

        CurrentTilt = Mathf.Lerp(CurrentTilt, TargetTilt , Targetspeed * Time.deltaTime);

       rb.linearVelocity = new Vector3(movedirection.x * Currentspeed , rb.linearVelocity.y, movedirection.z * Currentspeed );  

       transform.rotation = Quaternion.Euler(mousey, mousex , CurrentTilt);

        if (camHolder != null)
        {
            camHolder.localRotation = Quaternion.Euler(mousey, 0f, CurrentTilt);
        }

        if (Wallrunscript != null && Wallrunscript.IsWallrunning && Wallrunscript.exitingWall)
        {
            return;

        }
    }


    public void GrapppleHook()
    {
        float t = ellapsedtime / Time_Taken;
        ellapsedtime += Time.deltaTime;
        if (Input.GetMouseButtonDown(0))
        {
            Ray GrappleRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(GrappleRay, out hit) && Physics.Raycast(camHolder.position,camHolder.forward,GrappleCheckLength,Wall_Layer))
            {
                Vector3 clickedPosition = hit.point;

                transform.position = Vector3.Slerp(transform.position,clickedPosition,t);
                Debug.Log(clickedPosition);
            }
                
        }
    }


    private void Jump()
        {

         if (Wallrunscript != null && Wallrunscript.IsWallrunning)
            return;


        if (Physics.Raycast(Jumpchecktransform.position, Vector3.down, Jumpchecklength , Ground))
            {

                Debug.Log("Hitting Ground");
                Canjump = false;
                rb.AddForce(Vector3.up * jumpforce, ForceMode.Impulse);
            }

            else
            {
                Canjump = true; 
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
        Debug.DrawRay(camHolder.position,camHolder.forward * GrappleCheckLength,Color.black);

    }

}
