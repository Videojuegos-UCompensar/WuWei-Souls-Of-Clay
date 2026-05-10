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
    private enum State { Patrolling, Chasing, Attacking, Fleeing, Returning }
    private State currentState;
    private float lastAttackTime;
    private Vector2 fleeDirection;
    private bool isGrounded;
    private float jumpCooldown = 0f;
    private GameObject healthBar;
    private SimpleHealthBar simpleHealthBar; // <-- Referencia al componente SimpleHealthBar
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
        
        if (leftBoundary == null || rightBoundary == null)
        {
            Debug.LogError("Los límites de patrulla no están configurados en " + gameObject.name);
        }
        
        if (player == null)
        {
            Debug.LogWarning("No se encontró el jugador con tag 'Player'");
        }

        // Crear barra de vida
        if (healthBarPrefab != null)
        {
            // FIX: Instanciar la barra en la escena SIN hacerla hija del enemigo directamente,
            // para evitar que el Flip() del enemigo deforme la barra.
            healthBar = Instantiate(healthBarPrefab, transform.position + healthBarOffset, Quaternion.identity);
            
            // Obtener el componente SimpleHealthBar del prefab instanciado
            simpleHealthBar = healthBar.GetComponent<SimpleHealthBar>();
            
            if (simpleHealthBar == null)
            {
                Debug.LogWarning("El prefab de la barra de vida no tiene el componente SimpleHealthBar en " + gameObject.name);
            }
            
            UpdateHealthBar();
        }
        else
        {
            Debug.LogWarning("healthBarPrefab no está asignado en el Inspector para " + gameObject.name);
        }
    }

    void Update()
    {
        if (isDead) return;

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        
        if (jumpCooldown > 0)
        {
            jumpCooldown -= Time.deltaTime;
        }
        
        float distanceToPlayer = player != null ? Vector2.Distance(transform.position, player.position) : float.MaxValue;
        
        UpdateState(distanceToPlayer);
        
        switch (currentState)
        {
            case State.Patrolling:
                Patrol();
                break;
            case State.Chasing:
                ChasePlayer();
                break;
            case State.Attacking:
                AttackPlayer();
                break;
            case State.Fleeing:
                FleeFromPlayer();
                break;
            case State.Returning:
                ReturnToPatrolArea();
                break;
        }
        
        UpdateAnimations();

        // FIX: Actualizar posición de la barra manualmente cada frame (ya que no es hija del enemigo)
        // y forzar que nunca se voltee con el enemigo.
        if (healthBar != null)
        {
            healthBar.transform.position = transform.position + healthBarOffset;
            // Asegurar que la escala X de la barra siempre sea positiva
            Vector3 barScale = healthBar.transform.localScale;
            barScale.x = Mathf.Abs(barScale.x);
            healthBar.transform.localScale = barScale;
        }
    }
    
    void UpdateState(float distanceToPlayer)
    {
        if (currentHealth <= maxHealth * (lowHealthThreshold / 100) && distanceToPlayer <= fleeDistance)
        {
            currentState = State.Fleeing;
            return;
        }
        
        if (IsOutsidePatrolArea() && currentState != State.Chasing && currentState != State.Attacking && currentState != State.Fleeing)
        {
            currentState = State.Returning;
            return;
        }
        
        if (distanceToPlayer <= detectionRange && distanceToPlayer > attackRange)
        {
            currentState = State.Chasing;
            return;
        }
        
        if (distanceToPlayer <= attackRange)
        {
            currentState = State.Attacking;
            return;
        }
        
        if (currentState != State.Patrolling && !IsOutsidePatrolArea() && currentState != State.Fleeing)
        {
            currentState = State.Patrolling;
        }
    }
    
    void Patrol()
    {
        if (isWaiting)
            return;
            
        if (isFacingRight && transform.position.x >= rightBoundary.position.x)
        {
            Flip();
            StartCoroutine(WaitAtBoundary());
        }
        else if (!isFacingRight && transform.position.x <= leftBoundary.position.x)
        {
            Flip();
            StartCoroutine(WaitAtBoundary());
        }
        
        rb.velocity = new Vector2(isFacingRight ? moveSpeed : -moveSpeed, rb.velocity.y);
        TryJump();
    }
    
    void ChasePlayer()
    {
        if (player == null) return;
        
        if ((player.position.x > transform.position.x && !isFacingRight) ||
            (player.position.x < transform.position.x && isFacingRight))
        {
            Flip();
        }
        
        float direction = player.position.x > transform.position.x ? 1 : -1;
        rb.velocity = new Vector2(direction * moveSpeed, rb.velocity.y);
        
        if (isGrounded && player.position.y > transform.position.y + 0.5f)
        {
            Jump();
        }
        else
        {
            TryJump();
        }
    }
    
    void AttackPlayer()
    {
        if (player == null) return;
        
        rb.velocity = new Vector2(0, rb.velocity.y);
        
        if ((player.position.x > transform.position.x && !isFacingRight) ||
            (player.position.x < transform.position.x && isFacingRight))
        {
            Flip();
        }
        
        if (canAttack && !isAttacking)
        {
            StartCoroutine(PerformAttack());
        }
    }
    
    void FleeFromPlayer()
    {
        if (player == null) return;
        
        float direction = transform.position.x > player.position.x ? 1 : -1;
        
        if ((direction > 0 && !isFacingRight) || (direction < 0 && isFacingRight))
        {
            Flip();
        }
        
        rb.velocity = new Vector2(direction * fleeSpeed, rb.velocity.y);
        TryJump();
    }
    
    void ReturnToPatrolArea()
    {
        Vector3 targetPosition;
        
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
            currentState = State.Patrolling;
            return;
        }
        
        float direction = targetPosition.x > transform.position.x ? 1 : -1;
        
        if ((direction > 0 && !isFacingRight) || (direction < 0 && isFacingRight))
        {
            Flip();
        }
        
        rb.velocity = new Vector2(direction * moveSpeed, rb.velocity.y);
        TryJump();
        
        if (!IsOutsidePatrolArea())
        {
            currentState = State.Patrolling;
        }
    }
    
    IEnumerator WaitAtBoundary()
    {
        isWaiting = true;
        rb.velocity = new Vector2(0, rb.velocity.y);
        yield return new WaitForSeconds(waitTime);
        isWaiting = false;
    }
    
    IEnumerator PerformAttack()
    {
        canAttack = false;
        isAttacking = true;
        
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
        
        yield return new WaitForSeconds(0.3f);
        
        Collider2D playerCollider = Physics2D.OverlapCircle(transform.position, attackRange, playerLayer);
        if (playerCollider != null)
        {
            PlayerHealth playerHealth = playerCollider.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
            }
        }
        
        yield return new WaitForSeconds(0.5f);
        isAttacking = false;
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
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
        UpdateHealthBar();
        
        if (animator != null)
        {
            animator.SetTrigger("Hit");
        }

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

    // FIX: UpdateHealthBar ahora usa el componente SimpleHealthBar correctamente
    private void UpdateHealthBar()
    {
        if (simpleHealthBar != null)
        {
            simpleHealthBar.UpdateHealthBar(currentHealth, maxHealth);
        }
    }
    
    void Die()
    {
        isDead = true;
        
        if (animator != null)
        {
            animator.SetTrigger("Die");
        }
        
        GetComponent<Collider2D>().enabled = false;
        rb.velocity = Vector2.zero;
        rb.gravityScale = 0;
        this.enabled = false;
        
        if (healthBar != null)
        {
            healthBar.SetActive(false);
        }
        
        StartCoroutine(DisableAfterDeath());
    }

    private IEnumerator DisableAfterDeath()
    {
        yield return new WaitForSeconds(1f);
        gameObject.SetActive(false);
    }
    
    void UpdateAnimations()
    {
        if (animator != null)
        {
            animator.SetFloat("Speed", Mathf.Abs(rb.velocity.x));
            animator.SetBool("IsGrounded", isGrounded);
            animator.SetBool("IsChasing", currentState == State.Chasing);
            animator.SetBool("IsFleeing", currentState == State.Fleeing);
            animator.SetBool("LowHealth", currentHealth <= maxHealth * (lowHealthThreshold / 100));
        }
    }
    
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

    // Limpiar la barra de vida si el enemigo es destruido
    void OnDestroy()
    {
        if (healthBar != null)
        {
            Destroy(healthBar);
        }
    }
}