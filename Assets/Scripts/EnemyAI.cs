
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
	public Transform[] waypoints;
	private int waypointIndex = 0;
	private NavMeshAgent agent;

	[Header("Combat")]
	public GameObject snowballPrefab;
	public Transform firePoint;
	public float detectionRange = 12f;
	public float attackRange = 10f;
	public float attackCooldown = 1.8f;
	public float snowballForce = 16f;
	public bool requireLineOfSight = false;
	public bool useSpeed = true;
	public float snowballSpeed = 16f;
	public float desiredCombatRangeMin = 8f;
	public float desiredCombatRangeMax = 12f;
	public float combatRepathInterval = 0.25f;
	
	[Header("AI Behavior")]
	public float maxChaseDistance = 20f;
	public float lostTargetSearchTime = 5f;
	public float lastKnownPositionSearchRadius = 5f;
	public float aggressiveness = 0.5f;

	[Header("Jump Reaction")]
	public float incomingSnowballDetectRadius = 3.0f;
	public float jumpHeight = 1.0f;
	public float jumpDuration = 0.6f;
	public string jumpStateName = "Jump";
	public string runStateName = "Run";

	private Transform player;
	private float lastAttackTime = -999f;
	private float nextRepathTime = 0f;
	private Animator animator;
	private bool isJumping = false;
	private float jumpStartTime = -1f;
	private float baseOffsetStart = 0f;
	private int baseLayerIndex = 0;
	private int previousStateHash = 0;
	
	// AI State variables
	private Vector3 lastKnownPlayerPosition;
	private float timePlayerLastSeen = -999f;
	private bool hasEverSeenPlayer = false;
	private AIState currentState = AIState.Patrolling;
	
	private enum AIState
	{
		Patrolling,
		Chasing,
		Combat,
		Searching
	}

	void Start()
	{
		agent = GetComponent<NavMeshAgent>();
		player = FindObjectOfType<PlayerMovement>()?.transform;
		if (agent != null)
		{
			agent.updateRotation = false;
			agent.baseOffset = 0f;
			baseOffsetStart = 0f;
		}

		animator = GetComponentInChildren<Animator>();

		if (firePoint == null)
		{
			GameObject fp = new GameObject("FirePoint");
			fp.transform.SetParent(transform);
			fp.transform.localPosition = new Vector3(0f, 1.5f, 0.5f);
			fp.transform.localRotation = Quaternion.identity;
			firePoint = fp.transform;
		}

		if (waypoints != null && waypoints.Length > 0)
		{
			agent.SetDestination(waypoints[waypointIndex].position);
		}
	}

	void Update()
	{
		if (player == null)
		{
			player = FindObjectOfType<PlayerMovement>()?.transform;
		}

		HandleIncomingSnowballReaction();
		UpdateJumpLerp();

		UpdateAIState();
		ExecuteCurrentState();
	}

	void UpdateAIState()
	{
		if (player == null) return;

		float distanceToPlayer = Vector3.Distance(transform.position, player.position);
		bool canSeePlayer = IsPlayerInDetection(player.position) && (!requireLineOfSight || HasLineOfSight());

		if (canSeePlayer)
		{
			hasEverSeenPlayer = true;
			lastKnownPlayerPosition = player.position;
			timePlayerLastSeen = Time.time;

			if (distanceToPlayer <= attackRange)
			{
				currentState = AIState.Combat;
			}
			else if (distanceToPlayer <= maxChaseDistance)
			{
				currentState = AIState.Chasing;
			}
			else
			{
				currentState = AIState.Patrolling;
			}
		}
		else
		{
			if (hasEverSeenPlayer && Time.time - timePlayerLastSeen < lostTargetSearchTime)
			{
				currentState = AIState.Searching;
			}
			else
			{
				currentState = AIState.Patrolling;
			}
		}
	}

	void ExecuteCurrentState()
	{
		switch (currentState)
		{
			case AIState.Patrolling:
				HandlePatrol();
				break;
			case AIState.Chasing:
				HandleChasing();
				break;
			case AIState.Combat:
				HandleCombat();
				break;
			case AIState.Searching:
				HandleSearching();
				break;
		}
	}

	void HandlePatrol()
	{
		if (agent != null)
		{
			agent.isStopped = false;
		}
		if (waypoints != null && waypoints.Length > 0 && !agent.pathPending && agent.remainingDistance < 0.5f)
		{
			GoToNextWaypoint();
		}
	}

	void HandleChasing()
	{
		if (agent != null && player != null)
		{
			agent.isStopped = false;
			agent.SetDestination(player.position);
			
			Vector3 toPlayer = (player.position - transform.position);
			toPlayer.y = 0f;
			if (toPlayer.sqrMagnitude > 0.0001f)
			{
				transform.rotation = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
			}
		}
	}

	void HandleSearching()
	{
		if (agent != null)
		{
			agent.isStopped = false;
			
			if (!agent.pathPending && agent.remainingDistance < 1f)
			{
				Vector3 searchPos = lastKnownPlayerPosition + Random.insideUnitSphere * lastKnownPositionSearchRadius;
				searchPos.y = transform.position.y;
				agent.SetDestination(searchPos);
			}
		}
	}

	void HandleCombat()
	{
		if (agent != null)
		{
			agent.isStopped = false;
		}

		Vector3 toPlayer = (player.position - transform.position);
		toPlayer.y = 0f;
		if (toPlayer.sqrMagnitude > 0.0001f)
		{
			transform.rotation = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
		}

		float dist = Vector3.Distance(transform.position, player.position);
		UpdateCombatMovement(dist);

		if (dist <= attackRange && Time.time - lastAttackTime >= attackCooldown)
		{
			if (!requireLineOfSight || HasLineOfSight())
			{
				ThrowSnowball();
				lastAttackTime = Time.time;
			}
		}
	}

	void UpdateCombatMovement(float distanceToPlayer)
	{
		if (agent == null || player == null) return;
		if (Time.time < nextRepathTime) return;

		Vector3 myPos = transform.position;
		Vector3 toPlayer = (player.position - myPos);
		Vector3 away = -toPlayer;
		toPlayer.y = 0f;
		away.y = 0f;

		Vector3 targetPos = myPos;
		
		// Adjust combat ranges based on aggressiveness
		float adjustedMinRange = desiredCombatRangeMin * (1f - aggressiveness * 0.5f);
		float adjustedMaxRange = desiredCombatRangeMax * (1f - aggressiveness * 0.3f);

		if (distanceToPlayer < adjustedMinRange)
		{
			// Too close - back away, but less if aggressive
			float backoffDistance = (adjustedMinRange - distanceToPlayer + 2f) * (1f - aggressiveness * 0.3f);
			targetPos = myPos + away.normalized * backoffDistance;
		}
		else if (distanceToPlayer > adjustedMaxRange)
		{
			// Too far - move closer, more aggressively if needed
			float chaseDistance = (distanceToPlayer - adjustedMaxRange + 2f) * (1f + aggressiveness * 0.5f);
			targetPos = myPos + toPlayer.normalized * chaseDistance;
		}
		else
		{
			// In optimal range - strafe or advance based on aggressiveness
			if (aggressiveness > 0.6f)
			{
				// Aggressive: try to get closer while strafing
				Vector3 perp = Vector3.Cross(Vector3.up, toPlayer).normalized;
				float dirSign = Mathf.Sign(Mathf.Sin(Time.time * 0.8f));
				Vector3 strafeMove = perp * dirSign * 2f;
				Vector3 advanceMove = toPlayer.normalized * 1f;
				targetPos = myPos + strafeMove + advanceMove;
			}
			else
			{
				// Defensive: maintain distance while strafing
				Vector3 perp = Vector3.Cross(Vector3.up, toPlayer).normalized;
				float dirSign = Mathf.Sign(Mathf.Sin(Time.time * 0.8f));
				targetPos = myPos + perp * dirSign * 3f;
			}
		}

		agent.SetDestination(targetPos);
		nextRepathTime = Time.time + combatRepathInterval;
	}

	bool IsPlayerInDetection(Vector3 playerPos)
	{
		float dist = Vector3.Distance(transform.position, playerPos);
		return dist <= detectionRange;
	}

	bool HasLineOfSight()
	{
		Vector3 origin = firePoint != null ? firePoint.position : transform.position + Vector3.up * 1.5f;
		Vector3 dir = (player.position + Vector3.up * 1.0f - origin).normalized;
		if (Physics.Raycast(origin, dir, out RaycastHit hit, detectionRange))
		{
			return hit.transform == player || hit.transform.GetComponentInParent<PlayerMovement>() != null;
		}
		return false;
	}

	void ThrowSnowball()
	{
		if (snowballPrefab == null || firePoint == null) return;

		Vector3 target = player.position + Vector3.up * 1.0f;
		Vector3 dir = (target - firePoint.position).normalized;

		GameObject snowball = Instantiate(snowballPrefab, firePoint.position, Quaternion.LookRotation(dir));

		if (snowball.GetComponent<SnowballProjectile>() == null)
		{
			snowball.AddComponent<SnowballProjectile>();
		}
		if (snowball.GetComponent<SnowballController>() == null)
		{
			snowball.AddComponent<SnowballController>();
		}

		Rigidbody rb = snowball.GetComponent<Rigidbody>();
		if (rb == null) rb = snowball.AddComponent<Rigidbody>();
		rb.useGravity = true;
		rb.mass = 0.2f;
		rb.linearDamping = 0.05f;
		rb.angularDamping = 0.05f;
		rb.linearVelocity = Vector3.zero;
		rb.angularVelocity = Vector3.zero;
		if (useSpeed)
		{
			rb.linearVelocity = dir * snowballSpeed;
		}
		else
		{
			rb.AddForce(dir * snowballForce, ForceMode.Impulse);
		}

		IgnoreCollisionWithSelf(snowball);
	}

	void IgnoreCollisionWithSelf(GameObject proj)
	{
		Collider projCol = proj.GetComponent<Collider>();
		if (projCol == null)
		{
			projCol = proj.AddComponent<SphereCollider>();
			((SphereCollider)projCol).radius = 0.1f;
		}
		projCol.isTrigger = false;
		foreach (var c in GetComponentsInChildren<Collider>())
		{
			Physics.IgnoreCollision(projCol, c, true);
		}
	}

	void GoToNextWaypoint()
	{
		waypointIndex = (waypointIndex + 1) % waypoints.Length;
		agent.SetDestination(waypoints[waypointIndex].position);
	}

	void HandleIncomingSnowballReaction()
	{
		if (isJumping) return;
		var hits = Physics.OverlapSphere(transform.position + Vector3.up * 0.5f, incomingSnowballDetectRadius);
		for (int i = 0; i < hits.Length; i++)
		{
			if (hits[i].attachedRigidbody == null) continue;
			if (hits[i].attachedRigidbody.gameObject.GetComponent<SnowballProjectile>() == null) continue;
			Vector3 toEnemy = (transform.position + Vector3.up) - hits[i].attachedRigidbody.worldCenterOfMass;
			Vector3 vel = hits[i].attachedRigidbody.linearVelocity;
			if (Vector3.Dot(vel, toEnemy) <= 0f) continue;
			StartJump();
			break;
		}
	}

	void StartJump()
	{
		if (agent == null) return;
		isJumping = true;
		jumpStartTime = Time.time;
		if (animator != null)
		{
			previousStateHash = animator.GetCurrentAnimatorStateInfo(baseLayerIndex).shortNameHash;
			int jumpHash = GetAvailableStateHash(jumpStateName, "Jump", "jump");
			if (jumpHash != 0)
			{
				animator.CrossFadeInFixedTime(jumpHash, 0.05f);
			}
		}
	}

	void UpdateJumpLerp()
	{
		if (!isJumping || agent == null) return;
		float t = (Time.time - jumpStartTime) / jumpDuration;
		if (t >= 1f)
		{
			isJumping = false;
			agent.baseOffset = 0f;
			if (animator != null)
			{
				int runHash = GetAvailableStateHash(runStateName, "Run", "run");
				if (runHash != 0)
				{
					animator.CrossFadeInFixedTime(runHash, 0.1f);
				}
				else if (previousStateHash != 0)
				{
					animator.CrossFadeInFixedTime(previousStateHash, 0.1f);
				}
			}
			return;
		}
		float height = Mathf.Sin(t * Mathf.PI) * jumpHeight;
		agent.baseOffset = height;
	}

	int GetAvailableStateHash(string preferred, string alt1, string alt2)
	{
		if (animator == null) return 0;
		if (!string.IsNullOrEmpty(preferred))
		{
			int hash = Animator.StringToHash(preferred);
			if (animator.HasState(baseLayerIndex, hash)) return hash;
		}
		if (!string.IsNullOrEmpty(alt1))
		{
			int hash = Animator.StringToHash(alt1);
			if (animator.HasState(baseLayerIndex, hash)) return hash;
		}
		if (!string.IsNullOrEmpty(alt2))
		{
			int hash = Animator.StringToHash(alt2);
			if (animator.HasState(baseLayerIndex, hash)) return hash;
		}
		return 0;
	}

	void OnDrawGizmosSelected()
	{
		// Draw detection range
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(transform.position, detectionRange);
		
		// Draw attack range
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(transform.position, attackRange);
		
		// Draw chase range
		Gizmos.color = Color.orange;
		Gizmos.DrawWireSphere(transform.position, maxChaseDistance);
		
		// Draw last known player position
		if (hasEverSeenPlayer)
		{
			Gizmos.color = Color.blue;
			Gizmos.DrawSphere(lastKnownPlayerPosition, 0.5f);
			Gizmos.DrawWireSphere(lastKnownPlayerPosition, lastKnownPositionSearchRadius);
		}
		
		// Draw current state
		if (Application.isPlaying)
		{
			UnityEngine.GUIStyle style = new UnityEngine.GUIStyle();
			style.normal.textColor = Color.white;
			UnityEngine.GUI.Label(new UnityEngine.Rect(10, 10, 200, 20), 
				$"AI State: {currentState}", style);
		}
	}
}
