using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 3f;
    public float jumpForce = 7f;
    public Transform groundCheck;
    public LayerMask groundLayer;
    public float groundCheckRadius = 0.2f;

    [Header("Patrulla")]
    public Transform leftBoundary;
    public Transform rightBoundary;
    public float waitTime = 2f;
    public float frontCheckDistance = 0.5f;

    [Header("Detección y Combate")]
    public Transform playerDetection;
    public float detectionRange = 5f;
    public float attackRange = 1.5f;
    public int attackDamage = 10;
    public float attackCooldown = 1.5f;
    public LayerMask playerLayer;

    [Header("Vida")]
    public int maxHealth = 100;
    public int currentHealth;
    public float lowHealthThreshold = 30f;
    public float fleeSpeed = 4f;
    public float fleeDistance = 7f;

    [Header("Visual Feedback")]
    [SerializeField] private GameObject healthBarPrefab;
    [SerializeField] private Vector3 healthBarOffset = new Vector3(0, 1.2f, 0);
    [SerializeField] private Color lowHealthColor = Color.red;
    [SerializeField] private Color fullHealthColor = Color.green;

    // Variables privadas
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private bool isFacingRight = true;
    private bool isWaiting = false;
    private bool isAttacking = false;
    private bool canAttack = true;
    private Vector2 startingPosition;
    private Transform player;
    private State currentState;
    private float lastAttackTime;
    private Vector2 fleeDirection;
    private bool isGrounded;
    private float jumpCooldown = 0f;
    private GameObject healthBar;
    private Transform healthFill;
    private bool isDead = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        startingPosition = transform.position;
        currentHealth = maxHealth;
        currentState = State.Patrolling;
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (player == null)
        {
            Debug.LogWarning("No se encontró el jugador con tag 'Player'");
        }

        // Crear barra de vida
        if (healthBarPrefab != null)
        {
            healthBar = Instantiate(healthBarPrefab, transform.position + healthBarOffset, Quaternion.identity);
            healthBar.transform.SetParent(transform);
            healthFill = healthBar.transform.Find("Fill");
            UpdateHealthBar();
        }
    }

    void Update()
    {
        if (isDead) return;

        // Verificar si está en el suelo
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // Reducir los contadores de cooldown
        if (jumpCooldown > 0)
        {
            jumpCooldown -= Time.deltaTime;
        }

        StateMachine();

        // Actualizar posición de la barra de vida
        if (healthBar != null)
        {
            healthBar.transform.position = transform.position + healthBarOffset;
        }
    }



    void StateMachine()
    {
        float distance = player != null ? Vector2.Distance(transform.position, player.position) : float.MaxValue;

        switch (currentState)
        {
            case State.Idle:
                HandleIdle(distance);
                break;

            case State.Patrolling:
                Patrol(distance);
                break;

            case State.Chasing:
                Chase(distance);
                break;

            case State.Attacking:
                Attack(distance);
                break;
        }
    }


    void HandleIdle(float distance)
    {
        rb.velocity = new Vector2(0, rb.velocity.y);
        animator.SetBool("isWalking", false);
        animator.SetBool("isGrounded", isGrounded);

        if (distance <= detectionRange)
        {
            currentState = State.Chasing;
        }
    }

    void Patrol(float distance)
    {
        if (isWaiting)
        {
            animator.SetBool("isWalking", false);
            rb.velocity = new Vector2(0, rb.velocity.y);
            return;
        }

        animator.SetBool("isWalking", true);

        if (distance <= detectionRange)
        {
            currentState = State.Chasing;
            return;
        }

        if (!HasGroundAhead())
        {
            // Stop and wait at boundary (flip visually so it looks correct)
            rb.velocity = new Vector2(0, rb.velocity.y);
            animator.SetBool("isWalking", false);
            Flip();
            if (!isWaiting) StartCoroutine(WaitAtBoundary());
            return;
        }

        rb.velocity = new Vector2(isFacingRight ? moveSpeed : -moveSpeed, rb.velocity.y);

        TryJump();
    }



    void Chase(float distance)
    {
        // Determine whether we should attempt to chase player directly (if on same platform)
        bool playerOnSamePlatform = player != null && IsPlayerOnSamePlatform();

        // Choose horizontal direction:
        // - If player is on same platform, chase towards player's X.
        // - Otherwise keep moving in current facing direction so enemy doesn't freeze.
        float dir;
        if (playerOnSamePlatform)
        {
            dir = player.position.x > transform.position.x ? 1f : -1f;
        }
        else
        {
            dir = isFacingRight ? 1f : -1f;
        }

        // If there's no ground ahead, stop and begin a wait (so enemy doesn't get stuck looking at void).
        if (!HasGroundAhead())
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
            animator.SetBool("isWalking", false);
            if (!isWaiting) StartCoroutine(WaitAtBoundary());
            return;
        }

        // If player is in attack range (only meaningful if on same platform), transition to attack.
        if (distance <= attackRange && playerOnSamePlatform)
        {
            currentState = State.Attacking;
            return;
        }

        // If player is far away, return to Idle
        if (distance > detectionRange * 1.5f)
        {
            currentState = State.Idle;
            return;
        }

        // Apply horizontal movement
        rb.velocity = new Vector2(dir * moveSpeed, rb.velocity.y);

        // Flip sprite if needed based on chosen dir
        if ((dir > 0 && !isFacingRight) || (dir < 0 && isFacingRight))
            Flip();

        // Update walking animation based on actual horizontal velocity
        animator.SetBool("isWalking", Mathf.Abs(rb.velocity.x) > 0.01f);

        TryJump();
    }

    void Attack(float distance)
    {
        animator.SetBool("isWalking", false);

        if (distance > attackRange)
        {
            currentState = State.Chasing;
            return;
        }

        rb.velocity = new Vector2(0, rb.velocity.y);

        if ((player.position.x > transform.position.x && !isFacingRight) ||
            (player.position.x < transform.position.x && isFacingRight))
            Flip();

        if (canAttack && !isAttacking)
            StartCoroutine(PerformAttack());
    }

    bool IsPlayerOnSamePlatform(float maxVerticalDiff = 0.6f)
    {
        if (player == null) return false;

        RaycastHit2D hitPlayer = Physics2D.Raycast(player.position, Vector2.down, 1.0f, groundLayer);
        if (hitPlayer.collider == null) return false;

        if (!isGrounded) return false;

        if (Mathf.Abs(player.position.y - transform.position.y) > maxVerticalDiff) return false;

        return true;
    }




    public enum State
    {
        Idle,
        Patrolling,
        Chasing,
        Attacking
    }


    void ReturnToPatrolArea()
    {
        // Determinar hacia qué punto del área de patrulla dirigirse
        Vector2 targetPosition;

        if (transform.position.x < leftBoundary.position.x)
        {
            targetPosition = leftBoundary.position;
        }
        else if (transform.position.x > rightBoundary.position.x)
        {
            targetPosition = rightBoundary.position;
        }
        else
        {
            // Ya está dentro del área de patrulla
            currentState = State.Patrolling;
            return;
        }

        // Determinar dirección hacia el objetivo
        float direction = targetPosition.x > transform.position.x ? 1 : -1;

        // Voltear si es necesario
        if ((direction > 0 && !isFacingRight) || (direction < 0 && isFacingRight))
        {
            Flip();
        }

        // Moverse hacia el objetivo
        rb.velocity = new Vector2(direction * moveSpeed, rb.velocity.y);

        // Saltar para superar obstáculos
        TryJump();

        // Verificar si ya regresó al área de patrulla
        if (!IsOutsidePatrolArea())
        {
            currentState = State.Patrolling;
        }
    }

    IEnumerator WaitAtBoundary()
    {
        isWaiting = true;
        rb.velocity = new Vector2(0, rb.velocity.y);
        animator.SetBool("isWalking", false);

        float elapsed = 0f;
        // monitor player during wait; if player returns to same platform and in range, resume immediately
        while (elapsed < waitTime)
        {
            if (player != null)
            {
                float distance = Vector2.Distance(transform.position, player.position);
                if (distance <= detectionRange && IsPlayerOnSamePlatform())
                {
                    // resume chasing immediately and face player
                    isWaiting = false;
                    currentState = State.Chasing;
                    FacePlayer();
                    yield break;
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        isWaiting = false;

        // After waiting, decide next state:
        float finalDistance = player != null ? Vector2.Distance(transform.position, player.position) : float.MaxValue;
        if (finalDistance <= detectionRange && IsPlayerOnSamePlatform())
        {
            currentState = State.Chasing;
            FacePlayer();
        }
        else
        {
            currentState = State.Patrolling;
        }
    }

    IEnumerator PerformAttack()
    {
        canAttack = false;
        isAttacking = true;

        // Activar animación de ataque
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        // Esperar a que la animación llegue al frame de daño (ajustar según la animación)
        yield return new WaitForSeconds(0.3f);

        // Detectar al jugador en rango de ataque y aplicar daño
        Collider2D playerCollider = Physics2D.OverlapCircle(transform.position, attackRange, playerLayer);
        if (playerCollider != null)
        {
            // Intentar aplicar daño al jugador usando HealthComponent
            HealthComponent playerHealth = playerCollider.GetComponent<HealthComponent>();
            if (playerHealth != null)
            {
                try { Debug.Log($"[Enemy] {name} attacking {playerCollider.gameObject.name} damage={attackDamage}"); } catch { }
                playerHealth.TakeDamage(attackDamage);
            }
        }

        // Esperar a que termine la animación
        yield return new WaitForSeconds(0.5f);

        isAttacking = false;

        // Aplicar cooldown de ataque
        yield return new WaitForSeconds(attackCooldown);

        canAttack = true;
    }

    void FacePlayer()
    {
        if (player == null) return;
        bool shouldFaceRight = player.position.x > transform.position.x;
        if (shouldFaceRight != isFacingRight)
            Flip();
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
        if (isGrounded && jumpCooldown <= 0)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            jumpCooldown = 1.0f;

            if (animator != null)
            {
                animator.SetTrigger("Jump");
            }
        }
    }

    void TryJump()
    {
        // Intentar saltar si hay un obstáculo adelante pero no hay nada arriba
        if (isGrounded && jumpCooldown <= 0)
        {
            Vector2 rayStart = transform.position + (isFacingRight ? Vector3.right : Vector3.left) * 0.7f;
            RaycastHit2D hitForward = Physics2D.Raycast(rayStart, Vector2.right * (isFacingRight ? 1 : -1), 0.5f, groundLayer);
            RaycastHit2D hitUp = Physics2D.Raycast(rayStart, Vector2.up, 1.5f, groundLayer);

            if (hitForward.collider != null && hitUp.collider == null)
            {
                Jump();
            }
        }
    }

    bool IsOutsidePatrolArea()
    {
        return transform.position.x < leftBoundary.position.x || transform.position.x > rightBoundary.position.x;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        // Update health bar
        UpdateHealthBar();

        // Activar animación de daño si existe
        if (animator != null)
        {
            animator.SetTrigger("Hit");
        }

        // Visual feedback
        StartCoroutine(FlashRed());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator FlashRed()
    {
        if (spriteRenderer != null)
        {
            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = originalColor;
        }
    }

    private void UpdateHealthBar()
    {
        if (healthFill != null)
        {
            float healthPercent = (float)currentHealth / maxHealth;
            healthFill.localScale = new Vector3(healthPercent, 1, 1);

            SpriteRenderer fillRenderer = healthFill.GetComponent<SpriteRenderer>();
            if (fillRenderer != null)
            {
                fillRenderer.color = Color.Lerp(lowHealthColor, fullHealthColor, healthPercent);
            }
        }
    }

    void Die()
    {
        isDead = true;

        // Activar animación de muerte si existe
        if (animator != null)
        {
            Debug.Log($"[Enemy] {name} died.");
            animator.SetTrigger("Die");
        }

        // Desactivar colisiones y movimiento
        GetComponent<Collider2D>().enabled = false;
        rb.velocity = Vector2.zero;
        rb.gravityScale = 0;
        this.enabled = false;

        // Hide health bar
        if (healthBar != null)
        {
            healthBar.SetActive(false);
        }

        // Destruir el objeto después de la animación
        StartCoroutine(DisableAfterDeath());
    }

    private IEnumerator DisableAfterDeath()
    {
        yield return new WaitForSeconds(1f);
        gameObject.SetActive(false);
    }


    // Para visualizar el rango de detección y ataque en el editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        // Dibujar área de patrulla
        if (leftBoundary != null && rightBoundary != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(
                new Vector3(leftBoundary.position.x, leftBoundary.position.y - 0.5f, 0),
                new Vector3(leftBoundary.position.x, leftBoundary.position.y + 0.5f, 0)
            );
            Gizmos.DrawLine(
                new Vector3(rightBoundary.position.x, rightBoundary.position.y - 0.5f, 0),
                new Vector3(rightBoundary.position.x, rightBoundary.position.y + 0.5f, 0)
            );
            Gizmos.DrawLine(
                new Vector3(leftBoundary.position.x, leftBoundary.position.y, 0),
                new Vector3(rightBoundary.position.x, rightBoundary.position.y, 0)
            );
        }
    }

    bool HasGroundAhead()
    {
        float offset = isFacingRight ? 0.5f : -0.5f;

        Vector2 rayOrigin = new Vector2(transform.position.x + offset, transform.position.y);
        Vector2 direction = Vector2.down;

        Debug.DrawRay(rayOrigin, direction * frontCheckDistance, Color.red);


        return Physics2D.Raycast(rayOrigin, direction, frontCheckDistance, groundLayer);
    }
}