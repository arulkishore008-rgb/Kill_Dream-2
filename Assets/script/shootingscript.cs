using UnityEngine;

public class shootingscript : MonoBehaviour
{
    public GameObject bullet;
    public float speed;
    public Camera cam;
    public float damage = 10f;
    public float range = 100f;
    public float Timetofire = 1f;
    public float firerate = 15f;
    public float hitforce = 60f;
    void Start()
    {

    }

    void Update()
    {
        if (Input.GetMouseButton(0) && Time.time >= Timetofire) 
        {
            Timetofire = Time.time + 1 / firerate;
            shoot();
        }
    }

    void shoot()
    {
        RaycastHit hit;

       if (Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, range))
        {
            Debug.Log(hit.transform.name);

            Target target = hit.transform.GetComponent<Target>();

            if (target != null)
            {
                target.takeDamage(damage);
            }

            if (hit.rigidbody != null)
            {
                hit.rigidbody.AddForce(hit.normal * hitforce );
            }

        }
    }

}
