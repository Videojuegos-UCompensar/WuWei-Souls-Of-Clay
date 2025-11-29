using UnityEngine;

/// <summary>
/// Componente de ataque a distancia que instancia proyectiles hacia el <see cref="target"/>.
/// Esta clase delega la gestión del cooldown a la clase base <see cref="AttackComponent"/> mediante
/// <see cref="CanAttack"/> y <c>lastAttackTime</c> para evitar duplicar la lógica de cadencia.
/// </summary>
public class RangedAttackComponent : AttackComponent
{
    [Header("Ranged settings")]
    [Tooltip("Prefab del proyectil que se instanciará.")]
    [SerializeField] private GameObject projectilePrefab;

    [Tooltip("Punto desde el que salen los proyectiles.")]
    [SerializeField] private Transform firePoint;

    [Tooltip("Datos configurables del proyectil (obligatorio para controlar velocidad).")]
    [SerializeField] private ProjectileData projectileData;

    /// <summary>
    /// Devuelve true si el componente está listo para disparar según la lógica de la clase base.
    /// </summary>
    public bool IsReadyToFire()
    {
        return CanAttack();
    }

    /// <summary>
    /// Intenta realizar un disparo. Devuelve true si el proyectil fue creado correctamente.
    /// </summary>
    public override bool TryAttack()
    {
        if (target == null) return false;
        if (!CanAttack()) return false; // respeta cooldown y restricciones de la clase base

        if (projectilePrefab == null || firePoint == null)
        {
            Debug.LogWarning($"{name}: projectilePrefab o firePoint no asignados.");
            return false;
        }

        // marcar el tiempo de ataque para la lógica de cooldown de la base
        lastAttackTime = Time.time;

        Vector2 dir = ((Vector2)target.position - (Vector2)firePoint.position).normalized;
        GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        if (proj == null) return false;

        // asignar velocidad al rigidbody si existe (usamos ProjectileData.speed)
        Rigidbody2D rb = proj.GetComponent<Rigidbody2D>();
        float sp = (projectileData != null && projectileData.speed > 0f) ? projectileData.speed : 1f;
        if (projectileData == null) Debug.LogWarning($"{name}: projectileData no asignado, usando fallback speed={sp}.");
        if (rb != null) rb.velocity = dir * sp;

        // transferir configuración y daño al script del proyectil si existe
        projectile projectileScript = proj.GetComponent<projectile>();
        if (projectileScript != null)
        {
            try { projectileScript.SetConfig(projectileData); } catch { }

            int finalDamage = damage;
            if (projectileData != null) finalDamage += projectileData.baseDamage;
            projectileScript.SetDamage(finalDamage);
            try { projectileScript.SetOwner(this.gameObject); } catch { }
        }

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        if (firePoint == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(firePoint.position, (Vector3)firePoint.position + Vector3.right * 0.5f);
    }
}
