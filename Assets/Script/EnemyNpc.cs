using UnityEngine;

public class EnemyNPC : MonoBehaviour
{
    [Header("Patrol")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float patrolDistance = 3f;
    [SerializeField] private float waitTime = 1.5f;

    [Header("Detection")]
    [SerializeField] private float detectionRange = 2.5f;
    [SerializeField] private LayerMask playerLayer;

    private Vector2 startPosition;
    private Rigidbody2D rb;
    private bool movingRight = true;
    private bool isWaiting = false;
    private float waitTimer = 0f;
    private bool battleTriggered = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();

        rb.gravityScale = 0;
        rb.freezeRotation = true;
        startPosition = transform.position;
    }

    void Update()
    {
        if (battleTriggered) return;

        DetectPlayer();

        if (!isWaiting)
            Patrol();
        else
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
                isWaiting = false;
        }
    }

    private void Patrol()
    {
        Vector2 target = movingRight
            ? startPosition + Vector2.right * patrolDistance
            : startPosition - Vector2.right * patrolDistance;

        transform.position = Vector2.MoveTowards(
            transform.position,
            target,
            patrolSpeed * Time.deltaTime
        );

        transform.localScale = new Vector3(movingRight ? 1 : -1, 1, 1);

        if (Vector2.Distance(transform.position, target) < 0.1f)
        {
            movingRight = !movingRight;
            isWaiting = true;
            waitTimer = waitTime;
        }
    }

    private void DetectPlayer()
    {
        Collider2D hit = Physics2D.OverlapCircle(
            transform.position,
            detectionRange,
            playerLayer
        );

        if (hit != null)
        {
            Debug.Log("Player detected — starting battle!");
            TriggerBattle(hit.gameObject);
        }
        else
        {
            Debug.Log("Searching for player...");
        }
    }

    private void TriggerBattle(GameObject player)
    {
        if (battleTriggered) return;
        battleTriggered = true;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        BattleManager battle = FindFirstObjectByType<BattleManager>();
        if (battle != null)
            battle.StartBattle(player, gameObject);
        else
            Debug.LogError("No BattleManager found in scene!", this);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.yellow;
        Vector2 pos = Application.isPlaying ? startPosition : (Vector2)transform.position;
        Gizmos.DrawLine(
            pos + Vector2.left * patrolDistance,
            pos + Vector2.right * patrolDistance
        );
    }
}