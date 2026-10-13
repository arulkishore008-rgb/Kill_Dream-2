using UnityEngine;

public class EnemyDrone : EnemyAI
{
    [SerializeField] private float ds;
    [SerializeField] private float ss;
    [SerializeField] private float sd;

    public Transform[] patrolPoints;

    public override void patrolling()
    {
        base.patrolling();
    }
}
