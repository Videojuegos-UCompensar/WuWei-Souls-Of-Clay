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
        
        // Verificar que los componentes necesarios estén configurados
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
        
        // Verificar si el jugador está en rango de detección
        float distanceToPlayer = player != null ? Vector2.Distance(transform.position, player.position) : float.MaxValue;
        
        // Actualizar el estado del enemigo según las condiciones
        UpdateState(distanceToPlayer);
        
        // Ejecutar comportamiento según el estado actual
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
        
        // Actualizar animaciones
        UpdateAnimations();

        // Actualizar posición de la barra de vida
        if (healthBar != null)
        {
            healthBar.transform.position = transform.position + healthBarOffset;
        }
    }
    
    void UpdateState(float distanceToPlayer)
    {
        // Si tiene poca vida, huir es prioridad
        if (currentHealth <= maxHealth * (lowHealthThreshold / 100) && distanceToPlayer <= fleeDistance)
        {
            currentState = State.Fleeing;
            return;
        }
        
        // Si está fuera de su zona de patrulla y no está persiguiendo al jugador
        if (IsOutsidePatrolArea() && currentState != State.Chasing && currentState != State.Attacking && currentState != State.Fleeing)
        {
            currentState = State.Returning;
            return;
        }
        
        // Si detecta al jugador, perseguirlo
        if (distanceToPlayer <= detectionRange && distanceToPlayer > attackRange)
        {
            currentState = State.Chasing;
            return;
        }
        
        // Si el jugador está en rango de ataque
        if (distanceToPlayer <= attackRange)
        {
            currentState = State.Attacking;
            return;
        }
        
        // Si no se cumple ninguna condición especial y está dentro de su zona, patrullar
        if (currentState != State.Patrolling && !IsOutsidePatrolArea() && currentState != State.Fleeing)
        {
            currentState = State.Patrolling;
        }
    }
    
    void Patrol()
    {
        if (isWaiting)
            return;
            
        // Determinar dirección de movimiento
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
        
        // Moverse en la dirección actual
        rb.velocity = new Vector2(isFacingRight ? moveSpeed : -moveSpeed, rb.velocity.y);
        
        // Saltar ocasionalmente para superar obstáculos pequeños
        TryJump();
    }
    
    void ChasePlayer()
    {
        if (player == null) return;
        
        // Determinar si debe voltearse para mirar al jugador
        if ((player.position.x > transform.position.x && !isFacingRight) ||
            (player.position.x < transform.position.x && isFacingRight))
        {
            Flip();
        }
        
        // Mover hacia el jugador
        float direction = player.position.x > transform.position.x ? 1 : -1;
        rb.velocity = new Vector2(direction * moveSpeed, rb.velocity.y);
        
        // Saltar si hay un obstáculo o si el jugador está más alto
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
        
        // Frenar movimiento durante el ataque
        rb.velocity = new Vector2(0, rb.velocity.y);
        
        // Asegurarse de estar mirando al jugador
        if ((player.position.x > transform.position.x && !isFacingRight) ||
            (player.position.x < transform.position.x && isFacingRight))
        {
            Flip();
        }
        
        // Realizar ataque si no está en cooldown
        if (canAttack && !isAttacking)
        {
            StartCoroutine(PerformAttack());
        }
    }
    
    void FleeFromPlayer()
    {
        if (player == null) return;
        
        // Determinar dirección de huida (opuesta al jugador)
        float fleeDirection = transform.position.x < player.position.x ? -1 : 1;
        
        // Voltear según la dirección de huida
        if ((fleeDirection > 0 && !isFacingRight) || (fleeDirection < 0 && isFacingRight))
        {
            Flip();
        }
        
        // Moverse más rápido para huir
        rb.velocity = new Vector2(fleeDirection * fleeSpeed, rb.velocity.y);
        
        // Saltar para escapar más rápido o evitar obstáculos
        TryJump();
        
        // Si ya está lo suficientemente lejos, volver a patrullar
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        if (distanceToPlayer > fleeDistance * 1.5f)
        {
            currentState = State.Returning;
        }
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
        
        yield return new WaitForSeconds(waitTime);
        
        isWaiting = false;
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
            // Intentar aplicar daño al jugador
            PlayerHealth playerHealth = playerCollider.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
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
    
    void UpdateAnimations()
    {
        if (animator != null)
        {
            // Actualizar parámetros del animator
            animator.SetFloat("Speed", Mathf.Abs(rb.velocity.x));
            animator.SetBool("IsGrounded", isGrounded);
            
            // Estados adicionales
            animator.SetBool("IsChasing", currentState == State.Chasing);
            animator.SetBool("IsFleeing", currentState == State.Fleeing);
            
            // Parámetro de vida baja
            animator.SetBool("LowHealth", currentHealth <= maxHealth * (lowHealthThreshold / 100));
        }
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
}