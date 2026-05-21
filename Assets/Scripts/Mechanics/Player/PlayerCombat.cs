using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    [Header("Attack Properties")]
    [SerializeField] private int attackDamage = 20;
    [SerializeField] private float attackRange = 1.2f;
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private Transform attackPoint;

    [Header("Effects")]
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private float knockbackForce = 3f;

    [Header("Combo System")]
    [SerializeField] private int maxComboCount = 3;
    [SerializeField] private float comboResetTime = 1.5f;

    // Referencias
    private Animator animator;
    private Movimiento2D movimientoScript;
    private Controles controles;
    private bool canAttack = true;
    private int currentCombo = 0;
    private float lastAttackTime = 0f;

    // Audio
    private AudioSource audioSource;
    [SerializeField] private AudioClip[] attackSounds;
    [SerializeField] private AudioClip[] hitSounds;
    [SerializeField] private bool debug = false;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        movimientoScript = GetComponent<Movimiento2D>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (attackPoint == null)
        {
            GameObject newAttackPoint = new GameObject("AttackPoint");
            newAttackPoint.transform.SetParent(transform);
            newAttackPoint.transform.localPosition = new Vector3(1f, 0f, 0f);
            attackPoint = newAttackPoint.transform;
        }

        controles = new Controles();
    }

    private void OnEnable()
    {
        controles.Enable();
        controles.Base.Attack.performed += ctx => TryAttack();
    }

    private void OnDisable()
    {
        controles.Base.Attack.performed -= ctx => TryAttack();
        controles.Disable();
    }

    private void Update()
    {
        if (Time.time > lastAttackTime + comboResetTime && currentCombo > 0)
        {
            currentCombo = 0;
        }
    }

    private void TryAttack()
    {
        if (!canAttack) return;

        movimientoScript.sePuedeMover = false;
        movimientoScript.rbd.velocity = Vector2.zero;

        currentCombo = (currentCombo % maxComboCount) + 1;
        lastAttackTime = Time.time;

        StartCoroutine(AttackSequence());
    }

    private IEnumerator AttackSequence()
    {
        canAttack = false;
        if (movimientoScript != null) movimientoScript.sePuedeMover = false;

        // Activar la animación de ataque correspondiente
        if (animator != null) animator.SetTrigger("Attack" + currentCombo);

        if (attackSounds != null && attackSounds.Length > 0 && audioSource != null)
        {
            int soundIndex = Random.Range(0, attackSounds.Length);
            audioSource.PlayOneShot(attackSounds[soundIndex]);
        }

        // Esperar el momento del golpe (ajusta según animación)
        yield return new WaitForSeconds(0.2f);

        PerformAttack();

        yield return new WaitForSeconds(Mathf.Max(0, attackCooldown - 0.2f));

        if (movimientoScript != null) movimientoScript.sePuedeMover = true;
        canAttack = true;
    }

    private void PerformAttack()
    {
        if (attackPoint == null)
        {
            Debug.LogWarning("PlayerCombat: attackPoint is null, cannot perform attack");
            return;
        }

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);

        if (hitEnemies == null || hitEnemies.Length == 0)
        {
            if (hitSounds != null && hitSounds.Length > 0 && audioSource != null)
            {
                int s = Random.Range(0, hitSounds.Length);
                audioSource.PlayOneShot(hitSounds[s]);
            }
            if (debug) Debug.Log("PlayerCombat: no enemies hit");
            return;
        }

        // Evitar aplicar daño múltiple a la misma entidad si tiene varios colliders
        var damaged = new HashSet<GameObject>();
        foreach (var enemy in hitEnemies)
        {
            if (enemy == null) continue;
            var root = enemy.transform.root.gameObject;
            if (damaged.Contains(root)) continue;
            damaged.Add(root);

            if (debug) Debug.Log($"PlayerCombat: hit {enemy.name} (root={root.name})");

            var hc = root.GetComponent<HealthComponent>() ?? root.GetComponentInChildren<HealthComponent>(true);
            if (hc != null)
            {
                hc.TakeDamage(attackDamage);
            }
            else
            {
                // Fallback: try reflection on root's mono behaviours
                var behaviour = root.GetComponent<MonoBehaviour>();
                if (behaviour != null)
                {
                    var method = behaviour.GetType().GetMethod("TakeDamage");
                    if (method != null)
                    {
                        try { method.Invoke(behaviour, new object[] { attackDamage }); }
                        catch { }
                    }
                }
            }

            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, root.transform.position, Quaternion.identity);
            }

            Rigidbody2D rb = root.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 knockDir = (root.transform.position - transform.position).normalized;
                rb.AddForce(knockDir * knockbackForce, ForceMode2D.Impulse);
            }
        }
    }

    private void ApplyKnockback(Transform enemyTransform)
    {
        // Placeholder in case you want a custom knockback behavior per enemy
    }

    public void OnAttackEvent()
    {
        PerformAttack();
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}