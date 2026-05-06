using UnityEngine;

public class MenuChickenWander : MonoBehaviour
{
    public float moveSpeed = 1.5f;
    public float turnSpeed = 4f;
    public float wanderRadius = 6f;
    public float minIdleTime = 1f;
    public float maxIdleTime = 4f;

    private Vector3 startPos;
    private Vector3 targetPos;
    private float idleTimer;
    private bool idle = true;

    private void Start()
    {
        startPos = transform.position;
        StartIdle();
    }

    private void Update()
    {
        if (idle)
        {
            idleTimer -= Time.deltaTime;
            if (idleTimer <= 0f)
                PickTarget();

            return;
        }

        Vector3 direction = targetPos - transform.position;
        direction.y = 0f;

        if (direction.magnitude < 0.2f)
        {
            StartIdle();
            return;
        }

        transform.position += direction.normalized * moveSpeed * Time.deltaTime;
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(direction.normalized),
            turnSpeed * Time.deltaTime
        );
    }

    private void StartIdle()
    {
        idle = true;
        idleTimer = Random.Range(minIdleTime, maxIdleTime);
    }

    private void PickTarget()
    {
        Vector2 random = Random.insideUnitCircle * wanderRadius;
        targetPos = startPos + new Vector3(random.x, 0f, random.y);
        idle = false;
    }
}