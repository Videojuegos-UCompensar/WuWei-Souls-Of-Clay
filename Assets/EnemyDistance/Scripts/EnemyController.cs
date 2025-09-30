using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float retreatDistance = 5f;
    [SerializeField] private float attackRange = 7f;
    [SerializeField] private float minDistance = 3f;
    [SerializeField] private float lowHealthThreshold = 30f;
    [SerializeField] private float lowHealthRetreatMultiplier = 1.5f;

    [Header("Movement & Jumping")]
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckRadius = 0.2f;

    [Header("Attack")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float projectileSpeed = 8f;
    [SerializeField] private int projectileDamage = 10;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float attackAnimationTime = 0.5f;

    [Header("Visual Feedback")]
    [SerializeField] private GameObject healthBarPrefab;
    [SerializeField] private Vector3 healthBarOffset = new Vector3(0, 1.2f, 0);
    [SerializeField] private Color lowHealthColor = Color.red;
    [SerializeField] private Color fullHealthColor = Color.green;

    // References
    private Transform player;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private GameObject healthBar;
    private Transform healthFill;

    // State variables
    private int currentHealth;
    private bool isAttacking = false;
    private bool isDead = false;
    private float lastAttackTime;
    private bool isFacingRight = true;
    private bool hasLowHealth = false;
    private bool isGrounded;
    private float jumpCooldown = 0f;

    // Animation parameters
    private readonly string ANIM_IDLE = "Idle";
    private readonly string ANIM_WALK = "Walk";
    private readonly string ANIM_ATTACK = "Attack";
    private readonly string ANIM_HURT = "Hurt";
    private readonly string ANIM_DEATH = "Death";
    private readonly string ANIM_JUMP = "Jump";

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        
        currentHealth = maxHealth;
    }

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        if (player == null)
        {
            Debug.LogError("Player not found. Make sure the player has the 'Player' tag.");
        }

        if (healthBarPrefab != null)
        {
            healthBar = Instantiate(healthBarPrefab, transform.position + healthBarOffset, Quaternion.identity);
            healthBar.transform.SetParent(transform);
            healthFill = healthBar.transform.Find("Fill");
            UpdateHealthBar();
        }

        if (groundCheck == null)
        {
            GameObject checkObject = new GameObject("GroundCheck");
            checkObject.transform.SetParent(transform);
            checkObject.transform.localPosition = new Vector3(0, -0.5f, 0);
            groundCheck = checkObject.transform;
        }
    }

    private void Update()
    {
        if (isDead || player == null) return;

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        
        if (jumpCooldown > 0)
        {
            jumpCooldown -= Time.deltaTime;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        
        UpdateFacingDirection();
        
        if (distanceToPlayer <= attackRange && Time.time > lastAttackTime + attackCooldown)
        {
            StartCoroutine(Attack());
        }
        
        UpdateAnimations(distanceToPlayer);
        
        if (healthBar != null)
        {
            healthBar.transform.position = transform.position + healthBarOffset;
        }

        // Try jumping if there's an obstacle
        TryJump();
    }

    private void FixedUpdate()
    {
        if (isDead || player == null || isAttacking) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        
        float effectiveRetreatDistance = retreatDistance;
        float effectiveMinDistance = minDistance;
        
        hasLowHealth = (float)currentHealth / maxHealth * 100 <= lowHealthThreshold;
        
        if (hasLowHealth)
        {
            effectiveRetreatDistance *= lowHealthRetreatMultiplier;
            effectiveMinDistance *= lowHealthRetreatMultiplier;
        }
        
        if (distanceToPlayer < effectiveRetreatDistance && distanceToPlayer > effectiveMinDistance)
        {
            Vector2 direction = (transform.position - player.position).normalized;
            rb.velocity = new Vector2(direction.x * moveSpeed, rb.velocity.y);
        }
        else if (distanceToPlayer <= effectiveMinDistance)
        {
            Vector2 direction = (transform.position - player.position).normalized;
            rb.velocity = new Vector2(direction.x * moveSpeed * (hasLowHealth ? 2f : 1.5f), rb.velocity.y);
        }
        else
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
        }
    }

    private void UpdateFacingDirection()
    {
        bool shouldFaceRight = player.position.x > transform.position.x;
        
        if (shouldFaceRight != isFacingRight)
        {
            Flip();
        }
    }

    private void UpdateAnimations(float distanceToPlayer)
    {
        // Update animator parameters instead of directly playing animations
        animator.SetFloat("Speed", Mathf.Abs(rb.velocity.x));
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsAttacking", isAttacking);
        animator.SetBool("IsDead", isDead);
        animator.SetBool("LowHealth", hasLowHealth);
        
        // Determine if the enemy is fleeing or retreating
        bool isFleeing = hasLowHealth && distanceToPlayer < retreatDistance * lowHealthRetreatMultiplier;
        animator.SetBool("IsFleeing", isFleeing);
        
        // Determine if the enemy is in retreat mode
        bool isRetreating = distanceToPlayer < retreatDistance && distanceToPlayer > minDistance;
        animator.SetBool("IsRetreating", isRetreating);
        
        // Set jumping state
        if (!isGrounded && rb.velocity.y > 0.1f)
        {
            animator.SetBool("IsJumping", true);
        }
        else
        {
            animator.SetBool("IsJumping", false);
        }
        
        // Determine if we're chasing the player
        bool isChasing = distanceToPlayer <= attackRange * 1.5f && distanceToPlayer > attackRange;
        animator.SetBool("IsChasing", isChasing);
    }

    private void Jump()
    {
        if (isGrounded && jumpCooldown <= 0)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            jumpCooldown = 1.0f;
            animator.SetTrigger("Jump");
        }
    }

    private void TryJump()
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

    private IEnumerator Attack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        
        rb.velocity = Vector2.zero;
        
        // Use trigger instead of direct animation play
        animator.SetTrigger("Attack");
        
        yield return new WaitForSeconds(attackAnimationTime);
        
        if (!isDead && player != null)
        {
            if (projectilePrefab != null && firePoint != null)
            {
                GameObject projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
                Vector2 direction = (player.position - firePoint.position).normalized;
                projectile.GetComponent<Rigidbody2D>().velocity = direction * projectileSpeed;
                
                Projectile projectileScript = projectile.GetComponent<Projectile>();
                if (projectileScript != null)
                {
                    projectileScript.SetDamage(projectileDamage);
                }
            }
        }
        
        yield return new WaitForSeconds(attackAnimationTime);
        isAttacking = false;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        UpdateHealthBar();
        
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(PlayHurtAnimation());
            StartCoroutine(FlashRed());
        }
    }

    private IEnumerator PlayHurtAnimation()
    {
        // Use trigger instead of direct animation play
        animator.SetTrigger("Hit");
        isAttacking = true;
        rb.velocity = Vector2.zero;
        yield return new WaitForSeconds(0.5f);
        isAttacking = false;
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

    private void Die()
    {
        isDead = true;
        
        // Use trigger instead of direct animation play
        animator.SetTrigger("Die");
        
        rb.velocity = Vector2.zero;
        GetComponent<Collider2D>().enabled = false;
        
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

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 localScale = transform.localScale;
        localScale.x *= -1;
        transform.localScale = localScale;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, retreatDistance);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, minDistance);
        
        if (Application.isPlaying && hasLowHealth)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, retreatDistance * lowHealthRetreatMultiplier);
        }

        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}