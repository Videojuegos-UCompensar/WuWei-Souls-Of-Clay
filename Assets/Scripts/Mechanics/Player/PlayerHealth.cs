using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Para UI de vida
using UnityEngine.Events; // Para eventos Unity

public class PlayerHealth : MonoBehaviour, IRestartable
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    public int currentHealth;
    
    public float invincibilityTime = 1f; // Tiempo de invencibilidad después de recibir daño
    [SerializeField] private bool flashOnDamage = true;
    private bool isDead = false;
    [SerializeField] private int flashCount = 3;
    
    [Header("UI")]
    public Image healthBar; // Referencia a una barra de vida UI
    public bool showDamageEffect = true;
    
    [Header("Efectos de Muerte")]
    public GameObject deathEffectPrefab; // Prefab con efecto visual (opcional)
    public AudioClip deathSound; // Sonido al morir (opcional)
    [SerializeField] private SimpleHealthBar simpleHealthBar; // Barra de salud alternativa
    public AudioClip hitSound; // Sonido al recibir daño (opcional)
    
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private AudioSource audioSource;
    
    // Variables para restaurar el estado inicial
    private int initialHealth;
    
    [Header("Events")]
    public UnityEvent<int> OnDamaged;
    public UnityEvent<int> OnHealed;
    public UnityEvent OnDeath;
    
    // Private variables
    private bool isInvincible = false;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    
    // Properties
    public bool IsInvincible => isInvincible;
    
    void Start()
    {
        // Inicializar componentes
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Guardar estado inicial
        currentHealth = maxHealth;
        initialHealth = maxHealth;
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        
        // Actualizar UI inicial si existe
        UpdateHealthUI();
        
        // Registrarse en el LevelManager si existe
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.FindRestartableObjects();
        }
    }
    
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        
        // Si no hay SimpleHealthBar asignada, intenta encontrarla
        if (simpleHealthBar == null)
        {
            simpleHealthBar = GetComponentInChildren<SimpleHealthBar>();
        }
    }
    
    public void TakeDamage(int damage)
    {
        // Si está invencible o muerto, no recibe daño
        if (isInvincible || isDead)
            return;
            
        currentHealth -= damage;
        
        // Limitar salud mínima a 0
        currentHealth = Mathf.Max(currentHealth, 0);
        
        // Actualizar UI
        UpdateHealthUI();
        
        // Reproducir sonido de daño
        if (audioSource != null && hitSound != null)
        {
            audioSource.PlayOneShot(hitSound);
        }
        
        // Activar animación de daño si existe
        if (animator != null)
        {
            animator.SetTrigger("Hit");
        }
        
        // Efecto visual de daño
        if (showDamageEffect && spriteRenderer != null)
        {
            StartCoroutine(DamageEffect());
        }
        
        // Activar invencibilidad temporal
        StartCoroutine(InvincibilityFrames());
        
        // Verificar si el jugador ha muerto
        if (currentHealth <= 0 && !isDead)
        {
            Die();
        }
    }
    
    IEnumerator DamageEffect()
    {
        // Parpadeo rojo al recibir daño
        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = originalColor;
    }
    
    IEnumerator InvincibilityFrames()
    {
        isInvincible = true;
        
        // Efecto de parpadeo durante la invencibilidad
        float elapsedTime = 0f;
        
        if (flashOnDamage && spriteRenderer != null)
        {
            while (elapsedTime < invincibilityTime)
            {
                spriteRenderer.enabled = !spriteRenderer.enabled;
                yield return new WaitForSeconds(0.1f);
                elapsedTime += 0.1f;
            }
            
            spriteRenderer.enabled = true;
        }
        else
        {
            // Si no queremos parpadeo, simplemente esperamos
            yield return new WaitForSeconds(invincibilityTime);
        }
        
        isInvincible = false;
    }
    
    /// <summary>
    /// Método alternativo para mantener compatibilidad con ambos scripts
    /// </summary>
    public void TakeDamage(int damage, GameObject attacker)
    {
        TakeDamage(damage);
    }
    
    void UpdateHealthUI()
    {
        // Actualizar barra de vida UI si existe
        if (healthBar != null)
        {
            healthBar.fillAmount = (float)currentHealth / maxHealth;
        }
        
        // Actualizar SimpleHealthBar si existe
        if (simpleHealthBar != null)
        {
            simpleHealthBar.UpdateHealthBar((float)currentHealth, maxHealth);
        }
    }
    
    public void Heal(int amount)
    {
        // No curar si está muerto
        if (isDead)
            return;
            
        currentHealth += amount;
        
        // Limitar salud máxima
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        
        // Actualizar UI
        UpdateHealthUI();
    }
    
    /// <summary>
    /// Método mejorado de parpadeo con número configurable de flashes
    /// </summary>
    private IEnumerator FlashRoutine()
    {
        if (spriteRenderer == null) yield break;
        
        Color originalColor = spriteRenderer.color;
        Color flashColor = Color.red;
        
        for (int i = 0; i < flashCount; i++)
        {
            spriteRenderer.color = flashColor;
            yield return new WaitForSeconds(invincibilityTime / (flashCount * 2));
            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(invincibilityTime / (flashCount * 2));
        }
    }
    
    public void RestoreFullHealth()
    {
        // Restaurar salud completa
        isDead = false;
        currentHealth = maxHealth;
        
        // Actualizar UI
        UpdateHealthUI();
        
        // Reactivar componentes
        if (GetComponent<Collider2D>() != null)
        {
            GetComponent<Collider2D>().enabled = true;
        }
        
        // Reactivar scripts de movimiento
        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            script.enabled = true;
        }
    }
    
    public void Die()
    {
        // Evitar llamadas múltiples
        if (isDead) return;
        
        isDead = true;
        
        // Reproducir sonido de muerte
        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }
        
        // Instanciar efecto de muerte
        if (deathEffectPrefab != null)
        {
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
        }
        
        // Activar animación de muerte si existe
        if (animator != null)
        {
            animator.SetTrigger("Die");
        }
        
        // Desactivar movimiento y colisiones
        GetComponent<Collider2D>().enabled = false;
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }
        
        // Desactivar scripts de control del jugador (excepto este)
        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            if (script != this)
            {
                script.enabled = false;
            }
        }
        
        // Iniciar reinicio del nivel
        StartCoroutine(TriggerRespawn());
    }
    
    /// <summary>
    /// Set health to a specific value
    /// </summary>
    /// <param name="value">New health value</param>
    public void SetHealth(int value)
    {
        int oldHealth = currentHealth;
        currentHealth = Mathf.Clamp(value, 0, maxHealth);
        
        // Update UI
        UpdateHealthUI();
        
        // Trigger appropriate events
        if (currentHealth < oldHealth)
        {
            OnDamaged?.Invoke(oldHealth - currentHealth);
        }
        else if (currentHealth > oldHealth)
        {
            OnHealed?.Invoke(currentHealth - oldHealth);
        }
        
        // Check if dead
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    IEnumerator TriggerRespawn()
    {
        // Esperar a que termine la animación de muerte
        yield return new WaitForSeconds(1.5f);
        
        // Reiniciar nivel utilizando el LevelManager
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.RestartLevel();
        }
        else
        {
            Debug.LogWarning("No hay LevelManager en la escena. El jugador no se reiniciará automáticamente.");
            // Como no hay LevelManager, intentar reiniciar solo el jugador
            RestoreFullHealth();
            transform.position = initialPosition;
            transform.rotation = initialRotation;
        }
    }
    
    // Implementación de IRestartable
    public void OnLevelRestart()
    {
        // Restaurar estado inicial
        isDead = false;
        isInvincible = false;
        currentHealth = initialHealth;
        
        // Actualizar UI
        UpdateHealthUI();
        
        // Asegurarse de que el sprite es visible
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }
        
        // Detener todas las corrutinas en curso
        StopAllCoroutines();
    }
    
    // Métodos públicos para obtener información sobre la salud
    
    /// <summary>
    /// Obtiene el porcentaje de salud actual (0-100)
    /// </summary>
    public float GetHealthPercentage()
    {
        return (float)currentHealth / maxHealth * 100f;
    }
    
    /// <summary>
    /// Comprueba si el jugador tiene poca salud
    /// </summary>
    /// <param name="thresholdPercentage">Umbral para considerar 'poca salud' (por defecto 30%)</param>
    public bool HasLowHealth(float thresholdPercentage = 30f)
    {
        return GetHealthPercentage() <= thresholdPercentage;
    }
}