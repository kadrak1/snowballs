
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
	public Transform[] waypoints;
	private int waypointIndex = 0;
	private NavMeshAgent agent;

	// Combat
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

	private Transform player;
	private float lastAttackTime = -999f;
	private float nextRepathTime = 0f;

	void Start()
	{
		agent = GetComponent<NavMeshAgent>();
		player = FindObjectOfType<PlayerMovement>()?.transform;
		if (agent != null)
		{
			agent.updateRotation = false;
		}

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

		bool inCombat = player != null && IsPlayerInDetection(player.position);
		if (inCombat)
		{
			HandleCombat();
		}
		else
		{
			HandlePatrol();
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

	void HandleCombat()
	{
		// В бою двигаемся, не стопаем агента; лишь вручную поворачиваемся к игроку
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
		if (distanceToPlayer < desiredCombatRangeMin)
		{
			targetPos = myPos + away.normalized * (desiredCombatRangeMin - distanceToPlayer + 2f);
		}
		else if (distanceToPlayer > desiredCombatRangeMax)
		{
			targetPos = myPos + toPlayer.normalized * (distanceToPlayer - desiredCombatRangeMax + 2f);
		}
		else
		{
			Vector3 perp = Vector3.Cross(Vector3.up, toPlayer).normalized;
			float dirSign = Mathf.Sign(Mathf.Sin(Time.time * 0.8f));
			targetPos = myPos + perp * dirSign * 3f;
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
		// Обычный коллайдер: Rigidbody снежка получит OnCollisionEnter при столкновении с CharacterController
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
}
