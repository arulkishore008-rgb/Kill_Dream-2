using TMPro.EditorUtilities;
using Unity.VisualScripting;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

public class ArmedEnemy : EnemyAI
{

    [Header("References")]

   // private new Animator Armed_animator;
   // private NavMeshAgent Agent;
    private Transform FirePoint;

    [Header("Combat")]
    public float CombatRange = 15f;
    public float PreferredShootingDistance = 8f;
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


    private float stateTimer;
    private float decisionTimer;
    private float fireTimer;
    private float searchTimer;

    private int BulletsInBurst;

    private float CurrentAnimSpeed;

    Vector3 repositionTarget;
    Vector3 lastKnownPlayerPosition;

    private CoverPoint currentCover;

    private new void Start()
    {
       // Armed_animator = GetComponent<Animator>();
        //Agent = GetComponent<NavMeshAgent>();
    }


    public override void Update()
    {
        //base.Update();

        CoverPoint targetCover = SeekingbestCover();

        if (targetCover != null)
        {
            targetCover.isOccupied = true;
            targetCover.currentOccupant = transform;

            NavMeshAgent.isStopped = false;
            NavMeshAgent.SetDestination(targetCover.transform.position);
        }
    }

    public override void Following()
    {
        if (player == null) return;

        if (NavMeshAgent.isActiveAndEnabled && NavMeshAgent.isOnNavMesh)
        {
            NavMeshAgent.isStopped = false;
            NavMeshAgent.speed = 5f;

           float distance = Vector3.Distance(transform.position , player.position);


            if (distance < PreferredShootingDistance - 2f)
            {
                Vector3 DirectionAwayfromPlayer = (transform.position - player.position).normalized;

                Vector3 Retreatposition = transform.position + DirectionAwayfromPlayer * PreferredShootingDistance;

                NavMeshAgent.isStopped = false ;
                NavMeshAgent.SetDestination(Retreatposition);
            }
            else if (distance > PreferredShootingDistance + 2f)
            {
                NavMeshAgent.isStopped = false;
                NavMeshAgent.SetDestination(player.position);
            }
   
            else
            {
                NavMeshAgent.isStopped = true;
                NavMeshAgent.ResetPath();
                print("SHOOTING ");
                Vector3 lookPos = new Vector3(player.position.x, transform.position.y, player.position.z);
                transform.LookAt(lookPos);
            }
        }
       
    }

    public CoverPoint SeekingbestCover()
    {
        CoverPoint[] allCovers = FindObjectsByType<CoverPoint>(FindObjectsSortMode.None);

        CoverPoint bestCover = null;
        float closestDistance = Mathf.Infinity;

        foreach (CoverPoint cover in allCovers)
        {
            if (cover.isOccupied) continue;

            float distance = Vector3.Distance(transform.position, cover.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                bestCover = cover;
            }
            
        }

        return bestCover;
    }

    void ReleaseCover()
    {
        if (currentCover != null)
        {
            currentCover.isOccupied = false;
            currentCover.currentOccupant = null;
            currentCover = null;
        }
    }
}


