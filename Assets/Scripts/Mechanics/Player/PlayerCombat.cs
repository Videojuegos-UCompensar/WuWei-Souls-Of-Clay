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
      
    }

    private void ApplyKnockback(Transform enemyTransform)
    {
        
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