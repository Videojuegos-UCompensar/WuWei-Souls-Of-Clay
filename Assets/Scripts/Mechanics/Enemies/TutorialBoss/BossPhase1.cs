using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BossPhase1 : MonoBehaviour
{
    public enum BossState { Idle, Chase, Jump, Attack }

    [Header("Referencias")]
    public Transform player;
    public Animator animator;
    public Transform attackPoint;
    public LayerMask playerLayer;
    public LayerMask groundLayer;

    [Header("Detección")]
    public float detectionRange = 10f;
    public float attackRange = 2f;

    [Header("Movimiento")]
    public float moveSpeed = 3f;
    public float jumpForce = 8f;

    [Header("Ataque")]
    public int damage = 10;
    public float attackCooldown = 1.5f;
    public float attackRadius = 1f;

    private Rigidbody2D rb;
    private BossState currentState;
    private float lastAttackTime;
    private bool isGrounded;
    private bool playerDetected;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
{
    if (player == null) return;

    float distance = Vector2.Distance(transform.position, player.position);
    playerDetected = distance <= detectionRange;

    if (!playerDetected)
    {
        ChangeState(BossState.Idle);
        return;
    }

    if (distance <= attackRange)
    {
        ChangeState(BossState.Attack);
        TryAttack(); // 👈 IMPORTANTE: llamarlo aquí siempre
    }
    else
    {
        ChangeState(BossState.Chase);
    }
}


    private void FixedUpdate()
    {
        isGrounded = Physics2D.Raycast(transform.position, Vector2.down, 1.1f, groundLayer);

        switch (currentState)
        {
            case BossState.Idle:
                rb.velocity = new Vector2(0, rb.velocity.y);
                break;

            case BossState.Chase:
                MoveTowardsPlayer();
                break;

            case BossState.Attack:
                rb.velocity = new Vector2(0, rb.velocity.y);
                break;
        }
    }

    void ChangeState(BossState newState)
{
    if (currentState == newState) return;

    currentState = newState;

    animator.SetBool("IsRunning", newState == BossState.Chase);

    if (newState == BossState.Attack)
    {
        rb.velocity = new Vector2(0, rb.velocity.y);
    }
}


    void MoveTowardsPlayer()
    {
        float direction = Mathf.Sign(player.position.x - transform.position.x);

        rb.velocity = new Vector2(direction * moveSpeed, rb.velocity.y);

        transform.localScale = new Vector3(direction, 1, 1);

        if (player.position.y > transform.position.y + 1f && isGrounded)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            animator.SetTrigger("Jump");
        }
    }

    void TryAttack()
    {
        if (Time.time < lastAttackTime + attackCooldown) return;

        lastAttackTime = Time.time;

        animator.SetTrigger("Attack");
    }

    // 🔥 LLAMAR DESDE ANIMACIÓN (Animation Event)
    public void DoDamage()
    {
        Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, playerLayer);

        foreach (Collider2D hit in hitPlayers)
        {
            HealthComponent hp = hit.GetComponent<HealthComponent>();
            if (hp != null)
            {
                hp.TakeDamage(damage,gameObject);
            }
        }
    }

    public bool IsPlayerDetected()
    {
        return playerDetected;
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        }
    }
}
