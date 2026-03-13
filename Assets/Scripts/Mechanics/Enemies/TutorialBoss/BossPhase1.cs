using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class BossPhase1 : MonoBehaviour
{
    public enum BossState { Idle, Chase, Jump, Attack }

    private bool phase2Activated = false;
    private HealthComponent health;
    private bool isPerformingSpecial = false;
    private float originalAttackRange;



    [Header("Phase 2")]
[SerializeField] private bool enableSpecialAttack = false;

[Header("Dash")]
[SerializeField] private float dashForce = 35f;
[SerializeField] private float dashDuration = 0.3f;

[Header("Vertical Slash")]
[SerializeField] private GameObject windSlashPrefab;
[SerializeField] private Transform slashSpawnPoint;


[Header("Orbs")]
[SerializeField] private GameObject orbPrefab;
[SerializeField] private int orbCount = 3;
[SerializeField] private float orbSpawnRadius = 1.5f;


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
    public int damage = 1;
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
        health = GetComponent<HealthComponent>();
        health.OnHealthChanged += CheckPhase;
        originalAttackRange = attackRange;
    }

private void Update()
{
    if (player == null) return;

    float distance = Vector2.Distance(transform.position, player.position);
    playerDetected = distance <= detectionRange;

    // 🔥 Si está haciendo especial → solo mirar al jugador
    if (isPerformingSpecial)
    {
        FacePlayer();
        return;
    }

    if (!playerDetected)
    {
        ChangeState(BossState.Idle);
        return;
    }

    float currentRange = attackRange;

if (enableSpecialAttack && Random.value < 0.3f) // 30% probabilidad
    currentRange = 10f;

if (distance <= currentRange)
{
    ChangeState(BossState.Attack);
    TryAttack();
}

    else
    {
        ChangeState(BossState.Chase);
    }
}

private void FacePlayer()
{
    float direction = Mathf.Sign(player.position.x - transform.position.x);
    transform.localScale = new Vector3(direction, 1, 1);
}


    private void FixedUpdate()
{
    if (isPerformingSpecial)
        return;

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

private void CheckPhase(HealthComponent source, float current, float max)
{
    float percentage = current / max;

    if (!phase2Activated && percentage <= 0.75f)
    {
        ActivatePhase2();
    }
}

private void ActivatePhase2()
{
    phase2Activated = true;

    Debug.Log("FASE 2 ACTIVADA");

    // Cambiar comportamiento IA
    moveSpeed *= 1.1f;
    attackCooldown *= 1f;

    // Opcional: pequeña pausa dramática
    StartCoroutine(PhaseTransition());
}

private IEnumerator PhaseTransition()
{
    // Mini freeze
    Time.timeScale = 0f;
    yield return new WaitForSecondsRealtime(0.2f);
    Time.timeScale = 1f;

    // Activar nuevos ataques
    enableSpecialAttack = true;
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

    if (enableSpecialAttack)
    {
        float roll = Random.value;

        if (roll < 0.4f)
        {
            StartCoroutine(DashAttack());          // 40%
        }
        else if (roll < 0.8f)
        {
            StartCoroutine(VerticalSlashRoutine()); // 40%
        }
        else
        {
            StartCoroutine(SummonOrbsRoutine());    // 20%
        }
    }
    else
    {
        animator.SetTrigger("Attack");
    }
}


private IEnumerator DashAttack()
{
    isPerformingSpecial = true;

    ChangeState(BossState.Attack);

    animator.SetTrigger("Dash");

    yield return new WaitForSeconds(0.1f);

    float direction = Mathf.Sign(transform.localScale.x);

    rb.velocity = new Vector2(direction * dashForce, 0);

    yield return new WaitForSeconds(dashDuration);

    rb.velocity = Vector2.zero;

    isPerformingSpecial = false;
}

public void StopBoss()
{
    // Cancelar coroutines activas (dash, slash, summon)
    StopAllCoroutines();

    // Parar movimiento
    rb.velocity = Vector2.zero;

    // Reset estados
    isPerformingSpecial = false;
    enableSpecialAttack = false;

    // Destruir orbes activas
    HomingOrb[] orbs = FindObjectsOfType<HomingOrb>();
    foreach (HomingOrb orb in orbs)
    {
        Destroy(orb.gameObject);
    }

    // Destruir cortes de viento activos
    WindSlash[] slashes = FindObjectsOfType<WindSlash>();
    foreach (WindSlash slash in slashes)
    {
        Destroy(slash.gameObject);
    }
}



private IEnumerator VerticalSlashRoutine()
{
    isPerformingSpecial = true;

    animator.SetTrigger("VerticalSlash");

    yield return new WaitForSeconds(0.2f);

    SpawnWindSlash();

    yield return new WaitForSeconds(0.3f);

    isPerformingSpecial = false;

    attackRange = originalAttackRange;
}

private void SpawnWindSlash()
{
    if (windSlashPrefab == null || slashSpawnPoint == null)
        return;

    float direction = Mathf.Sign(transform.localScale.x);

    GameObject slash = Instantiate(
        windSlashPrefab,
        slashSpawnPoint.position,
        Quaternion.identity
    );

    slash.transform.localScale = new Vector3(direction, 1, 1);

    WindSlash ws = slash.GetComponent<WindSlash>();
    if (ws != null)
        ws.SetDirection(new Vector2(direction, 0));
}


private IEnumerator SummonOrbsRoutine()
{
    isPerformingSpecial = true;

    animator.SetTrigger("Summon");

    yield return new WaitForSeconds(0.3f);

    for (int i = 0; i < orbCount; i++)
    {
    Vector2 spawnPos =
            (Vector2)transform.position +
            Random.insideUnitCircle * orbSpawnRadius;

        GameObject orb = Instantiate(orbPrefab, spawnPos, Quaternion.identity);

        HomingOrb homing = orb.GetComponent<HomingOrb>();
        if (homing != null)
            homing.SetTarget(player);
    }

    yield return new WaitForSeconds(0.5f);

    isPerformingSpecial = false;

    attackRange = originalAttackRange;
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
