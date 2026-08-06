using UnityEngine;

public class LandMine : MonoBehaviour
{
    public float MineRadius;
    public float MineBlastForce;
    public LayerMask EnemyLayer;
    void Start()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            MineExplode();
        }
    }

    public void MineExplode()
    {
        Collider[] Colliders = Physics.OverlapSphere(transform.position, MineRadius, EnemyLayer);

        foreach (Collider SteppedEnemy in Colliders)
        {
            Rigidbody rb = SteppedEnemy.GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.AddExplosionForce(MineBlastForce, transform.position, MineRadius);
            }

        }

        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawSphere(transform.position, MineRadius);
    }

}
