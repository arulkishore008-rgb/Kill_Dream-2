using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using static Interfaces;

public class EnemyAI : MonoBehaviour , Ishockable
{
    private NavMeshAgent NavMeshAgent;
    private Animator animator;
    public Transform player;
    public float detectradius = 10;
    public float angle;
    public Transform enemyrad;
    public Transform[] patrolpoints;
    public float waittime = 2f;
    public float stopatdistance;
    public int currentpatrol_index;
    public bool is_Waiting;

    public void Recieveshock(float shockpower)
    {
        Debug.Log("Enemyyyyyyyy");
        
        NavMeshAgent.isStopped = true;

    }
    private enum Enemystate
    {
        Patrolling,
        Following,
        Attack
    }
    private Enemystate _state = Enemystate.Patrolling;
    private IEnumerator waitatpoints()
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

        GameObject playerobj = GameObject.FindGameObjectWithTag("Player");
            
        if(player != null)
        {
            player = playerobj.transform;
        }
    }

   

    void Update()
    {

        Vector3 direction = transform.position - player.position;
         angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Vector3 distance = transform.position - player.transform.position;

        switch (_state)
        {
            case Enemystate.Patrolling:
            patrolling();
            animator.SetBool("Iswalking", true);


                if (CanseePlayer())
                {
                    animator.SetBool("Isidle", false);
                    _state = Enemystate.Following;
                }


       
            break;

                case Enemystate.Following:
                animator.SetBool("Iswalking", true);

                Following();
                if (!CanseePlayer())
                {
                    animator.SetBool("Isidle", true);

                    _state = Enemystate.Patrolling;
                }

                break;

            case Enemystate.Attack:

                {
                animator.SetBool("Isattacking", true);

                }
                break;

            
        }




    }

    private void patrolling()
    {
        if (is_Waiting) return;
        if (!NavMeshAgent.pathPending && NavMeshAgent.remainingDistance <= stopatdistance)
        {
            StartCoroutine(waitatpoints());
        }
    }

    private void updateanimations()
    {

    }

    void GoTonextpatrolpoint()
    {
       if (patrolpoints.Length == 0) return;


        var isMoving = NavMeshAgent.velocity.sqrMagnitude > 0.01;
        NavMeshAgent.SetDestination(patrolpoints[currentpatrol_index].position);
        currentpatrol_index = (currentpatrol_index +1) % patrolpoints.Length;

    }

    bool CanseePlayer()
    {
        if (player == null) return false; 
        Collider[] hits = Physics.OverlapSphere(enemyrad.position, detectradius);

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player") && angle < 120f)
            {
                return true;
            }

        }

        return false;
    }

    void Following()
    {
        NavMeshAgent.SetDestination(player.transform.position);

    }
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(enemyrad.position, detectradius);
    }
}
