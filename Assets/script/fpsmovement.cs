using System.ComponentModel;
using Unity.Collections;
using Unity.Services.Analytics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Rendering.Universal;
using static Interfaces;

public class fpsmovement : MonoBehaviour 
{
    Rigidbody rb;
    public Transform EnemyInteractPoint;
    private Wallrunning Wallrunscript;
    public float mousesensitivity;

    public float raylength = 5f;
    public float jumpforce = 5f;

    float mousex;
    float mousey;

    public static bool Canjump;

    [Header("Movement")]
    
    public float Currentspeed;
    public float walkspeed = 3f;
    public float Sprintspeed = 10f;
    public float Targetspeed = 1f;
    public float acceleration = 5f;


    Vector3 movedirection;
    public Transform Jumpchecktransform;
    public float Jumpchecklength;
    private int JumpCount;

    public Volume globalvolume;
    private UnityEngine.Rendering.Universal.Vignette vignette;
    public float SprintVignette;
    public float VignetteSpeed;
    public bool IsSprinting;


    [Header("Camera Tilt")]

    public Transform camHolder;
    public float Maxtilt = 15f;
    public float Tiltspeed = 5f;
    private float CurrentTilt = 0f;

    public Camera Camera;
    public GameObject Interactive_img;


    public LayerMask Ground;
    public LayerMask Enemy_Layer;
    private float TakedownTimer = 1f;


    [Header("Camera Movements")]

    Vector3 startPos;
    public float bobSpeed;
    public float bobAmount;
    public float xbobAmount;
    public float Timer;
    public float CurrentFOV = 60f;
    public float SprintFOV = 80f;
    public float SprintFOV_Timer = 3f;
    public float JumpshakeDuration;
    public float CameraShakeTimer;
    public float JumpShakeAmount;
    public float JumpShakeSpeed;
    bool IsShaking = false;

    [Header("Health")]

    public float CurrentHealth = 0f;

    // private Animator anim;

    void Start()
    {
        startPos = camHolder.localPosition;
        rb = GetComponent<Rigidbody>();
        Wallrunscript = GetComponent<Wallrunning>();
      //  anim = GetComponent<Animator>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        transform.rotation = Quaternion.identity;

        if (globalvolume == null) return;

        globalvolume.profile.TryGet<UnityEngine.Rendering.Universal.Vignette>(out vignette);
        

        
        Interactive_img.SetActive(false);

    }

    void Update()
    {
        Canjump = Physics.Raycast(Jumpchecktransform.position, Vector3.down, Jumpchecklength, Ground);

        if (Input.GetKeyDown(KeyCode.Space) && Canjump)  // JUMP
        {
            //if (!Wallrunscript.IsWallrunning )
            {
                {
                    rb.AddForce(Vector3.up * jumpforce , ForceMode.Impulse);

                }
            }

        }

        if (Input.GetAxis("Vertical") > .1f || Input.GetAxis("Horizontal") > .1f || Input.GetAxis("Vertical") < -.1f  || Input.GetAxis("Horizontal") < -.1f)
        {
            Timer += Time.deltaTime * bobSpeed;
            float y = Mathf.Sin(Timer) * bobAmount;
            float xbob = Mathf.Sin(Timer) * xbobAmount;
            camHolder.localPosition = startPos + new Vector3(xbob, y, 0f);

        }
        else
        {
            Timer = 0f;

            camHolder.localPosition = Vector3.Lerp(camHolder.localPosition, startPos, Time.deltaTime * 8f);

        }

        float z = Input.GetAxis("Vertical");
        float x = Input.GetAxis("Horizontal");

        movedirection = transform.forward * z + transform.right * x;

        float TargetTilt = 0f;

        if (Wallrunscript != null && Wallrunscript.IsWallrunning)
        {
            if (Wallrunscript.wallleft) TargetTilt = -Maxtilt;

            else if (Wallrunscript.wallright) TargetTilt = Maxtilt;
        }

        CurrentTilt = Mathf.Lerp(CurrentTilt, TargetTilt, Targetspeed * Time.deltaTime);

         rb.linearVelocity = new Vector3(movedirection.x * Currentspeed, rb.linearVelocity.y, movedirection.z * Currentspeed);

        transform.rotation = Quaternion.Euler(mousey, mousex, CurrentTilt);

        if (camHolder != null)
        {
            camHolder.localRotation = Quaternion.Euler(mousey, 0f, CurrentTilt);
        }

        if (Wallrunscript != null && Wallrunscript.IsWallrunning && Wallrunscript.exitingWall)
        {
            return;

        }

        {
            RaycastHit hitinfo;
            float TakedownRadius = 2.5f;
            float ThickRadius = 0.5f;

            Vector3 StartPoint = Camera.transform.position + (Camera.transform.forward * .5f);

            if (Physics.SphereCast(StartPoint,ThickRadius,Camera.main.transform.forward, out hitinfo,TakedownRadius, Enemy_Layer))
            {
                TakedownTimer -= Time.deltaTime;

                EnemyAI enemy = hitinfo.collider.GetComponentInParent<EnemyAI>();


                if (enemy != null)
                {
                    Vector3 DirectiontoPlayer = (transform.position - enemy.transform.position).normalized;
                    float Angle = Vector3.Angle( enemy.transform.forward,DirectiontoPlayer);

                    if (Angle > 100f)
                    {
                        if (!enemy.IsDead())
                        {
                            Debug.Log("Went into takedown area");
                            Interactive_img.SetActive(true);
                        }

                        if (Input.GetKeyDown(KeyCode.E))
                        {
                           enemy.TakedownEnemy();
                        }

                    }
                    
                }

            }
            else Interactive_img.SetActive (false);

        }

        Sprint();

        UpdateCameraShake();
    }

    void LateUpdate()
    {


        mousex += Input.GetAxis("Mouse X") * mousesensitivity;
        mousey -= Input.GetAxis("Mouse Y") * mousesensitivity;

        mousey = Mathf.Clamp(mousey, -60f, 20f);
        transform.rotation = Quaternion.Euler(0f, mousex, 0f);


        // Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        //float horizontalinput = Input.GetAxis("Horizontal");
        //float verticalinput = Input.GetAxis("Vertical");
        //bool IsMoving = Mathf.Abs(horizontalinput) > .1f || Mathf.Abs(verticalinput) > .1f;

        //if (IsMoving)
        //{
        //}


    }




    //private void Jump()
    //    {

    //     if (Wallrunscript != null && Wallrunscript.IsWallrunning)
    //        return;


    //    if (Physics.Raycast(Jumpchecktransform.position, Vector3.down, Jumpchecklength , Ground))
    //        {

    //            Debug.Log("Hitting Ground");
    //            Canjump = false;
    //            rb.AddForce(Vector3.up * jumpforce, ForceMode.Impulse);
    //        }

    //        else
    //        {
    //            Canjump = true; 
    //        }
    //    }



    void Sprint()
    {

        IsSprinting = Input.GetKey(KeyCode.LeftShift) && Input.GetAxis("Vertical") > .1f;

        Targetspeed = IsSprinting ? Sprintspeed : walkspeed;

        float targetIntensity = IsSprinting ? SprintVignette : 0f;

        float targetFOV = IsSprinting ? SprintFOV : CurrentFOV;
        vignette.color.value = Color.gray;
        Currentspeed = Mathf.Lerp(Currentspeed, Targetspeed, acceleration * Time.deltaTime);
        vignette.intensity.value = (Mathf.Lerp(vignette.intensity.value,targetIntensity, VignetteSpeed * Time.deltaTime));
        Camera.main.fieldOfView  = Mathf.Lerp(Camera.main.fieldOfView, targetFOV, SprintFOV_Timer * Time.deltaTime);

        if (IsSprinting && Currentspeed > 0.1f)
        {
           SoundManager.MakeNoise(transform.position, 5f, gameObject);
        }
        

    }

    private void UpdateCameraShake()
    {
        if (!IsShaking) return;

        CameraShakeTimer += Time.deltaTime;
        float shake = Mathf.Sin(CameraShakeTimer * JumpShakeSpeed) * JumpShakeAmount;
        camHolder.localPosition = startPos + new Vector3(0f, shake, shake);

        if (CameraShakeTimer >= JumpshakeDuration)
        {
            IsShaking = false;
            CameraShakeTimer = 0f;

            camHolder.localPosition = startPos;
        }
    }

    private void OnDrawGizmos()
    {
        Debug.DrawRay(camHolder.position, camHolder.forward * raylength, Color.red);
        Debug.DrawRay(Jumpchecktransform.position, Vector3.down * Jumpchecklength, Color.white);

    }

    //private void OnCollisionEnter(Collision collision)
    //{
    //    if (collision.gameObject.tag == "Ground")
    //    {
    //            Canjump = true;

    //            CameraShakeTimer = 0f;
    //            IsShaking = true;


    //    }
    //}

    //private void OnCollisionExit(Collision collision)
    //{
    //    if (collision.gameObject.tag == "Ground")
    //    {
    //        Canjump = false;
    //    }
        
    //}

    public void TakeDamage()
    {
       
    }


}





