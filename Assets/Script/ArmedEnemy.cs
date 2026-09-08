using TMPro.EditorUtilities;
using Unity.VisualScripting;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

public class ArmedEnemy : EnemyAI 
{
    public enum ArmedEnemyState
    {
        None,
        Alert,
        Combat,
        Reposition,
        SeekCover,
        TakeCover,
        Heal,
        Search,
        Dead

    }

    [Header("References")]
    
    private Animator Armed_animator;
    private NavMeshAgent Agent;
    private Transform FirePoint;

    [Header("Combat")]
    public float CombatRange = 15f;
    public float PrefferedCombatDistance = 8f;
    public float MinimumCombatDistance = 4f;

    [Header("Shooting")]
    private float fireRate = .15f;
    private int bulletPerBurst = 3;
    public float BurstCooldown = 0.8f;

    [Header("Combat Decision")]
    private float DecisionInterval = 2f;
    private float repositionDistance = 4f;

    [Header("Health")]
    public float Maxhealth = 100f;
    public float CurrentHealth = 100f;
    public float lowHealthThreshold = 30f;

    [Header("Healing")]
    public float HealAmount = 30f;
    public float HealDuration = 2f;

    [Header("Searching")]
    private float SearchDuration = 5f;

    private ArmedEnemyState CombatState = ArmedEnemyState.None;

    private float stateTimer;
    private float decisionTimer;
    private float fireTimer;
    private float searchTimer;

    private int BulletsInBurst;

    private float CurrentAnimSpeed;

    Vector3 repositionTarget;
    Vector3 lastKnownPlayerPosition;

    private Transform currentCover;

    private void Start()
    {
        Armed_animator = GetComponent<Animator>();
        Agent = GetComponent<NavMeshAgent>();
    }


    public override void Update()
    {
        StateMachine();
    }
    public override void Following()
    {
        base.Following();
    }

    public override void StateMachine()
    {
        base.StateMachine();

        if(IsDead()) return;

        switch (CombatState)
        {
            case ArmedEnemyState.Alert:
                AlertState();
                break;

            case ArmedEnemyState.Combat:
                combatState();
                break;
            
            case ArmedEnemyState.Reposition:
                RepositionState(); 
                break;

            case ArmedEnemyState.SeekCover:
                SeekCoverState(); 
                break;

            case ArmedEnemyState.TakeCover:
                TakeCoverState();
                break;

            case ArmedEnemyState.Heal:
                HealState();
                break;

            case ArmedEnemyState.Search:
                SearchState(); 
                break;

        }
    }
    public void AlertState()
    {
        if (player == null)
        {
            return;
        }

        Agent.isStopped = true;

        animator.SetBool("Iswalking", false);
        animator.SetBool("Isidle", false);

        FacePlayer();

        stateTimer += Time.deltaTime;

        if (stateTimer >= .4f)
        {
            changeCombatState(ArmedEnemyState.Combat);
        }
    }
     public void combatState()
     {
        if (player == null) return;

        Agent.isStopped = true;

        animator.SetBool("IsWalking", false);
        animator.SetBool("Isidle", false);

        FacePlayer();

        float distance = Vector3.Distance(transform.position, player.position);

        if (CanseePlayer())
        {
            lastKnownPlayerPosition = player.position;
        }
        else
        {
            changeCombatState(ArmedEnemyState.Search);
            return;
        }

        if (distance > CombatRange)
        {
            ClearCombatState();

            return;
        }

        if (CurrentHealth <= lowHealthThreshold)
        {
            changeCombatState(ArmedEnemyState.SeekCover);
            return;
        }

            HandleShooting();

        if (decisionTimer >= DecisionInterval)
        {
            decisionTimer = 0f;

            float decision = Random.value;

            if (distance < MinimumCombatDistance)
            {
                CalculateRepositionTarget();
                changeCombatState(ArmedEnemyState.Reposition);

            }
            else if (decision < 0.35f)
            {
                CalculateRepositionTarget();
                changeCombatState(ArmedEnemyState.Reposition);
            }
        }
     }

     public void RepositionState()
     {
           Agent.isStopped = false;

        animator.SetBool("Iswalking", true);
        animator.SetBool("Isidle", false);

        Agent.SetDestination(repositionTarget);

        if (!Agent.pathPending && Agent.remainingDistance <= .7f)
        {
            changeCombatState(ArmedEnemyState.Combat);
        }

     }

    private void CalculateRepositionTarget()
    {
        if(player == null) { return; }

        Vector3 directionToPlayer = (player.position - transform.position);

        Vector3 SideViewToPlayer = Vector3.Cross(Vector3.up , directionToPlayer);

        if (Random.value > 0.5f)
            SideViewToPlayer *= -1f;

        Vector3 candidate = transform.position + SideViewToPlayer * repositionDistance;


        if (NavMesh.SamplePosition(candidate , out NavMeshHit hit , 3f, NavMesh.AllAreas)) 
        {
            repositionTarget = hit.position;
        }
    }

     public void SeekCoverState()
     {
        Agent.isStopped = false;

        animator.SetBool("Iswalking", true);
        animator.SetBool("isIdle", false);

        if (currentCover == null)
        {
            currentCover = FindBestCover();

            if (currentCover == null)
            {
                changeCombatState(ArmedEnemyState.Combat);
                return;
            }
        }

        Agent.SetDestination(currentCover.position);

        if (!Agent.pathPending && Agent.remainingDistance <= .7f)
        {
            changeCombatState(ArmedEnemyState.TakeCover);
        }


    }

    public Transform FindBestCover()
    {
        return null;
    }

    public void TakeCoverState()
    {
        Agent.isStopped = true;

        animator.SetBool("Iswalking", false);
        animator.SetBool("isIdle", true);

        stateTimer += Time.deltaTime;

        if (CanseePlayer())
        {
            changeCombatState(ArmedEnemyState.Combat);
            return;
        }
        if (stateTimer >= .5f)
        {
            changeCombatState(ArmedEnemyState.Heal);
        }
    }

    public void HealState()
    {
        Agent.isStopped = true;

        animator.SetBool("isIdle",true);

        stateTimer += Time.deltaTime;

        if (CanseePlayer())
        {
            changeCombatState(ArmedEnemyState.Combat);
            return;
        }

        if (stateTimer >= HealDuration)
        {
            CurrentHealth = Mathf.Min(CurrentHealth + HealAmount , Maxhealth);
            currentCover = null;
            changeCombatState(ArmedEnemyState.Combat);

        }
    }

    public void SearchState()
    {
        Agent.isStopped = false;

        animator.SetBool("Iswalking", true);
        animator.SetBool("isIdle", false);

        if (CanseePlayer())
        {
            lastKnownPlayerPosition = player.position;

            changeCombatState(ArmedEnemyState.Combat);
            return;
        }


        if (!Agent.pathPending && Agent.remainingDistance <= 1f)
        {
            Agent.isStopped = true;

            animator.SetBool("Iswalking", false);
            animator.SetBool("Isidle", true);

            searchTimer += Time.deltaTime;

        }
        if (searchTimer >= SearchDuration)
        {
            searchTimer = 0f;

            ClearCombatState();
        }

    }

    public void FacePlayer()
    {
        if(player == null) return;

        Vector3 lookPosition = new Vector3(player.position.x , transform.position.y , player.position.z);

        transform.LookAt(lookPosition);
    }

    public void HandleShooting()
    {
        fireTimer -= Time.deltaTime;

        if(fireTimer > 0f) return;

        FireWeapon();

        BulletsInBurst++;

        if (BulletsInBurst >= bulletPerBurst )
        {
            BulletsInBurst = 0;
            fireTimer = BurstCooldown;
        }
        else
        {
            fireTimer = fireRate;
        }

    }

    public void changeCombatState(ArmedEnemyState newState)
    {
        CombatState = newState;

        stateTimer = 0f;
        decisionTimer = 0f;

        repositionTarget = Vector3.zero;
    }

    private void ClearCombatState()
    {
        CombatState = ArmedEnemyState.None;

        stateTimer = 0f;
        decisionTimer = 0f;
        searchTimer = 0f;

        currentCover = null;
    }

    private void FireWeapon()
    {
        animator.SetBool("Rifle_Shoot", true);
    }

    

}





