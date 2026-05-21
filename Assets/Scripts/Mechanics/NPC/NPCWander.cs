using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCWander : MonoBehaviour
{
    public enum State { Idle, Flee, Dead }
    private State state;

    [Header("Movement")]
    public float speed = 2f;
    public float fleeDistance = 2f;
    public float detectRange = 4f;
    public float patrolDistance = 3f;

    [Header("Detection")]
    public LayerMask enemyLayer;

    [Header("Health")]
    public float maxHealth = 3f;
    private float health;

    private Transform closestEnemy;

    private float startX;
    private float leftLimit;
    private float rightLimit;
    private float targetX;

    private float idleTimer;

    private Rigidbody2D rb;
    private Animator animator;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();

        startX = transform.position.x;
        leftLimit = startX - patrolDistance;
        rightLimit = startX + patrolDistance;

        health = maxHealth;

        SetIdle();
    }

    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        if (state == State.Dead) return;

        DetectEnemy();

        switch (state)
        {
            case State.Idle:
                HandleIdle();
                break;

            case State.Flee:
                HandleFlee();
                break;
        }
    }

    // =========================
    // DETECCIÓN (PRIORIDAD MÁXIMA)
    // =========================

    void DetectEnemy()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectRange, enemyLayer);

        if (hits.Length > 0)
        {
            closestEnemy = hits[0].transform;
            SetFlee();
        }
        else
        {
            closestEnemy = null;
        }
    }

    // =========================
    // IDLE
    // =========================

    void SetIdle()
    {
        state = State.Idle;

        idleTimer = Random.Range(1.5f, 2.5f);

        animator.SetBool("isWalking", false);
    }

    void HandleIdle()
    {
        idleTimer -= Time.deltaTime;

        if (idleTimer <= 0f)
        {
            idleTimer = Random.Range(1.5f, 2.5f);
        }
    }

    // =========================
    // FLEE (HUYE INMEDIATO)
    // =========================

    void SetFlee()
    {
        if (closestEnemy == null) return;

        state = State.Flee;

        animator.SetBool("isWalking", true);

        float dir = transform.position.x < closestEnemy.position.x ? -1f : 1f;

        targetX = transform.position.x + dir * fleeDistance;
        targetX = Mathf.Clamp(targetX, leftLimit, rightLimit);
    }

    void HandleFlee()
    {
        float newX = Mathf.MoveTowards(rb.position.x, targetX, speed * Time.deltaTime);

        rb.MovePosition(new Vector2(newX, rb.position.y));

        // Flip basado en movimiento REAL (no enemigo)
        if (newX > rb.position.x)
            transform.localScale = Vector3.one;
        else if (newX < rb.position.x)
            transform.localScale = new Vector3(-1, 1, 1);

        if (Mathf.Abs(newX - targetX) < 0.05f)
        {
            SetIdle();
        }
    }

    // =========================
    // DAMAGE
    // =========================

    public void TakeDamage(float dmg)
    {
        if (state == State.Dead) return;

        health -= dmg;

        animator.SetTrigger("hit");

        if (health <= 0)
        {
            Die();
        }
    }


    // =========================
    // DEATH
    // =========================

    public void Die()
    {
        state = State.Dead;

        animator.SetTrigger("die");

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.simulated = false;
        }

        //gameObject.layer = LayerMask.NameToLayer("Dead");

        this.enabled = false;
    }

    // =========================
    // DEBUG
    // =========================

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectRange);
    }
}