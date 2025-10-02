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
    [SerializeField] private float cameraShakeIntensity = 1.5f;
    [SerializeField] private float cameraShakeDuration = 0.1f;

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
        controles.Disable();
        controles.Base.Attack.performed -= ctx => TryAttack();
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

        currentCombo = (currentCombo % maxComboCount) + 1;
        lastAttackTime = Time.time;
        
        StartCoroutine(AttackSequence());
    }

    private IEnumerator AttackSequence()
    {
        canAttack = false;
        movimientoScript.sePuedeMover = false;
        
        // Activar la animación de ataque correspondiente
        animator.SetTrigger("Attack" + currentCombo);
        
        if (attackSounds.Length > 0)
        {
            int soundIndex = Random.Range(0, attackSounds.Length);
            audioSource.PlayOneShot(attackSounds[soundIndex]);
        }
        
        yield return new WaitForSeconds(0.2f);
        
        PerformAttack();
        
        yield return new WaitForSeconds(attackCooldown - 0.2f);
        
        movimientoScript.sePuedeMover = true;
        canAttack = true;
    }

    private void PerformAttack()
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
        
        bool hitAny = false;
        
        foreach (Collider2D enemy in hitEnemies)
        {
            hitAny = true;
            
            // Activar la animación de daño en el enemigo
            Animator enemyAnimator = enemy.GetComponent<Animator>();
            if (enemyAnimator != null)
            {
                enemyAnimator.SetTrigger("Hit");
            }
            
            EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
            if (enemyAI != null)
            {
                enemyAI.TakeDamage(attackDamage);
                ApplyKnockback(enemy.transform);
            }
            
            EnemyController enemyController = enemy.GetComponent<EnemyController>();
            if (enemyController != null)
            {
                enemyController.TakeDamage(attackDamage);
                ApplyKnockback(enemy.transform);
            }
            
            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, enemy.transform.position, Quaternion.identity);
            }
        }
        
        if (hitAny)
        {
            StartCoroutine(CameraShake());
            
            if (hitSounds.Length > 0)
            {
                int soundIndex = Random.Range(0, hitSounds.Length);
                audioSource.PlayOneShot(hitSounds[soundIndex]);
            }
        }
    }

    private void ApplyKnockback(Transform enemyTransform)
    {
        Vector2 knockbackDirection = (enemyTransform.position - transform.position).normalized;
        Rigidbody2D enemyRb = enemyTransform.GetComponent<Rigidbody2D>();
        if (enemyRb != null)
        {
            enemyRb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
        }
    }

    private IEnumerator CameraShake()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) yield break;
        
        Vector3 originalPosition = mainCamera.transform.position;
        float elapsed = 0f;
        
        while (elapsed < cameraShakeDuration)
        {
            float xOffset = Random.Range(-1f, 1f) * cameraShakeIntensity * 0.1f;
            float yOffset = Random.Range(-1f, 1f) * cameraShakeIntensity * 0.1f;
            
            mainCamera.transform.position = new Vector3(
                originalPosition.x + xOffset,
                originalPosition.y + yOffset,
                originalPosition.z);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        mainCamera.transform.position = originalPosition;
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