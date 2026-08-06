using UnityEngine;

public class Grenade : MonoBehaviour
{
    public float delay = 3f;
    float timer;
    public float Radius;
    public float ExplosiveForce;
    public GameObject ExplosiveEffect;
    bool HasExploded = false;

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
        }

         Destroy(gameObject);
        Destroy(explosion , 4f);
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, Radius);
    }

}