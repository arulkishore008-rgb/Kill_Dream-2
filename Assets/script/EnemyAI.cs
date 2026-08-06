using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using static Interfaces;

public class EnemyAI : MonoBehaviour , Ishockable 
{
    private NavMeshAgent NavMeshAgent;
    private Animator animator;
    public Transform player;
    public Transform playerInt;
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
    public Transform Handenemy; 


    public void Recieveshock(float shockpower)
    {
        Debug.Log("Enemyyyyyyyy");
        
        NavMeshAgent.isStopped = true;

    }
    public enum Enemystate
    {
        Patrolling,
        Following,
        Attack,
        Dead
    }
    private Enemystate _state = Enemystate.Patrolling;
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

        GameObject PlayerObj = GameObject.FindGameObjectWithTag("Player");
        GameObject PlayerInt = GameObject.FindGameObjectWithTag("PlayerInteractivePoint");
            
        playerInt = PlayerInt.transform;
        
        if (PlayerObj != null)
        {
            player = PlayerObj.transform;
        }
        

    }
    public virtual void Update()
    {
        StateMachine();    
    }

    public virtual void StateMachine()
    {
        if (_state == Enemystate.Dead) return;

        float DistToPlayer = 0f;
        if (player != null)
        {
            DistToPlayer = Vector3.Distance(transform.position, player.transform.position);
        }

        Vector3 direction = transform.position - player.position;
        angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Vector3 distance = transform.position - player.transform.position;

        switch (_state)
        {
            case Enemystate.Patrolling:
                patrolling();

                if (is_Waiting)
                {
                    animator.SetBool("Iswalking", false);
                    animator.SetBool("Isidle", true);
                }
                else
                {
                    animator.SetBool("Iswalking", true);
                    animator.SetBool("Isidle", false);

                }


                if (CanseePlayer())
                {
                    animator.SetBool("Isidle", false);
                    _state = Enemystate.Following;
                }



                break;

            case Enemystate.Following:
                animator.SetBool("Iswalking", true);
                animator.SetBool("Isidle", false);

                Following();


                if (!CanseePlayer())
                {
                    _state = Enemystate.Patrolling;
                }
                else if (DistToPlayer <= AttackRadius && CanseePlayer())
                {
                    _state = Enemystate.Attack;
                }

                break;

            case Enemystate.Attack:

                NavMeshAgent.isStopped = true;
                animator.SetBool("Iswalking", false);

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
        float punchDistance = .2f;
        Vector3 Distance = playerInt.transform.position - transform.position;
       Collider[] punchedEnemies = Physics.OverlapSphere(Handenemy.position, punchDistance, Player_Layer);

        foreach (Collider punchedEnemy in punchedEnemies)
        {
           Debug.Log("ENEMY ATTACKED");
        }
      
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

    public virtual bool CanseePlayer()
    {
        if (player == null) return false; 
        Collider[] hits = Physics.OverlapSphere(enemyrad.position, detectradius);

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player") )
            {

                Vector3 directionToPlayer = (player.transform.position - transform.position).normalized;
                float AngleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);

                if (AngleToPlayer < 80f)
                {
                    return true;
                }

            }

        }

        return false;
    }

   public virtual void Following()
    {

        if (NavMeshAgent.isActiveAndEnabled && NavMeshAgent.isOnNavMesh)
        {
            NavMeshAgent.SetDestination(playerInt.transform.position);
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
        animator.SetBool("IsDead", true);

    }

    public bool IsDead()
    {
        return _state == Enemystate.Dead;
    }
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(enemyrad.position, detectradius);
        Gizmos.DrawWireSphere(Handenemy.position, .2f);
        Gizmos.DrawCube(TriggerPoint.position, transform.localScale );
    }
}
