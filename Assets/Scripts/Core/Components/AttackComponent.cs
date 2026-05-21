using UnityEngine;

/// <summary>
/// Clase base para ataques. No tiene comportamiento concreto: obliga a las subclases a implementar TryAttack().
/// Maneja la lógica común: target, cooldown y daño base.
/// </summary>
public abstract class AttackComponent : MonoBehaviour
{
    [Header("Configuración base de ataque")]
    [SerializeField] protected int damage = 10;                // daño que aplican los ataques
    [SerializeField] protected float attackCooldown = 1f;      // cooldown mínimo entre ataques

    protected float lastAttackTime = -999f;                    // tiempo del último ataque
    protected Transform target;                                // objetivo (por ejemplo el player)

    // Exponer el daño de forma segura para que otros objetos (por ejemplo proyectiles)
    // puedan leer el valor si es necesario sin romper encapsulación.
    public int Damage => damage;

    /// <summary> Asigna el objetivo (por ejemplo desde SlugEnemy). </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    /// <summary> Comprueba cooldown y objetivo. </summary>
    protected bool CanAttack()
    {
        return target != null && Time.time >= lastAttackTime + attackCooldown;
    }

    /// <summary>
    /// Permite a scripts externos (ej: AI) comprobar si el componente está listo para realizar un ataque.
    /// </summary>
    public bool IsReadyToAttack()
    {
        return CanAttack();
    }

    /// <summary>
    /// Método que debe implementar cada subclase para ejecutar su tipo de ataque.
    /// (Devuelve true si se realizó el ataque; opcional)
    /// </summary>
    public abstract bool TryAttack();
}
