using UnityEngine;
using UnityEngine.Events;

public class HealthComponent : MonoBehaviour
{

    [Header("Configuracion de Vida")]
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;


    void Update()
    {
        Debug.Log($"Current Health: {currentHealth}");
    }
    // el onDeath Es para usar los eventos de muerte en caso de un boss una animacion de muerte especifica o coass asi, tambien podriamos
    // reproducir algun sonido especifico al momento de morir.
    public UnityEvent onDeath;
    public UnityEvent onDamage;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    // Update is called once per frame
    public void takeDamage(int amount)
    {
        currentHealth -= amount;

        onDamage?.Invoke();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);

    }

    private void Die()
    {
        onDeath?.Invoke();
        Destroy(gameObject);
    }
    
}
