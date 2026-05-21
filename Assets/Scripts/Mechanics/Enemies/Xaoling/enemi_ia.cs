using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 3f;
    public float jumpForce = 7f;

    [Header("Suelo")]
    public Transform groundCheck;
    public LayerMask groundLayer;
    public float groundCheckRadius = 0.2f;
    public float frontCheckDistance = 0.7f;

    [Header("Patrulla")]
    public Transform leftBoundary;
    public Transform rightBoundary;
    public float waitTime = 2f;

    [Header("Jugador")]
    public float detectionRange = 5f;
    public float playerViewRange = 12f;
    public LayerMask playerLayer;

    [Header("NPC")]
    public LayerMask npcLayer;
    public float npcDetectionRange = 4f;

    [Header("Combate")]
    public float attackRange = 1.5f;
    public int attackDamage = 10;
    public float attackCooldown = 1.5f;

    [Header("Vida")]
    public int maxHealth = 100;

    private int currentHealth;

    private Rigidbody2D rb;
    private Animator animator;

    private bool isFacingRight = true;
    private bool isGrounded;

    private bool canAttack = true;
    private bool isAttacking = false;

    private bool isWaiting = false;

    private Transform player;
    private Transform npcTarget;
    private Transform currentTarget;

    private float jumpCooldown;

    private State currentState;

    public enum State
    {
        Idle,
        Patrolling,
        Chasing,
        Attacking
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        currentHealth = maxHealth;

        player =
            GameObject.FindGameObjectWithTag("Player")
            ?.transform;

        currentState = State.Patrolling;
    }

    void Update()
    {
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );

        if (jumpCooldown > 0)
            jumpCooldown -= Time.deltaTime;

        StateMachine();
    }

    void StateMachine()
    {
        BuscarNPC();

        currentTarget = null;

        float playerDistance =
            player != null
            ? Vector2.Distance(
                transform.position,
                player.position
            )
            : Mathf.Infinity;

        // PRIORIDAD 1:
        // jugador MUY cerca
        if (player != null &&
            playerDistance <= detectionRange)
        {
            currentTarget = player;
        }

        // PRIORIDAD 2:
        // jugador observando pelea
        else if (
            npcTarget != null &&
            player != null &&
            playerDistance <= playerViewRange
        )
        {
            currentTarget = npcTarget;
        }

        float distance =
            currentTarget != null
            ? Vector2.Distance(
                transform.position,
                currentTarget.position
            )
            : Mathf.Infinity;

        switch (currentState)
        {
            case State.Idle:

                if (currentTarget != null)
                    currentState = State.Chasing;
                else
                    Idle();

                break;

            case State.Patrolling:

                if (currentTarget != null)
                    currentState = State.Chasing;
                else
                    Patrol();

                break;

            case State.Chasing:
                Chase(distance);
                break;

            case State.Attacking:
                Attack(distance);
                break;
        }
    }

    void Idle()
    {
        rb.velocity = new Vector2(
            0,
            rb.velocity.y
        );

        animator.SetBool("isWalking", false);
    }

    void Patrol()
    {
        if (isWaiting)
            return;

        animator.SetBool("isWalking", true);

        float dir = isFacingRight ? 1f : -1f;

        if (!HasGroundAhead())
        {
            StartCoroutine(WaitAndTurn());
            return;
        }

        rb.velocity = new Vector2(
            dir * moveSpeed,
            rb.velocity.y
        );

        if (transform.position.x >= rightBoundary.position.x)
        {
            Flip();
        }

        if (transform.position.x <= leftBoundary.position.x)
        {
            Flip();
        }

        TryJump();
    }

    void Chase(float distance)
    {
        if (currentTarget == null)
        {
            currentState = State.Patrolling;
            return;
        }

        float dir =
            currentTarget.position.x >
            transform.position.x
            ? 1f
            : -1f;

        if (!HasGroundAhead())
        {
            rb.velocity = new Vector2(
                0,
                rb.velocity.y
            );

            animator.SetBool("isWalking", false);

            return;
        }

        rb.velocity = new Vector2(
            dir * moveSpeed,
            rb.velocity.y
        );

        animator.SetBool("isWalking", true);

        if ((dir > 0 && !isFacingRight) ||
            (dir < 0 && isFacingRight))
        {
            Flip();
        }

        if (distance <= attackRange)
        {
            currentState = State.Attacking;
        }

        TryJump();
    }

    void Attack(float distance)
    {
        if (currentTarget == null)
        {
            currentState = State.Patrolling;
            return;
        }

        if (distance > attackRange)
        {
            currentState = State.Chasing;
            return;
        }

        rb.velocity = new Vector2(
            0,
            rb.velocity.y
        );

        animator.SetBool("isWalking", false);

        if ((currentTarget.position.x > transform.position.x &&
            !isFacingRight) ||

            (currentTarget.position.x < transform.position.x &&
            isFacingRight))
        {
            Flip();
        }

        if (canAttack && !isAttacking)
        {
            StartCoroutine(PerformAttack());
        }
    }

    IEnumerator PerformAttack()
    {
        canAttack = false;
        isAttacking = true;

        animator.SetTrigger("Attack");

        yield return new WaitForSeconds(0.3f);

        Collider2D[] objetivos =
            Physics2D.OverlapCircleAll(
                transform.position,
                attackRange
            );

        foreach (Collider2D objetivo in objetivos)
        {
            if (objetivo.gameObject == gameObject)
                continue;

            bool esJugador =
                ((1 << objetivo.gameObject.layer)
                & playerLayer) != 0;

            bool esNPC =
                ((1 << objetivo.gameObject.layer)
                & npcLayer) != 0;

            if (!esJugador && !esNPC)
                continue;

            HealthComponent health =
                objetivo.GetComponentInParent<HealthComponent>();

            if (health != null)
            {
                health.TakeDamage(
                    attackDamage,
                    gameObject
                );
            }
        }

        yield return new WaitForSeconds(0.5f);

        isAttacking = false;

        yield return new WaitForSeconds(
            attackCooldown
        );

        canAttack = true;
    }

    void BuscarNPC()
    {
        npcTarget = null;

        Collider2D[] npcs =
            Physics2D.OverlapCircleAll(
                transform.position,
                npcDetectionRange,
                npcLayer
            );

        foreach (Collider2D npc in npcs)
        {
            if (npc.gameObject == gameObject)
                continue;

            HealthComponent hp =
                npc.GetComponentInParent<HealthComponent>();

            if (hp == null)
                continue;

            npcTarget = npc.transform;
            return;
        }
    }

    bool HasGroundAhead()
    {
        float dir = isFacingRight ? 1f : -1f;

        Vector2 origin = new Vector2(
            transform.position.x + dir * 0.6f,
            transform.position.y
        );

        return Physics2D.Raycast(
            origin,
            Vector2.down,
            frontCheckDistance,
            groundLayer
        );
    }

    IEnumerator WaitAndTurn()
    {
        isWaiting = true;

        rb.velocity = new Vector2(
            0,
            rb.velocity.y
        );

        animator.SetBool("isWalking", false);

        yield return new WaitForSeconds(waitTime);

        Flip();

        isWaiting = false;
    }

    void Flip()
    {
        isFacingRight = !isFacingRight;

        Vector3 scale = transform.localScale;
        scale.x *= -1;

        transform.localScale = scale;
    }

    void Jump()
    {
        if (!isGrounded)
            return;

        rb.velocity = new Vector2(
            rb.velocity.x,
            jumpForce
        );

        jumpCooldown = 1f;
    }

    void TryJump()
    {
        if (!isGrounded || jumpCooldown > 0)
            return;

        Vector2 origin =
            transform.position +
            (isFacingRight
            ? Vector3.right
            : Vector3.left) * 0.7f;

        RaycastHit2D wall =
            Physics2D.Raycast(
                origin,
                isFacingRight
                ? Vector2.right
                : Vector2.left,
                0.5f,
                groundLayer
            );

        RaycastHit2D up =
            Physics2D.Raycast(
                origin,
                Vector2.up,
                1.5f,
                groundLayer
            );

        if (wall.collider != null &&
            up.collider == null)
        {
            Jump();
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        animator.SetTrigger("Hit");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        animator.SetTrigger("Die");

        rb.velocity = Vector2.zero;

        GetComponent<Collider2D>().enabled = false;

        enabled = false;

        Destroy(gameObject, 1f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            transform.position,
            detectionRange
        );

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(
            transform.position,
            playerViewRange
        );

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(
            transform.position,
            npcDetectionRange
        );

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(
            transform.position,
            attackRange
        );

        if (groundCheck != null)
        {
            Gizmos.color = Color.green;

            Gizmos.DrawWireSphere(
                groundCheck.position,
                groundCheckRadius
            );
        }
    }
}