using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.UI;
using static Interfaces;

public class Taser : MonoBehaviour , Iweapon
{
    public float raylength;
    public float throwForce;
    public float Chargetime;
    public float Countdown;
    public float MinChargetime;
    public float MaxChargetime;
    public bool CanThrow = false;
    public GameObject ProjectilePrerfab;
    private Vector3 StartPoint;

    public float thickRadius;
    public float meleeRange;
    private Camera cam;

    private Rigidbody rb;
    void Start()
    {
         cam = Camera.main;
    }

    void Update()
    {

        if (Input.GetKeyDown(KeyCode.E))
        {
            Taserr();
        }

        if (Input.GetKey(KeyCode.F))
        {

            Chargetime += Time.deltaTime;

            Chargetime = Mathf.Clamp(Chargetime, 0f, MaxChargetime);

            if (Chargetime >= MinChargetime || Chargetime == MaxChargetime)
            {
                CanThrow = true;

            }

        }

            if (Input.GetKeyUp(KeyCode.F))
            {

                if (CanThrow)
                {
                    Debug.Log("RMB RELEASED");
                    Throwables();
                    Destroy(gameObject);
                }
                else
                {
                    Debug.Log("FAILED NOT LONG ENOUGH TO LAUNCH ");
                }
                    Chargetime = 0f;
                    CanThrow = false;
             }

    }
    private void Taserr()
    {

        RaycastHit hit;

         meleeRange = 2.5f; 
         thickRadius = 0.5f; 

         StartPoint = cam.transform.position + (cam.transform.forward * 0.5f); 

        if (Physics.SphereCast(StartPoint, thickRadius, cam.transform.forward, out hit, meleeRange))
        {
            
            Ishockable shockable = hit.collider.GetComponent<Ishockable>();

            if (shockable != null)
            {
                shockable.Recieveshock(10f);
                Debug.Log("Taser MFF");

            }

        }

        if (Inventorynew.instance != null)
        {
            Inventorynew.instance.ConsumeEquippedItem(1);
            Inventorynew.instance.Equipping();
        }

    }
    public void useweapon()
    {
        Debug.Log("gun fire");
    }

    public void Throwables()
    {
        Debug.Log("SPAWNING OBJ");

        if (Inventorynew.instance != null)
        {
            int CurrentCharge = Inventorynew.instance.GetEquippedItemAmount();

            if (CurrentCharge < 3)
            {
                Debug.Log("taser's charge is done");
                return; 
            }
        }

        Vector3 safeSpawnPoint = Camera.main.transform.position + Camera.main.transform.forward;
        GameObject Throwables = Instantiate(ProjectilePrerfab, safeSpawnPoint, transform.rotation);

        Rigidbody rb = Throwables.GetComponent<Rigidbody>();

        if (rb != null)
        {
            Debug.Log("RIGIDBODY FOUND");
            rb.AddForce(Camera.main.transform.forward * throwForce, ForceMode.Impulse);

            rb.AddTorque(new Vector3(Random.Range(-2f, 2f), Random.Range(-2f, 2f), Random.Range(-2f, 2f)), ForceMode.Impulse);
        }
        else
        {
            Debug.Log("NO PREFAB INIT");
        }

        if (Inventorynew.instance != null)
        {
            Debug.Log("5. Consuming ammo and clearing hand.");
            Inventorynew.instance.ConsumeEquippedItem(3);
            Inventorynew.instance.Equipping();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawSphere( StartPoint, thickRadius);
    }

}


