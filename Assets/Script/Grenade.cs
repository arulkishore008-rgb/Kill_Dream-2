using UnityEngine;

public class Grenade : MonoBehaviour
{
    public float delay = 3f;
    float timer;
    public float Radius;
    public float ExplosiveForce;
    public GameObject ExplosiveEffect;
    bool HasExploded = false;
    public LayerMask EnemyLayer;


    private void Start()
    {
        timer = delay;
    }

    private void Update()
    {
       timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            Explode();
        }
    }

    public void Explode()
    {
        GameObject explosion = Instantiate(ExplosiveEffect,transform.position,transform.rotation);

        Collider[] colliders = Physics.OverlapSphere(transform.position, Radius);

        foreach (Collider NearbyObj in colliders)
        {
            Rigidbody rb = NearbyObj.GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.AddExplosionForce(ExplosiveForce, transform.position, Radius);
            }

            Debug.Log(
                "Detected: " +
                NearbyObj.name +
                " | Parent: " +
                NearbyObj.transform.parent?.name +
                " | Root: " +
                NearbyObj.transform.root.name);




         EnemyAI enemy = NearbyObj.GetComponentInParent<EnemyAI>();

                if (enemy != null) 
                {
                 print("ENEMY ENTERED GRENADE ZONE");
                    enemy.GrenadeDeath();
                }
        }

         Destroy(gameObject);
        Destroy(explosion , 4f);
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, Radius);
    }

}

