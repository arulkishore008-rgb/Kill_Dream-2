using UnityEngine;
using UnityEngine.AI;

public class DroneEnemyAI : MonoBehaviour
{
    private NavMeshAgent agent;
    public Transform[] Patrolpoints;
    private int currentIndex = 0;
    [SerializeField] private float droneSpeed = 10.0f;

    [SerializeField] private float RotateSpeed = 5f;
    [SerializeField] private float waitTime = 2f;
    private float waitTimer = 0f;
    private bool isWaiting = false;



    private void Update()
    {
        DronePatrol();  
    }
    void DronePatrol()
    {
        if (Patrolpoints.Length == 0) return;


        if (isWaiting)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitTime)
            {
                isWaiting = false;
                waitTimer = 0f;
                currentIndex = (currentIndex + 1) % Patrolpoints.Length;
            }
            return; 
        }

        Transform targetPoint = Patrolpoints[currentIndex];

        Vector3 direction = (targetPoint.position - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, RotateSpeed * Time.deltaTime);
        }

        transform.position = Vector3.MoveTowards(transform.position, Patrolpoints[currentIndex].position,droneSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, Patrolpoints[currentIndex].position) < .1f)
        {
            isWaiting = true;
        }
        
    }
}