using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Experimental.AI;
using UnityEngine.Rendering;
using static Interfaces;

public class EnemyAI : MonoBehaviour , Ishockable 
{
    [Header("Global Volume")]

    public Volume globalvolume;
    private UnityEngine.Rendering.Universal.Vignette Attackvignette;
    private float VignetteTimer;
    public float MaxIntensity = 0.75f;


    protected NavMeshAgent NavMeshAgent;
    protected Animator animator;
    public Transform player;
    //public Transform playerInt;
    public float detectradius = 10;
    public float angle;
    public Transform enemyrad;
    public Transform[] patrolpoints;
    public float waittime = 2f;
    public float stopatdistance;
    public int currentpatrol_index;
    public bool is_Waiting;

    bool hasAttacked;
    public float AttackRadius = 1.2f;
    public Transform TriggerPoint;
    public LayerMask Player_Layer;
    public LayerMask SightMask;
    public LayerMask ObstacleMask;
    public Transform Handenemy;
    public bool IsAttacked = false;
    Camera PlayerCam;

    private Vector3 lastHeardPosition;
    private Vector3 LastKnownPosition;
    private float ViewAngle = 180f;
    private bool heardNoise;
    private float investigateTimer;
    public float investigateTime = 3f;
    public GameObject AlertImg;
    private float ProcessingTimer = 0f;
    private float SightlostTimer = 0f;
    private float Sightlost = .3f;


    [Header("Health")]

    public float health = 50f;

    [Header("Investigating")]

    public float AlertTimer;
    public float investigateElapsed;
    public float MaxinvestigatingTime = 8f;

    public void Recieveshock(float shockpower)
    {
        Debug.Log("Enemyyyyyyyy");
        
    }
    public enum Enemystate
    {
        Patrolling,
        Investigating,
        Searching,
        Following,
        Processing,
        Attack,
        Dead
    }
    private Enemystate _state = Enemystate.Patrolling;

    protected virtual Enemystate CurrentState
    {
        get => _state;
        set => _state = value;
    }
    public virtual IEnumerator waitatpoints()
    {
        is_Waiting = true;
        NavMeshAgent.isStopped = true;

        yield return new WaitForSeconds(waittime);

        GoTonextpatrolpoint();

        is_Waiting = false;
        NavMeshAgent.isStopped = false;
    }

    private void Awake()
    {
        NavMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }
    void Start()
    {
        GoTonextpatrolpoint();
         PlayerCam = Camera.main;
        AlertImg.SetActive(false);
        GameObject PlayerObj = GameObject.FindGameObjectWithTag("Player");
        globalvolume.profile.TryGet<UnityEngine.Rendering.Universal.Vignette>(out Attackvignette);
            
        //playerInt = PlayerInt.transform;
        
        if (PlayerObj != null)
        {
            player = PlayerObj.transform;
        }
        
    }
    public virtual void Update()
    {
        StateMachine();

        if (IsInvestigating())
        {
            AlertImg.SetActive(true);
        }
        else AlertImg.SetActive(false);

    }

    public virtual void StateMachine()
    {
        if (_state == Enemystate.Dead) return;
        if (player == null) return;

        float DistToPlayer = 0f;
        if (player != null)
        {
            DistToPlayer = Vector3.Distance(transform.position, player.transform.position);
        }

        bool seesPlayer = CanseePlayer();

        if (seesPlayer)
        {
            LastKnownPosition = player.transform.position;
        }


        switch (_state)
        {

            case Enemystate.Patrolling:
                patrolling();

                if (is_Waiting)
                {
                    animator.SetBool("Isidle", true);
                    animator.SetBool("IsProcessing", false);
                    animator.SetBool("Iswalking", false);
                    animator.SetBool("IsChasing", false);
                }
                else
                {
                    animator.SetBool("Iswalking", true);
                    animator.SetBool("IsProcessing", false);
                    animator.SetBool("IsChasing", false);
                    animator.SetBool("Isidle", false);

                }


                if (seesPlayer)
                {
                    animator.SetBool("IsProcessing", true);
                    animator.SetBool("Isidle", false);
                    animator.SetBool("Iswalking", false);
                    ProcessingTimer = 0f;
                    SightlostTimer = 0f;

                    _state = Enemystate.Processing;

                    if (is_Waiting)
                    {
                        StopAllCoroutines();
                        is_Waiting = false;
                    }

                }

                break;


            case Enemystate.Processing:
                NavMeshAgent.isStopped = true;
                animator.SetBool("IsProcessing", true);
                animator.SetBool("Isidle", false);
                animator.SetBool("Iswalking", false);
                animator.SetBool("IsChasing", false);

                if (seesPlayer)
                {

                    SightlostTimer = 0f;
                    ProcessingTimer += Time.deltaTime;

                    Vector3 Lookpos = new Vector3(player.position.x,transform.position.y, player.position.z); 
                    transform.LookAt(Lookpos);

                    if (ProcessingTimer >= 3f)
                    {
                        Debug.Log("ALERTED");
                        ProcessingTimer = 0f;

                        lastHeardPosition = player.position;

                        NavMeshAgent.isStopped = false;

                        _state = Enemystate.Following;
                    }
                }

                else
                {
                    SightlostTimer += Time.deltaTime;

                    if (SightlostTimer >= Sightlost)
                    {
                        ProcessingTimer = 0f;
                        SightlostTimer = 0f;
                        NavMeshAgent.isStopped = false;
                        NavMeshAgent.SetDestination(LastKnownPosition);
                        _state = Enemystate.Investigating   ;
                    }


                }
                    

                     break;

                    

                //

            case Enemystate.Investigating:

                NavMeshAgent.isStopped = false;

                animator.SetBool("IsProcessing", false);
                animator.SetBool("Iswalking", true);
                animator.SetBool("IsChasing", false);
                animator.SetBool("Isidle", false);



                 if (seesPlayer)
                 {
                    ProcessingTimer = 0f;
                    SightlostTimer = 0f;
                     _state = Enemystate.Processing;
                     break;
                 }

                investigateElapsed += Time.deltaTime;
                bool Arrived = !NavMeshAgent.pathPending && NavMeshAgent.remainingDistance <= 1f;
                bool canReachIt = !NavMeshAgent.pathPending && NavMeshAgent.hasPath && NavMeshAgent.pathStatus != NavMeshPathStatus.PathComplete;
                bool timedout = investigateElapsed >= MaxinvestigatingTime;

                if (Arrived || canReachIt || timedout)
                {
                    NavMeshAgent.isStopped = true;

                    investigateTimer = 0f;
                    investigateElapsed = 0f;

                    _state = Enemystate.Searching;
                }
                   

                break;

                //

                case Enemystate.Searching:

                NavMeshAgent.isStopped = true;

                animator.SetBool("Iswalking", false);
                animator.SetBool("IsProcessing", false);
                animator.SetBool("IsChasing", false);
                animator.SetBool("Isidle", true);

                investigateTimer += Time.deltaTime;

                if (seesPlayer)
                {
                    investigateTimer = 0f;
                    NavMeshAgent.isStopped = false;
                    _state = Enemystate.Following;
                    break;
                }


                if (heardNoise)
                {
                    heardNoise = false;
                    investigateTimer = 0f;
                    investigateElapsed = 0f;
                    _state = Enemystate.Investigating;
                    break;
                }

                if (investigateTimer >= investigateTime)
                {
                    NavMeshAgent.isStopped = false;
                    investigateTimer = 0f;
                    _state = Enemystate.Patrolling;
                }

                break;

                //

            case Enemystate.Following:
                animator.SetBool("IsChasing", true);
                animator.SetBool("IsProcessing", false);
                animator.SetBool("Iswalking", false);
                animator.SetBool("Isidle", false);

                Following();


                if (!seesPlayer)
                {
                    NavMeshAgent.isStopped = false;
                    NavMeshAgent.SetDestination(LastKnownPosition);


                    _state = Enemystate.Investigating;
                }
                else if (DistToPlayer <= AttackRadius && CanseePlayer())
                {
                    _state = Enemystate.Attack;
                }

                break;

                //  

            case Enemystate.Attack:

                NavMeshAgent.isStopped = true;
                animator.SetBool("IsChasing", false);
                animator.SetBool("IsProcessing", false);


                if (player != null)
                {
                    Vector3 LookPos = new Vector3(player.transform.position.x, transform.position.y, player.transform.position.z);
                    transform.LookAt(LookPos);

                    if (!hasAttacked)
                    {
                        animator.SetTrigger("Isattacking");
                        hasAttacked = true;
                    }
                }
 
                if (DistToPlayer > AttackRadius)
                {
                    hasAttacked = false;
                    NavMeshAgent.isStopped = false;
                    _state = Enemystate.Following;
                }

                break;


        }
    }
    public  virtual void AttackPlayer()
    {
        IsAttacked = true;
        float punchDistance = .2f;
        Vector3 Distance = player.transform.position - transform.position;
       Collider[] punchedEnemies = Physics.OverlapSphere(Handenemy.position, punchDistance, Player_Layer);

        if(punchedEnemies.Length > 0)
        {
           Debug.Log("ENEMY ATTACKED");

            StartCoroutine(VignettePulse());

        }
      
    }

    private IEnumerator VignettePulse()
    {
        float timer = 0f;
        float duration = 0.3f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            Attackvignette.color.value = Color.red;
            float value = Mathf.Sin(timer / duration) * Mathf.PI/2;
            Attackvignette.intensity.value = value * MaxIntensity;

            yield return null;

        } 
            Attackvignette.intensity.value = 0f;
    }

    public virtual void patrolling()
    {
        if (is_Waiting) return;
        if (!NavMeshAgent.pathPending && NavMeshAgent.remainingDistance <= stopatdistance)
        {
            StartCoroutine(waitatpoints());
        }
    }

    public virtual void GoTonextpatrolpoint()
    {
       if (patrolpoints.Length == 0) return;

        var isMoving = NavMeshAgent.velocity.sqrMagnitude > 0.01;
        NavMeshAgent.SetDestination(patrolpoints[currentpatrol_index].position);
        currentpatrol_index = (currentpatrol_index +1) % patrolpoints.Length;

    }

    [Header("Sight settings")]
    public float fovAngle = 160f; 
    public float eyeHeight = 1.6f; 
    public bool debugSight = false;
    
    public virtual bool CanseePlayer()
    {
        if (player == null) return false;

        Vector3 Orgin = (enemyrad != null ? enemyrad.position : transform.position) + Vector3.up * eyeHeight;

        Collider[] playerInRange = Physics.OverlapSphere(transform.position, detectradius, Player_Layer);

        for (int i = 0; i < playerInRange.Length; i++)
        {
            Transform Player = playerInRange[i].transform;

            Vector3 targetpoint = player.position + Vector3.up * eyeHeight;

            Vector3 directionToPlayer = (targetpoint - Orgin).normalized;

            if (Vector3.Angle(transform.forward, directionToPlayer) < ViewAngle / 2f)
            {
                float distanceToPlayer = Vector3.Distance(targetpoint, Orgin);
                
                if (!Physics.Raycast(Orgin, directionToPlayer, out RaycastHit hit , distanceToPlayer, ObstacleMask , QueryTriggerInteraction.Ignore))
                {
                    if (debugSight) Debug.DrawLine(Orgin , targetpoint , Color.green);
                    return true;
                }
                else if (debugSight)
                {
                    Debug.DrawLine(Orgin, hit.point, Color.magenta);
                }
            }
        }
         return false;
    }

   public virtual void Following()
    {
        if (NavMeshAgent.isActiveAndEnabled && NavMeshAgent.isOnNavMesh)
        {
            NavMeshAgent.SetDestination(player.transform.position);
            NavMeshAgent.speed = 7f;
        }
        else
        {
            NavMeshAgent.speed = 3;
        }
    }


    public void TakedownEnemy()
    {
        Debug.Log("Enemy taken down!");

        _state = Enemystate.Dead;

        StopAllCoroutines();

        if (NavMeshAgent.isOnNavMesh)
        {
            NavMeshAgent.isStopped = true;
        }
        NavMeshAgent.enabled = false;

        animator.SetBool("Isidle", false);
        animator.SetBool("Iswalking", false);
        animator.SetBool("IsChasing", false);
        animator.SetBool("IsDead", true);

    }

    public bool IsDead()
    {
        return _state == Enemystate.Dead;
        
    }

    public bool IsInvestigating()
    {
        return _state == Enemystate.Investigating;
    }

    public void takeDamage(float amount)
    {
        health -= amount;
        if (health <= 0f)
        {
            _state = Enemystate.Dead;
            StopAllCoroutines();

            if (NavMeshAgent.isOnNavMesh)
            {
                NavMeshAgent.isStopped = true;
            }

            NavMeshAgent.enabled = false;
            animator.SetBool("Isidle", false);
            animator.SetBool("Iswalking", false);
            animator.SetBool("IsChasing", false);

            animator.SetBool("IsDead", true);
        }
    }

    public void GrenadeDeath()
    {
            _state = Enemystate.Dead;
            StopAllCoroutines();

            if (NavMeshAgent.isOnNavMesh)
            {
                NavMeshAgent.isStopped = true;
            }

            NavMeshAgent.enabled = false;
            animator.SetBool("Isidle", false);
            animator.SetBool("Iswalking", false);
            animator.SetBool("IsChasing", false);

            animator.SetBool("GrenadeDeath", true);

    }


    private void StartInvestigation(Vector3 soundPosition)
    {
        lastHeardPosition = soundPosition;
        investigateTimer = 0f;

        NavMeshAgent.isStopped = false;
        NavMeshAgent.SetDestination(lastHeardPosition);

        _state = Enemystate.Investigating;
    }


    private void HearNoise(NoiseEvent noise)
    {
        if (_state == Enemystate.Dead || _state == Enemystate.Attack || _state == Enemystate.Following || _state == Enemystate.Processing)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position , noise.position);

        if (distance > noise.radius)
        {
            return;
        }

            lastHeardPosition = noise.position;
            heardNoise = true;
            investigateTimer = 0f;
        
            NavMeshAgent.isStopped = false;
        NavMeshAgent.SetDestination(lastHeardPosition);

            _state = Enemystate.Investigating;

        Debug.Log("ENEMY HEARD SOMETHING");
    }

    private void OnEnable()
    {
        SoundManager.OnNoiseMade += HearNoise;
    }

    private void OnDisable()
    {
        SoundManager.OnNoiseMade -= HearNoise;
        
    }

    public virtual void IdleAnimation()
    {
        animator.SetBool("isIdle" , true);
    }

    private void OnDrawGizmos()
    {
        if (enemyrad == null)
            return;

        // Detection radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(enemyrad.position, detectradius);

        // FOV boundaries
        Vector3 leftBoundary =
            Quaternion.Euler(0f, -80f, 0f) * transform.forward;

        Vector3 rightBoundary =
            Quaternion.Euler(0f, 80f, 0f) * transform.forward;

        Gizmos.color = Color.red;

        Gizmos.DrawRay(enemyrad.position, leftBoundary * detectradius);

        Gizmos.DrawRay(enemyrad.position, rightBoundary * detectradius);

        // Center vision direction
        Gizmos.color = Color.green;

        Gizmos.DrawRay(enemyrad.position, transform.forward * detectradius);

        Gizmos.DrawWireSphere(enemyrad.position, detectradius);
        Gizmos.DrawWireSphere(Handenemy.position, .2f);
        Gizmos.DrawCube(TriggerPoint.position, transform.localScale);
    }
}
