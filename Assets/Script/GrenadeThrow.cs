using System.Collections.Generic;
using Unity.Hierarchy;
using Unity.VisualScripting;
using UnityEngine;

public class GrenadeThrow : MonoBehaviour
{
    public GameObject ProjectilePrefab;
    private bool HasThrown = false;
    private bool CanThrow = false;
    public float throwForce = 2f;
    public float Chargetime;
    public float MinChargetime = .5f;
    public  float MaxChargetime;
    public static bool IsThrown = false;
    public LayerMask EnemyLayer;

    void Start()
    {

    }

    void Update()
    {
        if (Input.GetKey(KeyCode.F))
        {
            Chargetime += Time.deltaTime;

            Chargetime = Mathf.Clamp(Chargetime, 0f, MaxChargetime);

            if (Chargetime >= MinChargetime || Chargetime >= MaxChargetime)
            {
                CanThrow = true;

            }

        }

        if (Input.GetKey(KeyCode.F) && CanThrow)
        {
                Throwables();
                Chargetime = 0f;

                Debug.Log("BOOMMMMMMM");
                Destroy(gameObject);

                IsThrown = true;

        }

    }

    public void Throwables()
    {

        GameObject Grenade = Instantiate(ProjectilePrefab , transform.position , transform.rotation);

        Rigidbody rb = Grenade.GetComponent<Rigidbody>();

        rb.AddForce(Camera.main.transform.forward * throwForce, ForceMode.Impulse);
        rb.AddTorque(new Vector3(Random.Range(-2f,2f), Random.Range(-2f,2f) , Random.Range(-2f, 2f)) , ForceMode.Impulse);

        Inventorynew.instance.ConsumeEquippedItem(1);
    }

}

