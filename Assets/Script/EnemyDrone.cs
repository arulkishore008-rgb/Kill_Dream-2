using System.ComponentModel.Design;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using static EnemyAI;

public class DroneEnemyAI : MonoBehaviour
{
    public Transform[] Patrolpoints;
    private int currentIndex = 0;
    private Rigidbody rb;
    [SerializeField] private float droneSpeed = 10.0f;

    [SerializeField] private float RotateSpeed = 5f;
    [SerializeField] private float waitTime = 2f;
    [SerializeField] private float SightlostTimer;
    [SerializeField] private float Sightlost = 0.3f;
    [SerializeField] private float ProcessingTimer;
    [SerializeField] private float acceleration;
    [SerializeField] private float attackRange;
    [SerializeField] private float preferredMaintainDistance;
    [SerializeField] private float searchTimer;
    [SerializeField] private float maxSearchDuration;
    [SerializeField] private float scanRotationSpeed;


    private float waitTimer = 0f;
    private bool isWaiting = false;
    private Vector3 lastHeardPosition;
    private Vector3 LastKnownPosition;
    public Transform Playerpos;


    [Header("Detection Settings")]
    public float maxDetectionDistance = 10f;
    public float visionAngle = 45f; 
    public int circleSegments = 12;

    [Header("Obstacle Avoidance")]
    public float obstacleCheckDistance = 3f; 
    public float avoidanceForce = 12f;       
    public LayerMask obstacleLayer;          

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        GameObject Player = GameObject.FindGameObjectWithTag("Player");
        if (Player != null)
        {
            Playerpos = Player.transform;   
        }
        
    }

    private void Update()
    {
        StateMachine();
    }

    public enum DroneState
    {
        Patrolling,
        Investigating,
        Searching,
        Following,
        Processing,
        Attack,
        Dead
    }
    private DroneState State = DroneState.Patrolling;

    protected DroneState CurrentState
    {
        get  => State;
        set => State = value;
    }

    public void StateMachine()
    {
        if (State == DroneState.Dead) return;
        if (Playerpos == null) return;

        bool seesPlayer = DetectPlayer();

        switch (State)
        {
            case DroneState.Patrolling:
                DronePatrol();

                if (seesPlayer)
                {
                    ProcessingTimer = 0f;
                    SightlostTimer = 0f;
                    
                    State = DroneState.Following;

                    if (isWaiting)
                    {
                        isWaiting = false;
                    }
                }

                break ;

                case DroneState.Investigating:
                    break ;


            case DroneState.Processing:

                 rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.deltaTime * 5f);

                if (seesPlayer)
                {
                    SightlostTimer = 0f;
                    ProcessingTimer += Time.deltaTime;

                    Vector3 dirToPlayer = (Playerpos.position - transform.position).normalized;
                    if (dirToPlayer != Vector3.zero)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(dirToPlayer);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, RotateSpeed * Time.deltaTime);
                    }

                    if (ProcessingTimer >= 3f)
                    {
                        Debug.Log("TARGET LOCKED - ENGAGING");
                        ProcessingTimer = 0f;
                        LastKnownPosition = Playerpos.position;
                        State = DroneState.Attack;
                    }
                }
                else
                {
                    SightlostTimer += Time.deltaTime;
                    if (SightlostTimer >= Sightlost)
                    {
                        ProcessingTimer = 0f;
                        SightlostTimer = 0f;
                        State = DroneState.Searching;
                    }
                }
                break;

            case DroneState.Following:
                Vector3 targetFlightPos = seesPlayer ? Playerpos.position : LastKnownPosition;

                if (seesPlayer)
                {
                    LastKnownPosition = Playerpos.position;
                    SightlostTimer = 0f;
                }
                else
                {
                    SightlostTimer += Time.deltaTime;
                }

                FlyToPositionPhysics(targetFlightPos);

                if (seesPlayer && Vector3.Distance(transform.position, Playerpos.position) <= attackRange)
                {
                    State = DroneState.Attack;
                }
                else if (SightlostTimer >= Sightlost)
                {
                    SightlostTimer = 0f;
                    State = DroneState.Searching;
                }
                break;

            case DroneState.Attack:
                if (seesPlayer)
                {
                    LastKnownPosition = Playerpos.position;
                    SightlostTimer = 0f;

                    Vector3 attackDir = (Playerpos.position - transform.position).normalized;
                    if (attackDir != Vector3.zero)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(attackDir);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, RotateSpeed * Time.deltaTime);
                    }

                    float distToPlayer = Vector3.Distance(transform.position, Playerpos.position);

                    if (distToPlayer < preferredMaintainDistance)
                    {
                        Vector3 retreatVelocity = -attackDir * (droneSpeed * 0.5f);
                        rb.AddForce((retreatVelocity - rb.linearVelocity) * acceleration, ForceMode.Acceleration);
                    }
                    else
                    {
                        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.deltaTime * 3f);
                    }

                    attack();
                }
                else
                {
                    SightlostTimer += Time.deltaTime;
                    if (SightlostTimer >= Sightlost)
                    {
                        SightlostTimer = 0f;
                        State = DroneState.Searching;
                    }
                }
                break;

            case DroneState.Searching:

                FlyToPositionPhysics(LastKnownPosition);

                if (Vector3.Distance(transform.position, LastKnownPosition) < 1.5f)
                {

                    rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.deltaTime * 5f);
                    searchTimer += Time.deltaTime;

                    transform.Rotate(Vector3.up, scanRotationSpeed * Time.deltaTime);

                    if (seesPlayer)
                    {
                        searchTimer = 0f;
                        State = DroneState.Processing;
                    }
                    else if (searchTimer >= maxSearchDuration)
                    {
                        searchTimer = 0f;
                        State = DroneState.Patrolling;
                    }
                }
                break;
        }
    }

    void FlyToPositionPhysics(Vector3 targetPosition)
    {
        Vector3 directionToTarget = (targetPosition - transform.position).normalized;

         Vector3 desiredVelocity = directionToTarget * droneSpeed;

         Vector3 avoidance = GetObstacleAvoidanceVector();
        desiredVelocity += avoidance;

         Vector3 velocityChange = desiredVelocity - rb.linearVelocity;
        rb.AddForce(velocityChange * acceleration, ForceMode.Acceleration);

         Vector3 moveDirection = rb.linearVelocity.normalized;
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, RotateSpeed * Time.deltaTime);
        }
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

    public LayerMask detectionMask;
    public bool DetectPlayer()
    {

        if (Playerpos.transform == null) return false;

        Vector3 directionToplayer = (Playerpos.transform.position - transform.position);
        float distanceToplayer = directionToplayer.magnitude;

        if (distanceToplayer > maxDetectionDistance) return false;

        Vector3 downDirection = -transform.up;
        float angleToplayer = Vector3.Angle(downDirection , directionToplayer.normalized);

        if (angleToplayer <= visionAngle / 2f)
        {
            if (Physics.Raycast(transform.position , directionToplayer.normalized, out RaycastHit hit , maxDetectionDistance,detectionMask))
            {
                if (hit.transform == Playerpos.transform)
                {
                    Debug.DrawLine(transform.position , hit.point , Color.red);
                    attack();
                    return true;
                }
                else
                {
                    Debug.DrawLine(transform.position, hit.point, Color.green);
                    return false;
                }
            }
        }
        return false;
    }


    void attack()
    {
        Debug.Log("playerrrrr");
    }

    Vector3 GetObstacleAvoidanceVector()
    {
        Vector3 avoidanceVector = Vector3.zero;

         Vector3[] directions = new Vector3[]
        {transform.forward,(transform.forward - transform.right).normalized,(transform.forward + transform.right).normalized,(transform.forward + transform.up).normalized,(transform.forward - transform.up).normalized};

        foreach (Vector3 dir in directions)
        {
            if (Physics.Raycast(transform.position, dir, out RaycastHit hit, obstacleCheckDistance, obstacleLayer))
            {
                 float hitFactor = 1f - (hit.distance / obstacleCheckDistance);  
                avoidanceVector += hit.normal * (avoidanceForce * hitFactor);

                Debug.DrawLine(transform.position, hit.point, Color.red);  
            }
        }

        return avoidanceVector;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.5f); 
        Vector3 centerPoint = transform.position;
        Vector3 downDirection = -transform.up;

        float baseRadius = maxDetectionDistance * Mathf.Tan((visionAngle/2f) * Mathf.Deg2Rad);
        Vector3 centerBasepoint = centerPoint + (downDirection * maxDetectionDistance);

        Gizmos.DrawLine(centerPoint, centerBasepoint);

        Vector3 previousPoint = Vector3.zero;
        Vector3 firstPoint = Vector3.zero;

        for (int i = 0; i < circleSegments; i++)
        {
            float stepAngle = (i * 360f / circleSegments) * Mathf.Deg2Rad;

            Vector3 localOffset = new Vector3(Mathf.Cos(stepAngle) * baseRadius, 0f, Mathf.Sin(stepAngle) * baseRadius);
            Vector3 worldPoint = centerBasepoint + transform.TransformDirection(localOffset);

            Gizmos.DrawLine(centerPoint, worldPoint);

            if (i > 0)
            {
                Gizmos.DrawLine(previousPoint, worldPoint);
            }
            else
            {
                firstPoint = worldPoint;
            }

            previousPoint = worldPoint;
        }

        Gizmos.DrawLine(previousPoint, firstPoint);
    }

}