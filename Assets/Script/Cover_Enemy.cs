using UnityEngine;
using UnityEngine.AI;

public class Cover_Enemy : EnemyAI
{
    private CoverPoint currentTargetCover;
    private bool reachedCover = false;

    public override void Update()
    {
       // base.Update();

        if (currentTargetCover == null)
        {
            currentTargetCover = SearchForClosestCover();

            if (currentTargetCover != null)
            {
                currentTargetCover.isOccupied = true;
                currentTargetCover.currentOccupant = transform;

                Vector3 playerToCoverDir = (currentTargetCover.transform.position - player.position).normalized;

                Vector3 safeCoverSpot = currentTargetCover.transform.position + (playerToCoverDir * 1.5f);

                NavMeshAgent.isStopped = false;
                NavMeshAgent.SetDestination(safeCoverSpot);
                reachedCover = false;

                Debug.Log($"Moving to dynamic cover spot behind: {currentTargetCover.name}");
            }
        }
        else if (!reachedCover)
        {
            if (!NavMeshAgent.pathPending && NavMeshAgent.remainingDistance <= NavMeshAgent.stoppingDistance)
            {
                reachedCover = true;
                NavMeshAgent.isStopped = true;

                 Vector3 lookDirection = new Vector3(player.position.x, transform.position.y, player.position.z);
                transform.LookAt(lookDirection);
            }
        }
    }

    private CoverPoint SearchForClosestCover()
    {
        CoverPoint[] allCovers = FindObjectsByType<CoverPoint>(FindObjectsSortMode.None);
        CoverPoint bestCover = null;
        float closestDistance = Mathf.Infinity;

        foreach (CoverPoint cover in allCovers)
        {
            float distance = Vector3.Distance(transform.position, cover.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                bestCover = cover;
            }
        }
        return bestCover;
    }
}