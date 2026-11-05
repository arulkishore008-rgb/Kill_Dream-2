using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static Interfaces;

[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour, Ishockable
{
    public enum Enemystate
    {
        Patrolling,
        Investigating,
        Searching,
        Following,
        Processing,
        Attack,
        Dead,
        Returning,
        HitReact
    }

    [Header("References")]
    public Transform player;
    public Transform enemyrad;
    public Transform[] patrolpoints;
    public Transform Handenemy;
    public Transform TriggerPoint;
    public GameObject AlertImg;
    public LayerMask Player_Layer;
    public LayerMask SightMask;
    public LayerMask ObstacleMask;
    public Volume globalvolume;

    [Header("Patrol")]
    public float waittime = 2f;
    public float stopatdistance = 0.45f;
    public float patrolSpeed = 2.5f;
    public int currentpatrol_index;
    public bool is_Waiting;

    [Header("Vision")]
    public float detectradius = 12f;
    [Range(1f, 360f)] public float fovAngle = 160f;
    [Range(1f, 360f)] public float alertedFovAngle = 270f;
    public float closeAwarenessRadius = 3f;
    public float eyeHeight = 1.6f;
    public float playerTargetHeight = 1.2f;
    public float sightLostGrace = 1.6f;
    public bool debugSight;
    public float angle;

    [Header("Detection")]
    public float processingTime = 1.4f;
    public float suspicionDecayRate = 0.65f;
    public float turnSpeed = 540f;
    public float investigateTime = 1.1f;
    public float MaxinvestigatingTime = 8f;
    public float AlertTimer;
    public float investigateElapsed;

    [Header("Chase")]
    public float chaseSpeed = 6.5f;
    public float destinationRefreshInterval = 0.2f;
    public float destinationChangeThreshold = 0.55f;

    [Header("Search")]
    public float searchDuration = 9f;
    public float searchRadius = 5f;
    [Range(1, 8)] public int searchPoints = 4;

    [Header("Melee Combat")]
    public float AttackRadius = 1.8f;
    public float attackExitBuffer = 0.4f;
    public float attackDamage = 15f;
    public float attackHitRadius = 0.6f;
    [Range(1f, 180f)] public float attackHitAngle = 110f;
    public float attackCooldown = 0.7f;
    public float maximumAttackDuration = 1.5f;
    public float fallbackHitTime = 0.4f;
    public bool useAnimationEvents = true;
    [Range(1, 3)] public int attackVariationCount = 3;
    public bool IsAttacked;

    [Header("Health And Hit Reaction")]
    public float health = 50f;
    public float hitStaggerThreshold = 12f;
    public float hitStaggerDuration = 0.35f;
    public float staggerImmunityTime = 0.65f;

    [Header("Nearby Ally Alert")]
    public bool alertNearbyEnemies = true;
    public float allyAlertRadius = 12f;
    public float allyAlertCooldown = 8f;

    [Header("Hit Feedback")]
    public float MaxIntensity = 0.75f;
    public float hitVignetteDuration = 0.35f;

    [Header("Debug")]
    public bool logStateChanges;

    protected NavMeshAgent NavMeshAgent;
    protected Animator animator;

    private Enemystate _state = Enemystate.Patrolling;
    private readonly HashSet<int> animatorParameters = new HashSet<int>();
    private Vignette attackVignette;
    private Coroutine vignetteCoroutine;
    private float originalVignetteIntensity;
    private Color originalVignetteColor;
    private float patrolWaitRemaining;
    private float processingProgress;
    private float lastVisibleTime = float.NegativeInfinity;
    private float nextDestinationRefresh;
    private float investigateRemaining;
    private float searchElapsed;
    private float searchPauseRemaining;
    private float hitReactRemaining;
    private float returnElapsed;
    private float nextAllowedStagger;
    private float nextAttackTime;
    private float attackElapsed;
    private float lastAllyAlertTime = float.NegativeInfinity;
    private int lastAttackIndex = -1;
    private int searchPointsVisited;
    private int returnPatrolIndex;
    private Vector3 lastKnownPosition;
    private Vector3 destination;
    private Vector3 searchLookDirection;
    private bool hasKnownPosition;
    private bool targetVisible;
    private bool targetAlerted;
    private bool attackInProgress;
    private bool attackHitApplied;
    private bool searchingForPoint;
    private bool hasDestination;
    private bool warnedMissingDamageReceiver;

    private static readonly int IsIdleHash = Animator.StringToHash("Isidle");
    private static readonly int IsWalkingHash = Animator.StringToHash("Iswalking");
    private static readonly int IsChasingHash = Animator.StringToHash("IsChasing");
    private static readonly int IsProcessingHash = Animator.StringToHash("IsProcessing");
    private static readonly int IsDeadHash = Animator.StringToHash("IsDead");
    private static readonly int GrenadeDeathHash = Animator.StringToHash("GrenadeDeath");
    private static readonly int IsAttackingHash = Animator.StringToHash("Isattacking");
    private static readonly int AttackIndexHash = Animator.StringToHash("AttackIndex");
    private static readonly int HitReactHash = Animator.StringToHash("HitReact");

    protected virtual Enemystate CurrentState
    {
        get => _state;
        set => _state = value;
    }

    public Enemystate DebugState => CurrentState;
    public Vector3 LastKnownPlayerPosition => lastKnownPosition;
    public bool HasLastKnownPlayerPosition => hasKnownPosition;
    public bool HasVisualContact => targetVisible;

    protected virtual void Awake()
    {
        NavMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        CacheAnimatorParameters();
    }

    public virtual void Start()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }

        if (AlertImg != null)
            AlertImg.SetActive(false);

        if (globalvolume != null && globalvolume.profile != null && globalvolume.profile.TryGet(out attackVignette))
        {
            originalVignetteIntensity = attackVignette.intensity.value;
            originalVignetteColor = attackVignette.color.value;
        }

        if (AgentReady())
        {
            NavMeshAgent.speed = patrolSpeed;
            GoTonextpatrolpoint();
        }
        else
        {
            Debug.LogWarning($"{name}: Enemy has no valid NavMesh position.", this);
        }
    }

    protected virtual void OnEnable()
    {
        SoundManager.OnNoiseMade += HearNoise;
    }

    protected virtual void OnDisable()
    {
        SoundManager.OnNoiseMade -= HearNoise;
        RestoreVignette();
    }

    public virtual void Update()
    {
        if (CurrentState == Enemystate.Dead)
            return;

        targetVisible = CanseePlayer();
        if (targetVisible && player != null)
        {
            RememberPosition(player.position);
            lastVisibleTime = Time.time;
            targetAlerted = true;
        }

        StateMachine();
        UpdateAnimatorState();

        if (AlertImg != null)
            AlertImg.SetActive(IsInvestigating() || CurrentState == Enemystate.Following || CurrentState == Enemystate.Attack);
    }

    public virtual void StateMachine()
    {
        if (CurrentState == Enemystate.Dead)
            return;

        switch (CurrentState)
        {
            case Enemystate.Patrolling:
                UpdatePatrol();
                if (targetVisible)
                    ChangeState(Enemystate.Processing);
                break;

            case Enemystate.Processing:
                UpdateProcessing();
                break;

            case Enemystate.Following:
                UpdateChase();
                break;

            case Enemystate.Attack:
                UpdateAttack();
                break;

            case Enemystate.Investigating:
                UpdateInvestigation();
                break;

            case Enemystate.Searching:
                UpdateSearch();
                break;

            case Enemystate.Returning:
                UpdateReturnToPatrol();
                break;

            case Enemystate.HitReact:
                UpdateHitReaction();
                break;
        }
    }

    private void UpdatePatrol()
    {
        if (!AgentReady())
            return;

        if (patrolpoints == null || patrolpoints.Length == 0)
        {
            StopAgent();
            return;
        }

        if (is_Waiting)
        {
            patrolWaitRemaining -= Time.deltaTime;
            if (patrolWaitRemaining <= 0f)
            {
                is_Waiting = false;
                GoTonextpatrolpoint();
            }
            return;
        }

        if (ReachedDestination(stopatdistance))
        {
            is_Waiting = true;
            patrolWaitRemaining = waittime;
            StopAgent();
            return;
        }

        if (!NavMeshAgent.hasPath && !NavMeshAgent.pathPending)
            GoTonextpatrolpoint();
    }

    private void UpdateProcessing()
    {
        StopAgent();
        FacePosition(targetVisible && player != null ? player.position : lastKnownPosition);

        if (targetVisible)
        {
            processingProgress += Time.deltaTime;
            if (processingProgress >= processingTime)
            {
                ChangeState(Enemystate.Following);
                AlertAllies();
            }
            return;
        }

        processingProgress = Mathf.Max(0f, processingProgress - suspicionDecayRate * Time.deltaTime);
        if (Time.time - lastVisibleTime > sightLostGrace)
            ChangeState(hasKnownPosition ? Enemystate.Investigating : Enemystate.Patrolling);
    }

    protected virtual void UpdateChase()
    {
        if (ShouldEnterAttack())
        {
            ChangeState(Enemystate.Attack);
            return;
        }

        Following();

        if (!targetVisible && Time.time - lastVisibleTime >= sightLostGrace)
            ChangeState(Enemystate.Investigating);
    }

    protected virtual bool ShouldEnterAttack()
    {
        return targetVisible && player != null && Vector3.Distance(transform.position, player.position) <= AttackRadius;
    }

    protected float TimeSinceLastSight => Time.time - lastVisibleTime;

    public virtual void Following()
    {
        if (!AgentReady())
            return;

        NavMeshAgent.speed = chaseSpeed;
        NavMeshAgent.stoppingDistance = Mathf.Max(0.05f, AttackRadius * 0.75f);

        if (targetVisible && player != null)
            RequestDestination(player.position);
        else if (hasKnownPosition)
            RequestDestination(lastKnownPosition);
    }

    private void UpdateInvestigation()
    {
        if (targetVisible)
        {
            ChangeState(Enemystate.Following);
            AlertAllies();
            return;
        }

        investigateElapsed += Time.deltaTime;
        if (!hasKnownPosition || investigateElapsed >= MaxinvestigatingTime || ReachedDestination(0.75f) || PathIsUnreachable())
        {
            ChangeState(Enemystate.Searching);
            return;
        }

        if (AgentReady() && (!NavMeshAgent.hasPath && !NavMeshAgent.pathPending))
            RequestDestination(lastKnownPosition, true);
    }

    private void UpdateSearch()
    {
        if (targetVisible)
        {
            ChangeState(Enemystate.Following);
            AlertAllies();
            return;
        }

        searchElapsed += Time.deltaTime;
        if (searchElapsed >= searchDuration)
        {
            ChangeState(Enemystate.Returning);
            return;
        }

        if (searchingForPoint)
        {
            if (ReachedDestination(0.6f) || PathIsUnreachable())
            {
                searchingForPoint = false;
                searchPauseRemaining = investigateTime;
                StopAgent();
                Vector2 randomDirection = UnityEngine.Random.insideUnitCircle.normalized;
                searchLookDirection = new Vector3(randomDirection.x, 0f, randomDirection.y);
            }
            return;
        }

        if (searchPauseRemaining > 0f)
        {
            searchPauseRemaining -= Time.deltaTime;
            FacePosition(transform.position + searchLookDirection);
            return;
        }

        if (searchPointsVisited >= searchPoints)
        {
            ChangeState(Enemystate.Returning);
            return;
        }

        if (TryChooseSearchPoint(out Vector3 point))
        {
            searchPointsVisited++;
            searchingForPoint = RequestDestination(point, true);
        }
        else
        {
            searchPointsVisited++;
            searchPauseRemaining = investigateTime;
        }
    }

    private void UpdateReturnToPatrol()
    {
        returnElapsed += Time.deltaTime;
        if (targetVisible)
        {
            ChangeState(Enemystate.Processing);
            return;
        }

        if (patrolpoints == null || patrolpoints.Length == 0 || !AgentReady())
        {
            targetAlerted = false;
            ChangeState(Enemystate.Patrolling);
            return;
        }

        if (ReachedDestination(stopatdistance) || PathIsUnreachable() || returnElapsed >= MaxinvestigatingTime)
        {
            currentpatrol_index = (returnPatrolIndex + 1) % patrolpoints.Length;
            is_Waiting = true;
            patrolWaitRemaining = waittime;
            targetAlerted = false;
            ChangeState(Enemystate.Patrolling);
        }
    }

    protected virtual void UpdateAttack()
    {
        StopAgent();

        if (targetVisible && player != null)
            FacePosition(player.position);
        else if (hasKnownPosition)
            FacePosition(lastKnownPosition);

        if (attackInProgress)
        {
            attackElapsed += Time.deltaTime;

            if (!useAnimationEvents && !attackHitApplied && attackElapsed >= fallbackHitTime)
                AttackPlayer();

            if (attackElapsed >= maximumAttackDuration)
                OnAttackAnimationFinished();

            return;
        }

        if (player == null || !targetVisible || Vector3.Distance(transform.position, player.position) > AttackRadius + attackExitBuffer)
        {
            ChangeState(Enemystate.Following);
            return;
        }

        if (Time.time >= nextAttackTime)
            BeginAttack();
    }

    private void BeginAttack()
    {
        if (attackInProgress)
            return;

        attackInProgress = true;
        attackHitApplied = false;
        IsAttacked = false;
        attackElapsed = 0f;
        int variationCount = Mathf.Clamp(attackVariationCount, 1, 3);
        int attackIndex = UnityEngine.Random.Range(0, variationCount);

        if (variationCount > 1 && lastAttackIndex >= 0 && lastAttackIndex < variationCount)
        {
            attackIndex = UnityEngine.Random.Range(0, variationCount - 1);
            if (attackIndex >= lastAttackIndex)
                attackIndex++;
        }

        lastAttackIndex = attackIndex;
        SetAnimatorInt(AttackIndexHash, attackIndex);
        SetAnimatorTrigger(IsAttackingHash);
    }

    public virtual void AttackPlayer()
    {
        if (CurrentState != Enemystate.Attack || !attackInProgress || attackHitApplied)
            return;

        attackHitApplied = true;
        Transform hitOrigin = Handenemy != null ? Handenemy : TriggerPoint != null ? TriggerPoint : transform;
        Collider[] hits = Physics.OverlapSphere(hitOrigin.position, attackHitRadius, Player_Layer, QueryTriggerInteraction.Collide);

        foreach (Collider hit in hits)
        {
            if (player == null || (hit.transform != player && !hit.transform.IsChildOf(player)))
                continue;

            Vector3 targetDirection = hit.bounds.center - transform.position;
            targetDirection.y = 0f;
            if (targetDirection.sqrMagnitude > 0.001f && Vector3.Angle(transform.forward, targetDirection) > attackHitAngle * 0.5f)
                continue;

            IEnemyMeleeDamageable damageable = hit.GetComponentInParent<IEnemyMeleeDamageable>();
            if (damageable != null)
            {
                damageable.ReceiveEnemyMeleeDamage(attackDamage, hit.ClosestPoint(hitOrigin.position));
            }
            else if (!warnedMissingDamageReceiver)
            {
                warnedMissingDamageReceiver = true;
                Debug.LogWarning($"{name}: Player was hit but no IEnemyMeleeDamageable component was found.", this);
            }

            IsAttacked = true;
            if (attackVignette != null)
            {
                if (vignetteCoroutine != null)
                    StopCoroutine(vignetteCoroutine);
                vignetteCoroutine = StartCoroutine(VignettePulse());
            }
            break;
        }
    }

    public void OnAttackAnimationFinished()
    {
        if (!attackInProgress)
            return;

        attackInProgress = false;
        nextAttackTime = Time.time + attackCooldown;
    }

    private void UpdateHitReaction()
    {
        hitReactRemaining -= Time.deltaTime;
        StopAgent();
        if (hitReactRemaining <= 0f)
        {
            bool stillTracking = Time.time - lastVisibleTime <= sightLostGrace;
            ChangeState(stillTracking ? Enemystate.Following : hasKnownPosition ? Enemystate.Investigating : Enemystate.Patrolling);
        }
    }

    public void takeDamage(float amount)
    {
        if (amount <= 0f || IsDead())
            return;

        health = Mathf.Max(0f, health - amount);
        if (health <= 0f)
        {
            Die(false);
            return;
        }

        if (targetVisible && player != null)
            RememberPosition(player.position);
        else if (!hasKnownPosition)
            RememberPosition(transform.position);

        targetAlerted = true;

        if (amount >= hitStaggerThreshold && Time.time >= nextAllowedStagger)
        {
            nextAllowedStagger = Time.time + staggerImmunityTime;
            BeginHitReaction(hitStaggerDuration);
        }
        else if (CurrentState != Enemystate.Attack && CurrentState != Enemystate.HitReact)
        {
            ChangeState(targetVisible ? Enemystate.Following : Enemystate.Investigating);
        }
    }

    private void BeginHitReaction(float duration)
    {
        if (IsDead())
            return;

        hitReactRemaining = Mathf.Max(hitReactRemaining, duration);
        ChangeState(Enemystate.HitReact);
        SetAnimatorTrigger(HitReactHash);
    }

    public void Recieveshock(float shockpower)
    {
        if (IsDead())
            return;

        if (targetVisible && player != null)
            RememberPosition(player.position);
        else if (!hasKnownPosition)
            RememberPosition(transform.position);

        targetAlerted = true;
        BeginHitReaction(Mathf.Clamp(shockpower, 0.2f, 3f));
    }

    public void TakedownEnemy()
    {
        Die(false);
    }

    public void GrenadeDeath()
    {
        Die(true);
    }

    private void Die(bool grenadeDeath)
    {
        if (IsDead())
            return;

        CurrentState = Enemystate.Dead;
        attackInProgress = false;
        StopAllCoroutines();
        vignetteCoroutine = null;
        RestoreVignette();
        StopAgent();

        if (NavMeshAgent != null)
            NavMeshAgent.enabled = false;

        SetAnimatorBool(IsIdleHash, false);
        SetAnimatorBool(IsWalkingHash, false);
        SetAnimatorBool(IsChasingHash, false);
        SetAnimatorBool(IsProcessingHash, false);
        SetAnimatorBool(grenadeDeath ? GrenadeDeathHash : IsDeadHash, true);

        if (AlertImg != null)
            AlertImg.SetActive(false);
    }

    public bool IsDead()
    {
        return CurrentState == Enemystate.Dead;
    }

    public bool IsInvestigating()
    {
        return CurrentState == Enemystate.Processing || CurrentState == Enemystate.Investigating || CurrentState == Enemystate.Searching;
    }

    public virtual void patrolling()
    {
        UpdatePatrol();
    }

    public virtual void GoTonextpatrolpoint()
    {
        if (!AgentReady() || patrolpoints == null || patrolpoints.Length == 0)
            return;

        for (int i = 0; i < patrolpoints.Length; i++)
        {
            int index = currentpatrol_index % patrolpoints.Length;
            currentpatrol_index = (index + 1) % patrolpoints.Length;
            if (patrolpoints[index] != null)
            {
                is_Waiting = false;
                NavMeshAgent.speed = patrolSpeed;
                NavMeshAgent.stoppingDistance = stopatdistance;
                RequestDestination(patrolpoints[index].position, true);
                return;
            }
        }
    }

    public virtual IEnumerator waitatpoints()
    {
        is_Waiting = true;
        StopAgent();
        yield return new WaitForSeconds(waittime);
        if (!IsDead() && CurrentState == Enemystate.Patrolling)
            GoTonextpatrolpoint();
    }

    public virtual bool CanseePlayer()
    {
        if (player == null || IsDead())
            return false;

        Vector3 origin = enemyrad != null ? enemyrad.position : transform.position + Vector3.up * eyeHeight;
        Vector3 target = player.position + Vector3.up * playerTargetHeight;
        Vector3 direction = target - origin;
        float distance = direction.magnitude;
        if (distance <= 0.001f || distance > detectradius)
            return false;

        float allowedFov = targetAlerted ? alertedFovAngle : fovAngle;
        if (targetAlerted && distance <= closeAwarenessRadius)
            allowedFov = 360f;

        if (allowedFov < 359.9f && Vector3.Angle(transform.forward, direction) > allowedFov * 0.5f)
            return false;

        if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit, distance, ObstacleMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform != player && !hit.transform.IsChildOf(player))
            {
                if (debugSight)
                    Debug.DrawLine(origin, hit.point, Color.red);
                return false;
            }
        }

        if (debugSight)
            Debug.DrawLine(origin, target, Color.green);

        return true;
    }

    public void ReceiveAllyAlert(Vector3 reportedPosition)
    {
        if (IsDead() || CurrentState == Enemystate.Attack || CurrentState == Enemystate.Following || CurrentState == Enemystate.HitReact)
            return;

        RememberPosition(reportedPosition);
        targetAlerted = true;
        ChangeState(Enemystate.Investigating);
    }

    private void AlertAllies()
    {
        if (!alertNearbyEnemies || !hasKnownPosition || Time.time - lastAllyAlertTime < allyAlertCooldown)
            return;

        lastAllyAlertTime = Time.time;
        Collider[] nearby = Physics.OverlapSphere(transform.position, allyAlertRadius);
        HashSet<EnemyAI> contacted = new HashSet<EnemyAI>();

        foreach (Collider collider in nearby)
        {
            EnemyAI ally = collider.GetComponentInParent<EnemyAI>();
            if (ally == null || ally == this || !contacted.Add(ally))
                continue;

            ally.ReceiveAllyAlert(lastKnownPosition);
        }
    }

    private void HearNoise(NoiseEvent noise)
    {
        if (!isActiveAndEnabled || IsDead())
            return;

        if ((noise.position - transform.position).sqrMagnitude > noise.radius * noise.radius)
            return;

        if (CurrentState == Enemystate.Attack || CurrentState == Enemystate.HitReact)
            return;

        if (targetVisible)
            return;

        RememberPosition(noise.position);
        targetAlerted = true;

        if (CurrentState == Enemystate.Following || CurrentState == Enemystate.Processing)
            return;

        if (CurrentState == Enemystate.Investigating)
        {
            RequestDestination(lastKnownPosition);
            return;
        }

        ChangeState(Enemystate.Investigating);
    }

    protected virtual void ChangeState(Enemystate nextState)
    {
        if (CurrentState == nextState || IsDead())
            return;

        if (CurrentState == Enemystate.Attack)
            attackInProgress = false;

        Enemystate previousState = CurrentState;
        CurrentState = nextState;
        if (logStateChanges)
            Debug.Log($"{name}: {previousState} -> {nextState}", this);

        switch (nextState)
        {
            case Enemystate.Patrolling:
                if (AgentReady())
                    NavMeshAgent.speed = patrolSpeed;
                break;

            case Enemystate.Processing:
                processingProgress = 0f;
                StopAgent();
                break;

            case Enemystate.Following:
                if (AgentReady())
                {
                    NavMeshAgent.speed = chaseSpeed;
                    NavMeshAgent.stoppingDistance = Mathf.Max(0.05f, AttackRadius * 0.75f);
                    NavMeshAgent.isStopped = false;
                }
                break;

            case Enemystate.Attack:
                StopAgent();
                nextAttackTime = Mathf.Max(nextAttackTime, Time.time);
                break;

            case Enemystate.Investigating:
                investigateElapsed = 0f;
                if (hasKnownPosition && AgentReady())
                {
                    NavMeshAgent.speed = patrolSpeed * 1.3f;
                    NavMeshAgent.stoppingDistance = 0.4f;
                    RequestDestination(lastKnownPosition, true);
                }
                break;

            case Enemystate.Searching:
                searchElapsed = 0f;
                searchPointsVisited = 0;
                searchingForPoint = false;
                searchPauseRemaining = investigateTime;
                searchLookDirection = transform.forward;
                StopAgent();
                break;

            case Enemystate.Returning:
                returnElapsed = 0f;
                if (AgentReady() && patrolpoints != null && patrolpoints.Length > 0)
                {
                    returnPatrolIndex = FindNearestPatrolPoint();
                    if (patrolpoints[returnPatrolIndex] != null)
                    {
                        NavMeshAgent.speed = patrolSpeed;
                        NavMeshAgent.stoppingDistance = stopatdistance;
                        RequestDestination(patrolpoints[returnPatrolIndex].position, true);
                    }
                }
                break;

            case Enemystate.HitReact:
                StopAgent();
                break;
        }

        OnStateChanged(previousState, nextState);
    }

    protected virtual void OnStateChanged(Enemystate previousState, Enemystate nextState)
    {
    }

    private bool TryChooseSearchPoint(out Vector3 result)
    {
        result = transform.position;
        if (!AgentReady() || !hasKnownPosition)
            return false;

        NavMeshPath path = new NavMeshPath();
        for (int attempt = 0; attempt < 10; attempt++)
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle * searchRadius;
            Vector3 candidate = lastKnownPosition + new Vector3(offset.x, 0f, offset.y);
            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMeshAgent.areaMask))
                continue;

            if (NavMeshAgent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
            {
                result = hit.position;
                return true;
            }
        }

        return false;
    }

    private int FindNearestPatrolPoint()
    {
        int result = 0;
        float nearest = float.PositiveInfinity;

        for (int i = 0; i < patrolpoints.Length; i++)
        {
            if (patrolpoints[i] == null)
                continue;

            float sqrDistance = (patrolpoints[i].position - transform.position).sqrMagnitude;
            if (sqrDistance < nearest)
            {
                nearest = sqrDistance;
                result = i;
            }
        }

        return result;
    }

    private bool RequestDestination(Vector3 target, bool force = false)
    {
        if (!AgentReady())
            return false;

        if (!force && hasDestination && NavMeshAgent.hasPath && (target - destination).sqrMagnitude < destinationChangeThreshold * destinationChangeThreshold)
            return true;

        if (!force && hasDestination && Time.time < nextDestinationRefresh)
            return true;

        if (force)
            hasDestination = false;

        nextDestinationRefresh = Time.time + destinationRefreshInterval;
        if (!NavMesh.SamplePosition(target, out NavMeshHit hit, 2f, NavMeshAgent.areaMask))
            return false;

        NavMeshAgent.isStopped = false;
        bool accepted = NavMeshAgent.SetDestination(hit.position);
        if (accepted)
        {
            destination = hit.position;
            hasDestination = true;
        }
        return accepted;
    }

    private bool ReachedDestination(float tolerance)
    {
        if (!AgentReady() || NavMeshAgent.pathPending || !hasDestination)
            return false;

        float allowedDistance = Mathf.Max(tolerance, NavMeshAgent.stoppingDistance);
        if (NavMeshAgent.hasPath && !float.IsInfinity(NavMeshAgent.remainingDistance))
            return NavMeshAgent.remainingDistance <= allowedDistance;

        return Vector3.Distance(transform.position, destination) <= allowedDistance;
    }

    private bool PathIsUnreachable()
    {
        return AgentReady() && !NavMeshAgent.pathPending && NavMeshAgent.hasPath && NavMeshAgent.pathStatus != NavMeshPathStatus.PathComplete;
    }

    protected bool AgentReady()
    {
        return NavMeshAgent != null && NavMeshAgent.isActiveAndEnabled && NavMeshAgent.isOnNavMesh;
    }

    protected void StopAgent()
    {
        if (AgentReady())
            NavMeshAgent.isStopped = true;
    }

    private void RememberPosition(Vector3 position)
    {
        lastKnownPosition = position;
        hasKnownPosition = true;
    }

    protected void FacePosition(Vector3 point)
    {
        Vector3 direction = point - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion facing = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, facing, turnSpeed * Time.deltaTime);
    }

    private void CacheAnimatorParameters()
    {
        animatorParameters.Clear();
        if (animator == null || animator.runtimeAnimatorController == null)
            return;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
            animatorParameters.Add(parameter.nameHash);
    }

    protected void SetAnimatorBool(int hash, bool value)
    {
        if (animator != null && animatorParameters.Contains(hash))
            animator.SetBool(hash, value);
    }

    private void SetAnimatorInt(int hash, int value)
    {
        if (animator != null && animatorParameters.Contains(hash))
            animator.SetInteger(hash, value);
    }

    protected void SetAnimatorTrigger(int hash)
    {
        if (animator != null && animatorParameters.Contains(hash))
            animator.SetTrigger(hash);
    }

    protected void SetAnimatorFloat(int hash, float value, float dampTime)
    {
        if (animator != null && animatorParameters.Contains(hash))
            animator.SetFloat(hash, value, dampTime, Time.deltaTime);
    }

    protected virtual void UpdateAnimatorState()
    {
        Enemystate current = CurrentState;
        bool walking = (current == Enemystate.Patrolling && !is_Waiting) || current == Enemystate.Investigating || current == Enemystate.Searching || current == Enemystate.Returning;
        bool chasing = current == Enemystate.Following;
        bool processing = current == Enemystate.Processing;
        bool idle = (current == Enemystate.Patrolling && is_Waiting) || current == Enemystate.Attack || current == Enemystate.HitReact || (current == Enemystate.Searching && !searchingForPoint);

        SetAnimatorBool(IsIdleHash, idle);
        SetAnimatorBool(IsWalkingHash, walking && !(current == Enemystate.Searching && !searchingForPoint));
        SetAnimatorBool(IsChasingHash, chasing);
        SetAnimatorBool(IsProcessingHash, processing);
    }

    public virtual void IdleAnimation()
    {
        SetAnimatorBool(IsIdleHash, true);
    }

    private IEnumerator VignettePulse()
    {
        float duration = Mathf.Max(0.05f, hitVignetteDuration);
        float elapsed = 0f;

        while (elapsed < duration && attackVignette != null)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            attackVignette.color.value = Color.Lerp(originalVignetteColor, Color.red, Mathf.Sin(progress * Mathf.PI));
            attackVignette.intensity.value = Mathf.Lerp(originalVignetteIntensity, MaxIntensity, Mathf.Sin(progress * Mathf.PI));
            yield return null;
        }

        RestoreVignette();
        vignetteCoroutine = null;
    }

    private void RestoreVignette()
    {
        if (attackVignette == null)
            return;

        attackVignette.intensity.value = originalVignetteIntensity;
        attackVignette.color.value = originalVignetteColor;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = enemyrad != null ? enemyrad.position : transform.position + Vector3.up * eyeHeight;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, detectradius);

        Vector3 left = Quaternion.Euler(0f, -fovAngle * 0.5f, 0f) * transform.forward;
        Vector3 right = Quaternion.Euler(0f, fovAngle * 0.5f, 0f) * transform.forward;
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(origin, left * detectradius);
        Gizmos.DrawRay(origin, right * detectradius);

        if (hasKnownPosition)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(lastKnownPosition, 0.4f);
        }

        if (Handenemy != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(Handenemy.position, attackHitRadius);
        }
    }
}

public interface IEnemyMeleeDamageable
{
    void ReceiveEnemyMeleeDamage(float damage, Vector3 hitPoint);
}
