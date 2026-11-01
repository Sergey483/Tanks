using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyTankAI : MonoBehaviour
{
    private enum State
    {
        Patrol,
        Chase,
        Attack
    }

    [Header("Links")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform turretPivot;
    [SerializeField] private TankShooter shooter;
    [SerializeField] private Transform[] patrolPoints;

    [Header("Distances")]
    [SerializeField, Min(0f)] private float detectionRange = 18f;
    [SerializeField, Min(0f)] private float attackRange = 10f;
    [SerializeField, Min(0f)] private float loseRange = 24f;

    [Header("Patrol")]
    [SerializeField, Min(0f)] private float pointReachDistance = 1f;

    [Header("Aiming")]
    [SerializeField, Min(0f)] private float turretRotationSpeed = 90f;
    [SerializeField, Range(0f, 30f)] private float shootAngle = 5f;
    [SerializeField] private float aimHeight = 0.8f;

    [Header("Optimization")]
    [SerializeField, Min(0.02f)] private float pathUpdateInterval = 0.25f;

    private NavMeshAgent agent;
    private State currentState;
    private int patrolPointIndex;
    private float nextPathUpdateTime;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        ChangeState(State.Patrol);
    }

    private void Update()
    {
        if (player == null)
            return;

        float distanceToPlayer = Vector3.Distance(
            transform.position,
            player.position
        );

        UpdateState(distanceToPlayer);

        switch (currentState)
        {
            case State.Patrol:
                Patrol();
                break;
            case State.Chase:
                ChasePlayer();
                AimAtPlayer();
                break;
            case State.Attack:
                AttackPlayer();
                break;
        }
    }

    private void UpdateState(float distanceToPlayer)
    {
        if (currentState == State.Patrol &&
            distanceToPlayer <= detectionRange)
        {
            ChangeState(State.Chase);
        }
        else if (currentState == State.Chase)
        {
            if (distanceToPlayer <= attackRange)
                ChangeState(State.Attack);
            else if (distanceToPlayer >= loseRange)
                ChangeState(State.Patrol);
        }
        else if (currentState == State.Attack)
        {
            if (distanceToPlayer > loseRange)
                ChangeState(State.Patrol);
            else if (distanceToPlayer > attackRange)
                ChangeState(State.Chase);
        }
    }

    private void ChangeState(State newState)
    {
        currentState = newState;

        if (currentState == State.Patrol)
        {
            agent.isStopped = false;
            SetPatrolDestination();
        }
        else if (currentState == State.Chase)
        {
            agent.isStopped = false;
            nextPathUpdateTime = 0f;
        }
        else
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    private void Patrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
            return;

        if (!agent.pathPending &&
            agent.remainingDistance <= pointReachDistance)
        {
            patrolPointIndex =
                (patrolPointIndex + 1) % patrolPoints.Length;

            SetPatrolDestination();
        }
    }

    private void SetPatrolDestination()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
            return;

        Transform point = patrolPoints[patrolPointIndex];

        if (point != null)
            agent.SetDestination(point.position);
    }

    private void ChasePlayer()
    {
        if (Time.time < nextPathUpdateTime)
            return;

        nextPathUpdateTime = Time.time + pathUpdateInterval;
        agent.SetDestination(player.position);
    }

    private void AttackPlayer()
    {
        agent.isStopped = true;
        AimAtPlayer();

        if (IsAimedAtPlayer() && shooter != null)
            shooter.TryShoot();
    }

    private void AimAtPlayer()
    {
        if (turretPivot == null)
            return;

        Vector3 targetPosition =
            player.position + Vector3.up * aimHeight;

        Vector3 direction =
            targetPosition - turretPivot.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction);

        turretPivot.rotation = Quaternion.RotateTowards(
            turretPivot.rotation,
            targetRotation,
            turretRotationSpeed * Time.deltaTime
        );
    }

    private bool IsAimedAtPlayer()
    {
        if (turretPivot == null)
            return false;

        Vector3 directionToPlayer =
            player.position - turretPivot.position;

        directionToPlayer.y = 0f;

        if (directionToPlayer.sqrMagnitude < 0.001f)
            return false;

        float angle = Vector3.Angle(
            turretPivot.forward,
            directionToPlayer.normalized
        );

        return angle <= shootAngle;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, loseRange);
    }
}
