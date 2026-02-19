using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class HealthComponent : MonoBehaviour
	, IRestartable
{
    private GameObject lastDamageSource;
    public GameObject LastDamageSource => lastDamageSource;

    [Header("Configuración de Vida")]
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;

    [Header("Eventos")]
    public Action onDeath;
    public UnityEvent onDamage; // legacy no-param event
    // New typed events useful for UI / listeners that need the amount
    public UnityEvent<int> OnDamaged;
    public UnityEvent<int> OnHealed;
    // If your project already provides a CameraShake component on the main
    // camera, we'll call it. We don't add any new files here.
    private CameraShake cameraShake;

    public Action<float, float> OnHealthChanged;

    private void Awake()
    {
        currentHealth = maxHealth;

        if (Camera.main != null)
        {
            cameraShake = Camera.main.GetComponent<CameraShake>();
        }
        // Debug: mostrar si encontramos la cámara principal y el componente CameraShake
        try
        {
            Debug.Log($"[HealthComponent] Awake on {name}: Camera.main={(Camera.main!=null?Camera.main.name:"null")}, CameraShake={(cameraShake!=null?"found":"null")}");
        }
        catch { }
    }

    private void Start()
    {
       OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    // Optional invincibility support (off by default). Player adapter can
    // enable temporary invincibility using StartInvincibility(duration).
    private bool isInvincible = false;
    public bool IsInvincible => isInvincible;

    public void StartInvincibility(float duration)
    {
        if (duration <= 0f) return;
        StopCoroutine("InvincibilityCoroutine");
        StartCoroutine(InvincibilityCoroutine(duration));
    }

    private System.Collections.IEnumerator InvincibilityCoroutine(float duration)
    {
        isInvincible = true;
        yield return new WaitForSeconds(duration);
        isInvincible = false;
    }

    public void TakeDamage(int amount, GameObject damageSource = null)
    {
        if (amount <= 0 || currentHealth <= 0)
            return;

        int before = currentHealth;
        Debug.Log($"[HealthComponent] {name} TakeDamage: amount={amount}, before={before}");

        currentHealth -= amount;

        // 👇 AGREGA ESTO
        onDamage?.Invoke();
        OnDamaged?.Invoke(amount);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // Trigger camera shake if available (use project's implementation)
        try
        {
            if (cameraShake != null)
            {
                Debug.Log($"[HealthComponent] {name} invoking CameraShake.Shake()");
                cameraShake?.Shake(0.15f);
            }
            else
            {
                Debug.Log($"[HealthComponent] {name} CameraShake not found (no shake)");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[HealthComponent] {name} exception invoking CameraShake: {ex.Message}");
        }

        Debug.Log($"[HealthComponent] {name} after damage: current={currentHealth}/{maxHealth}");

        lastDamageSource = damageSource;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        try { OnHealed?.Invoke(amount); } catch { }
    }

    /// <summary>
    /// Restore the health to max and reactivate common components. This
    /// mirrors the behaviour previously provided by PlayerHealth.RestoreFullHealth().
    /// </summary>
    public void RestoreFullHealth()
{
    currentHealth = maxHealth;

    OnHealthChanged?.Invoke(currentHealth, maxHealth);

    Collider2D[] cols = GetComponents<Collider2D>();
    foreach (var c in cols) if (c != null) c.enabled = true;

    MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
    foreach (var s in scripts) if (s != null) s.enabled = true;
}


    /// <summary>
    /// Set health to a specific value (used by LevelManager or debug tools)
    /// </summary>
    public void SetHealth(int value)
    {
        currentHealth = Mathf.Clamp(value, 0, maxHealth);
        if (currentHealth <= 0) Die();
    }

    // IRestartable implementation: restore health when level restarts
    public void OnLevelRestart()
    {
        RestoreFullHealth();
    }


   private void Die()
{

    onDeath?.Invoke();
    gameObject.SetActive(false);
    
}

}


