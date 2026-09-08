using UnityEngine;

public class shootingscript : MonoBehaviour
{
    private Camera cam;
    public float damage = 10f;
    public float range = 100f;
    public float Timetofire = 1f;
    public float firerate = 15f;
    public float hitforce = 60f;

   // public GameObject SmokeEffect;
   // public Transform SmokeTrans;
    public Animator Animator;

    void Start()
    {
        cam = Camera.main;
        Animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) 
        {
            Animator.SetBool("shoot", true);
            SoundManager.MakeNoise(transform.position, 50f, gameObject);

           // Timetofire = Time.time + 1 / firerate;
            shoot();
        }

        else  
             Animator.SetBool("shoot", false);
    }

    void shoot()
    {
        RaycastHit hit;

       // GameObject smokeefx = Instantiate(SmokeEffect,SmokeTrans.position,cam.transform.rotation);
       // Destroy(smokeefx,1f);

       if (Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, range))
        {
            //Debug.Log(hit.transform.name);

            EnemyAI target = hit.transform.GetComponent<EnemyAI>();

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
