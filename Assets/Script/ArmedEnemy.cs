using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class ArmedEnemy : EnemyAI
{
    [Header("Weapon")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private LayerMask bulletHitMask = ~0;
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private GameObject impactEffect;
    [SerializeField] private AudioSource shotAudio;
    [Min(0f)] public float shotDamage = 10f;
    [Min(1f)] public float bulletRange = 45f;
    [Range(0f, 15f)] public float spreadDegrees = 2.2f;
    [Range(0f, 10f)] public float movingSpreadBonus = 1.1f;
    [Min(0.05f)] public float aimTime = 0.5f;
    [Min(0.05f)] public float shotInterval = 0.16f;
    [Range(1, 12)] public int shotsPerBurst = 3;
    [Min(0.05f)] public float BurstCooldown = 0.9f;

    [Header("Combat Distance")]
    [Min(1f)] public float CombatRange = 15f;
    [Min(1f)] public float PreferredShootingDistance = 8f;
    [Min(0.5f)] public float MinimumCombatDistance = 4f;
    [Min(0f)] public float combatExitBuffer = 2f;
    [Min(0.1f)] public float distanceTolerance = 1.7f;
    [Min(0.1f)] public float combatMoveSpeed = 3.6f;

    [Header("Dynamic Repositioning")]
    [Min(0.5f)] public float repositionDistance = 4f;
    [Min(0.3f)] public float decisionIntervalMin = 1.7f;
    [Min(0.3f)] public float decisionIntervalMax = 2.7f;
    [Min(0.1f)] public float failedRepositionRetry = 0.9f;
    [Min(0.1f)] public float minimumStep = 0.45f;
    [Min(0.1f)] public float positionTolerance = 0.55f;
    [Min(0.1f)] public float stuckCheckInterval = 0.9f;
    [Min(0.01f)] public float stuckMinimumProgress = 0.18f;
    [SerializeField] private bool debugCombatMovement;

    private static readonly int IsIdleHash = Animator.StringToHash("Isidle");
    private static readonly int IsWalkingHash = Animator.StringToHash("Iswalking");
    private static readonly int IsChasingHash = Animator.StringToHash("IsChasing");
    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");
    private static readonly int IsFiringHash = Animator.StringToHash("IsFiring");
    private static readonly int FireHash = Animator.StringToHash("Fire");

    private NavMeshPath candidatePath;
    private bool initialAgentRotation;
    private Vector3 combatDestination;
    private Vector3 previousProgressPosition;
    private float nextDecisionTime;
    private float nextShotTime;
    private float nextBurstTime;
    private float nextProgressCheck;
    private float firingIndicatorUntil;
    private int remainingShots;
    private bool movingToCombatPoint;
    private bool warnedMissingPlayerHealth;
    private int preferredStrafeSide = 1;
    private bool wasTooClose;

    protected override void Awake()
    {
        base.Awake();
        candidatePath = new NavMeshPath();
        initialAgentRotation = NavMeshAgent != null && NavMeshAgent.updateRotation;
    }

    public override void Start()
    {
        base.Start();
        if (firePoint == null)
            Debug.LogWarning($"{name}: Assign a Fire Point for accurate weapon muzzle positioning.", this);
    }

    protected override bool ShouldEnterAttack()
    {
        return HasVisualContact && player != null && FlatDistance(transform.position, player.position) <= CombatRange;
    }

    public override void Following()
    {
        if (!AgentReady())
            return;

        NavMeshAgent.speed = chaseSpeed;
        NavMeshAgent.stoppingDistance = Mathf.Max(0.3f, PreferredShootingDistance - distanceTolerance);
        NavMeshAgent.updateRotation = initialAgentRotation;
        NavMeshAgent.isStopped = false;

        Vector3 destination = HasVisualContact && player != null ? player.position : LastKnownPlayerPosition;
        if (!HasVisualContact && !HasLastKnownPlayerPosition)
            return;

        if (!NavMeshAgent.pathPending && NavMesh.SamplePosition(destination, out NavMeshHit sample, 1.5f, NavMeshAgent.areaMask))
        {
            if (!NavMeshAgent.hasPath || (NavMeshAgent.destination - sample.position).sqrMagnitude > 0.64f)
                NavMeshAgent.SetDestination(sample.position);
        }
    }

    protected override void UpdateAttack()
    {
        if (player == null || !AgentReady())
            return;

        if (!HasVisualContact)
        {
            remainingShots = 0;
            movingToCombatPoint = false;
            StopAgent();
            if (TimeSinceLastSight >= sightLostGrace)
                ChangeState(Enemystate.Investigating);
            return;
        }

        float distance = FlatDistance(transform.position, player.position);
        if (distance > CombatRange + combatExitBuffer)
        {
            ChangeState(Enemystate.Following);
            return;
        }

        NavMeshAgent.updateRotation = false;
        NavMeshAgent.speed = combatMoveSpeed;
        NavMeshAgent.stoppingDistance = Mathf.Min(positionTolerance, 0.3f);
        FacePosition(player.position);

        UpdateCombatMovement(distance);
        UpdateBurstFire();
    }

    protected override void OnStateChanged(Enemystate previousState, Enemystate nextState)
    {
        if (previousState == Enemystate.Attack && AgentReady())
            NavMeshAgent.updateRotation = initialAgentRotation;

        if (nextState == Enemystate.Attack)
        {
            remainingShots = 0;
            movingToCombatPoint = false;
            nextBurstTime = Time.time + aimTime;
            nextShotTime = nextBurstTime;
            nextDecisionTime = Time.time + 0.15f;
            nextProgressCheck = Time.time + stuckCheckInterval;
            previousProgressPosition = transform.position;
            preferredStrafeSide = Random.value < 0.5f ? -1 : 1;
            wasTooClose = false;

            if (AgentReady())
            {
                NavMeshAgent.updateRotation = false;
                NavMeshAgent.speed = combatMoveSpeed;
                NavMeshAgent.stoppingDistance = 0.2f;
                NavMeshAgent.ResetPath();
                NavMeshAgent.isStopped = false;
            }
        }
        else
        {
            remainingShots = 0;
            movingToCombatPoint = false;
        }
    }

    private void UpdateCombatMovement(float distance)
    {
        bool tooClose = distance < MinimumCombatDistance;
        if (tooClose && !wasTooClose)
        {
            movingToCombatPoint = false;
            nextDecisionTime = Time.time;
        }
        wasTooClose = tooClose;

        bool needNewPoint = !movingToCombatPoint || Time.time >= nextDecisionTime;

        if (movingToCombatPoint)
        {
            bool pathFailed = !NavMeshAgent.pathPending && NavMeshAgent.hasPath && NavMeshAgent.pathStatus != NavMeshPathStatus.PathComplete;
            bool arrived = !NavMeshAgent.pathPending && Vector3.Distance(transform.position, combatDestination) <= positionTolerance;

            if (Time.time >= nextProgressCheck)
            {
                bool stuck = Vector3.Distance(transform.position, previousProgressPosition) < stuckMinimumProgress;
                nextProgressCheck = Time.time + stuckCheckInterval;
                previousProgressPosition = transform.position;

                if (stuck && !arrived && !NavMeshAgent.pathPending)
                    pathFailed = true;
            }

            if (arrived || pathFailed)
            {
                movingToCombatPoint = false;
                needNewPoint = true;
            }
        }

        if (tooClose && !movingToCombatPoint)
            needNewPoint = true;

        if (!needNewPoint)
            return;

        if (Time.time < nextDecisionTime)
            return;

        if (TryChooseCombatPosition(tooClose, out Vector3 candidate))
        {
            combatDestination = candidate;
            NavMeshAgent.isStopped = false;
            movingToCombatPoint = NavMeshAgent.SetDestination(candidate);
            nextDecisionTime = Time.time + Random.Range(decisionIntervalMin, Mathf.Max(decisionIntervalMin, decisionIntervalMax));
            nextProgressCheck = Time.time + stuckCheckInterval;
            previousProgressPosition = transform.position;
        }
        else
        {
            movingToCombatPoint = false;
            NavMeshAgent.ResetPath();
            StopAgent();
            nextDecisionTime = Time.time + failedRepositionRetry;
        }
    }

    private bool TryChooseCombatPosition(bool retreat, out Vector3 bestPosition)
    {
        bestPosition = transform.position;
        if (!AgentReady() || player == null)
            return false;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.001f)
            toPlayer = transform.forward;
        toPlayer.Normalize();

        Vector3 away = -toPlayer;
        Vector3 right = Vector3.Cross(Vector3.up, toPlayer).normalized;
        Vector3 side = right * preferredStrafeSide;

        Vector3[] directions = retreat
            ? new[] { away, (away + side * 0.7f).normalized, (away - side * 0.7f).normalized, side, -side }
            : new[] { side, -side, (side + away * 0.35f).normalized, (-side + away * 0.35f).normalized, away };

        float[] lengths = { 1f, 0.6f, 0.3f };
        float bestScore = float.NegativeInfinity;
        int selectedSide = preferredStrafeSide;

        foreach (float factor in lengths)
        {
            foreach (Vector3 direction in directions)
            {
                float step = repositionDistance * factor;
                Vector3 requested = transform.position + direction * step;
                float sampleRadius = Mathf.Min(0.65f, Mathf.Max(0.3f, step * 0.3f));

                if (!NavMesh.SamplePosition(requested, out NavMeshHit sampled, sampleRadius, NavMeshAgent.areaMask))
                    continue;

                Vector3 proposed = sampled.position;
                float travel = FlatDistance(transform.position, proposed);
                if (travel < minimumStep || travel < repositionDistance * factor * 0.45f)
                    continue;

                if (!NavMeshAgent.CalculatePath(proposed, candidatePath) || candidatePath.status != NavMeshPathStatus.PathComplete)
                    continue;

                float pathLength = CalculatePathLength(candidatePath);
                if (pathLength > travel * 2.2f + 1.25f)
                    continue;

                float playerDistance = FlatDistance(proposed, player.position);
                if (!retreat && playerDistance < MinimumCombatDistance * 0.8f)
                    continue;

                float distanceImprovement = retreat
                    ? playerDistance - FlatDistance(transform.position, player.position)
                    : -Mathf.Abs(playerDistance - PreferredShootingDistance);

                float score = distanceImprovement * 1.65f + travel * 0.16f - pathLength * 0.16f;
                score += Vector3.Dot(direction, side) * 0.35f;
                score += Random.Range(-0.12f, 0.12f);

                if (NavMesh.FindClosestEdge(proposed, out NavMeshHit edge, NavMeshAgent.areaMask))
                    score += Mathf.Min(edge.distance, 1.5f) * 0.3f;

                if (score <= bestScore)
                    continue;

                bestScore = score;
                bestPosition = proposed;
                float sideDot = Vector3.Dot(direction, right);
                selectedSide = Mathf.Abs(sideDot) > 0.2f ? (sideDot > 0f ? 1 : -1) : preferredStrafeSide;
            }

            if (bestScore > float.NegativeInfinity)
                break;
        }

        if (bestScore == float.NegativeInfinity)
            return false;

        preferredStrafeSide = selectedSide;
        if (debugCombatMovement)
            Debug.DrawLine(transform.position + Vector3.up * 0.3f, bestPosition + Vector3.up * 0.3f, Color.cyan, 1.5f);
        return true;
    }

    private float CalculatePathLength(NavMeshPath path)
    {
        float length = 0f;
        Vector3[] corners = path.corners;
        for (int i = 1; i < corners.Length; i++)
            length += Vector3.Distance(corners[i - 1], corners[i]);
        return length;
    }

    private void UpdateBurstFire()
    {
        if (Time.time < nextShotTime || Time.time < nextBurstTime)
            return;

        if (remainingShots <= 0)
            remainingShots = Mathf.Max(1, shotsPerBurst);

        ShootBullet();
        remainingShots--;
        nextShotTime = Time.time + shotInterval;

        if (remainingShots <= 0)
            nextBurstTime = Time.time + BurstCooldown + Random.Range(0f, 0.2f);
    }

    private void ShootBullet()
    {
        Vector3 muzzlePosition = firePoint != null
            ? firePoint.position
            : transform.position + Vector3.up * eyeHeight + transform.forward * 0.35f;
        Vector3 targetPosition = player.position + Vector3.up * playerTargetHeight;
        Vector3 targetDirection = targetPosition - muzzlePosition;
        if (targetDirection.sqrMagnitude < 0.001f)
            return;

        float spread = spreadDegrees;
        if (AgentReady() && NavMeshAgent.velocity.sqrMagnitude > 0.25f)
            spread += movingSpreadBonus;

        Quaternion aim = Quaternion.LookRotation(targetDirection.normalized);
        Vector3 shotDirection = aim * Quaternion.Euler(Random.Range(-spread, spread), Random.Range(-spread, spread), 0f) * Vector3.forward;

        if (muzzleFlash != null)
            muzzleFlash.Play();
        if (shotAudio != null && shotAudio.clip != null)
            shotAudio.PlayOneShot(shotAudio.clip);

        SetAnimatorTrigger(FireHash);
        firingIndicatorUntil = Time.time + 0.12f;

        RaycastHit[] hits = Physics.RaycastAll(muzzlePosition, shotDirection, bulletRange, bulletHitMask, QueryTriggerInteraction.Ignore);
        float closestDistance = float.PositiveInfinity;
        Collider closestCollider = null;
        Vector3 closestPoint = muzzlePosition + shotDirection * bulletRange;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null || hit.collider.transform.IsChildOf(transform))
                continue;

            if (hit.distance >= closestDistance)
                continue;

            closestDistance = hit.distance;
            closestCollider = hit.collider;
            closestPoint = hit.point;
        }

        if (closestCollider != null)
        {
            if (impactEffect != null)
                Instantiate(impactEffect, closestPoint, Quaternion.LookRotation(-shotDirection));

            if (closestCollider.transform == player || closestCollider.transform.IsChildOf(player))
            {
                IEnemyRangedDamageable rangedHealth = closestCollider.GetComponentInParent<IEnemyRangedDamageable>();
                if (rangedHealth != null)
                    rangedHealth.ReceiveEnemyRangedDamage(shotDamage, closestPoint, shotDirection);
                else
                {
                    IEnemyMeleeDamageable existingHealth = closestCollider.GetComponentInParent<IEnemyMeleeDamageable>();
                    if (existingHealth != null)
                        existingHealth.ReceiveEnemyMeleeDamage(shotDamage, closestPoint);
                    else if (!warnedMissingPlayerHealth)
                    {
                        warnedMissingPlayerHealth = true;
                        Debug.LogWarning($"{name}: Player was hit but has no IEnemyRangedDamageable or IEnemyMeleeDamageable receiver.", this);
                    }
                }
            }
        }

        if (debugCombatMovement)
            Debug.DrawRay(muzzlePosition, shotDirection * (float.IsInfinity(closestDistance) ? bulletRange : closestDistance), Color.red, 0.4f);
    }

    protected override void UpdateAnimatorState()
    {
        base.UpdateAnimatorState();

        if (CurrentState == Enemystate.Attack && AgentReady())
        {
            bool moving = NavMeshAgent.velocity.sqrMagnitude > 0.09f;
            SetAnimatorBool(IsIdleHash, !moving);
            SetAnimatorBool(IsWalkingHash, moving);
            SetAnimatorBool(IsChasingHash, false);
        }

        if (AgentReady())
        {
            Vector3 local = transform.InverseTransformDirection(NavMeshAgent.velocity);
            float normalizer = Mathf.Max(0.1f, CurrentState == Enemystate.Attack ? combatMoveSpeed : chaseSpeed);
            SetAnimatorFloat(MoveXHash, Mathf.Clamp(local.x / normalizer, -1f, 1f), 0.12f);
            SetAnimatorFloat(MoveYHash, Mathf.Clamp(local.z / normalizer, -1f, 1f), 0.12f);
        }
        else
        {
            SetAnimatorFloat(MoveXHash, 0f, 0.12f);
            SetAnimatorFloat(MoveYHash, 0f, 0.12f);
        }

        SetAnimatorBool(IsFiringHash, CurrentState == Enemystate.Attack && Time.time < firingIndicatorUntil);
    }

    public override void AttackPlayer()
    {
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void OnDrawGizmosSelected()
    {
        if (player != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(player.position, PreferredShootingDistance);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(player.position, MinimumCombatDistance);
        }

        if (movingToCombatPoint)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(combatDestination, 0.35f);
            Gizmos.DrawLine(transform.position, combatDestination);
        }
    }
}

public interface IEnemyRangedDamageable
{
    void ReceiveEnemyRangedDamage(float damage, Vector3 hitPoint, Vector3 direction);
}
