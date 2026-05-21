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

    [SerializeField] private ParticleSystem superHit;

    public int currentHealth { get; private set; }

    [Header("Eventos")]
    public Action onDeath;
    public UnityEvent onDamage; // legacy no-param event
    // New typed events useful for UI / listeners that need the amount
    public UnityEvent<int> OnDamaged;
    public UnityEvent<int> OnHealed;
    // If your project already provides a CameraShake component on the main
    // camera, we'll call it. We don't add any new files here.
    private CameraShake cameraShake;

    public Action<HealthComponent, float, float> OnHealthChanged;


    private void Awake()
    {
        currentHealth = maxHealth;

        if (Camera.main != null)
        {
            cameraShake = Camera.main.GetComponent<CameraShake>();
        }
        // ...
    }

    private void Start()
    {
       OnHealthChanged?.Invoke(this, currentHealth, maxHealth);
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

        if (damageSource == gameObject)
            return;

        // ...

        currentHealth -= amount;

        onDamage?.Invoke();
        OnDamaged?.Invoke(amount);

        OnHealthChanged?.Invoke(this, currentHealth, maxHealth);

        // Trigger camera shake if available (use project's implementation)
        try
        {
            if (cameraShake != null)
            {
                cameraShake?.Shake(0.15f);
            }
        }
        catch { }

        // ...

        lastDamageSource = damageSource;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        OnHealthChanged?.Invoke(this, currentHealth, maxHealth);
        try { OnHealed?.Invoke(amount); } catch { }
    }

    /// <summary>
    /// Restore the health to max and reactivate common components. This
    /// mirrors the behaviour previously provided by PlayerHealth.RestoreFullHealth().
    /// </summary>
    public void RestoreFullHealth()
{
    lastDamageSource = null;
    currentHealth = maxHealth;

    if (!gameObject.activeSelf)
        gameObject.SetActive(true);

    OnHealthChanged?.Invoke(this, currentHealth, maxHealth);

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

    // Solo manipula si la partícula es una instancia en la escena (no un prefab asset)
    if (superHit != null && superHit.gameObject.scene.IsValid())
    {
        superHit.transform.SetParent(null); // Saca la partícula del objeto
        superHit.gameObject.SetActive(true); // Asegura que esté activa
        superHit.Play();
        Destroy(superHit.gameObject, superHit.main.duration); // Destruye la partícula después de reproducirse
    }
    // ...

    onDeath?.Invoke();
    gameObject.SetActive(false);
}

public int GetCurrentHealth()
{
    return currentHealth;
}

public int GetMaxHealth()
{
    return maxHealth;
}

public void PlayHitEffect()
    {
        if (superHit != null)
        {
            if (!superHit.gameObject.activeInHierarchy)
            {
                superHit.gameObject.SetActive(true);
            }
            superHit.Play();
        }
    }

}


