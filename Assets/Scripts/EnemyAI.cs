
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public Transform[] waypoints;
    private int waypointIndex = 0;
    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (waypoints != null && waypoints.Length > 0)
        {
            agent.SetDestination(waypoints[waypointIndex].position);
        }
    }

    void Update()
    {
        if (waypoints != null && waypoints.Length > 0 && !agent.pathPending && agent.remainingDistance < 0.5f)
        {
            GoToNextWaypoint();
        }
    }

    void GoToNextWaypoint()
    {
        waypointIndex = (waypointIndex + 1) % waypoints.Length;
        agent.SetDestination(waypoints[waypointIndex].position);
    }
}
