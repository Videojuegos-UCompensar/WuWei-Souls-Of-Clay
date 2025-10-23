using UnityEngine;

/// <summary>
/// Ataque a distancia: instancia proyectiles hacia el target.
/// Maneja su propio fireRate; también usa damage y cooldown.
/// </summary>
public class RangedAttackComponent : AttackComponent
{
    [Header("Ranged settings")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;             // desde donde salen los proyectiles
    [SerializeField] private float projectileSpeed = 0.05f;
    [SerializeField] private float fireRate = 1f;             // disparos por segundo

    private float nextFireTime = -999f;

    /// <summary>
    /// Comprueba si el componente está listo para disparar según su propio fireRate y cooldown.
    /// </summary>
    public bool IsReadyToFire()
    {
        return Time.time >= nextFireTime && CanAttack();
    }

    public override bool TryAttack()
    {
        if (target == null) return false;
        if (Time.time < nextFireTime) return false;

        // optional: usar CanAttack() para bloquear con attackCooldown también
        if (!CanAttack()) return false;

        nextFireTime = Time.time + 1f / Mathf.Max(0.0001f, fireRate); // evitar división por cero
        lastAttackTime = Time.time;

        if (projectilePrefab == null || firePoint == null)
        {
            Debug.LogWarning($"{name}: Projectile prefab o FirePoint no asignados.");
            return false;
        }

        Vector2 dir = ((Vector2)target.position - (Vector2)firePoint.position).normalized;
        GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);

        Rigidbody2D rb = proj.GetComponent<Rigidbody2D>();
        if (rb != null) rb.velocity = dir * projectileSpeed;

        // si el projectile tiene script Projectile, pasarle el daño
        projectile projectileScript = proj.GetComponent<projectile>();
        if (projectileScript != null)
        {
            projectileScript.SetDamage(damage);
            try { projectileScript.SetOwner(this.gameObject); } catch { }
        }
        

        Debug.Log($"{gameObject.name} disparó un proyectil a {target.name}");
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        if (firePoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(firePoint.position, (Vector3)firePoint.position + Vector3.right * 0.5f);
        }
    }
}
