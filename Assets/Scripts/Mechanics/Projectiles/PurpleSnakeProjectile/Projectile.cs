using UnityEngine;

public class projectile : MonoBehaviour
{
    // Damage is assigned by the AttackComponent (e.g. RangedAttackComponent) via SetDamage.
    // Default to 0 to avoid duplicate/unexpected damage if SetDamage is not called.
    private int damage = 0;
    [SerializeField] private float lifeTime = 10f;
    [SerializeField] private LayerMask hitLayers; // opcional, filtrar qué puede golpear
    private GameObject owner;
    [SerializeField] private ProjectileData defaultConfig;
    private ProjectileData config;
    private Collider2D col;
    [SerializeField] private bool debugProjectile = false;
    private bool hasHit = false;

    private void Start()
    {
        col = GetComponent<Collider2D>();

        // Si SetOwner fue llamado antes de Start, aplicar IgnoreCollision ahora
        if (owner != null && col != null)
        {
            ApplyIgnoreWithOwner();
        }

        // Si no se pasó config en tiempo de instancia, usar el default asignado en el inspector
        if (config == null && defaultConfig != null)
        {
            config = defaultConfig;
            if (debugProjectile) Debug.Log($"{name}: using defaultConfig assigned on prefab");
        }

        // Si aún no tenemos damage explícito (SetDamage), usar el baseDamage del config si existe.
        if (damage == 0 && config != null)
        {
            damage = config.baseDamage;
            if (debugProjectile) Debug.Log($"{name}: damage taken from config = {damage}");
        }

        Destroy(gameObject, lifeTime);
    }

    public void SetDamage(int d)
    {
        damage = d;
    }

    public void SetConfig(ProjectileData cfg)
    {
        config = cfg;
        if (config == null) return;
        if (config.lifeTime > 0) lifeTime = config.lifeTime;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return; // evitar doble impacto por múltiples colliders o múltiples llamadas

        if (debugProjectile) Debug.Log($"{name}.OnTriggerEnter2D: other={other.name}, owner={(owner!=null?owner.name:"null")}, otherLayer={other.gameObject.layer}");

        // Si hitLayers está configurado (no cero), ignorar colisiones con capas fuera del mask
        if (hitLayers.value != 0)
        {
            int otherLayer = 1 << other.gameObject.layer;
            if ((hitLayers.value & otherLayer) == 0)
            {
                if (debugProjectile) Debug.Log($"{name}: other {other.name} layer not in hitLayers");
                return; // NO marcar hasHit: queremos seguir golpeando otros objetos
            }
        }

        // Evitar colisionar con el owner (si no pudimos ignorar las colisiones por alguna razón)
        if (owner != null && (other.transform.IsChildOf(owner.transform) || other.gameObject == owner))
        {
            if (debugProjectile) Debug.Log($"{name}: ignored collision with owner {other.name}");
            return; // NO marcar hasHit
        }

        // Buscar HealthComponent o PlayerHealth en el objeto o en sus padres (más robusto)
        HealthComponent h = other.GetComponentInParent<HealthComponent>();
        if (h == null)
        {
            h = other.GetComponent<HealthComponent>();
        }

        if (h != null)
        {
            // Marcar como impactado y desactivar collider para evitar impactos duplicados
            hasHit = true;
            if (col != null) col.enabled = false;
            // Debug: quien fue el owner y cuanto daño aplicamos
            try { Debug.Log($"[projectile] {name} hit {h.gameObject.name} owner={(owner!=null?owner.name:"null")} damage={damage}"); } catch { }
            h.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

    }

    /// <summary>
    /// Asigna el owner del proyectil y evita colisiones con sus colliders.
    /// </summary>
    public void SetOwner(GameObject o)
    {
        owner = o;
        // Si el collider del proyectil aún no existe (Start no corrido), ApplyIgnoreWithOwner lo hará en Start
        if (owner == null) return;

        if (col != null)
        {
            ApplyIgnoreWithOwner();
        }
    }

    // Aplica el IgnoreCollision entre el collider actual y todos los colliders del owner
    private void ApplyIgnoreWithOwner()
    {
        if (owner == null || col == null) return;

        Collider2D[] ownerCols = owner.GetComponentsInChildren<Collider2D>(true);
        foreach (var oc in ownerCols)
        {
            if (oc == null) continue;
            Physics2D.IgnoreCollision(col, oc, true);
            if (debugProjectile) Debug.Log($"{name}: Ignoring collision between projectile and owner collider {oc.name}");
        }
    }

    /// <summary>
    /// Si por alguna razón necesitas volver a habilitar colisiones entre el owner y este proyectil,
    /// llama a este método. Usar con precaución.
    /// </summary>
    public void ClearOwnerIgnore()
    {
        if (owner == null || col == null) return;
        Collider2D[] ownerCols = owner.GetComponentsInChildren<Collider2D>(true);
        foreach (var oc in ownerCols)
        {
            if (oc == null) continue;
            Physics2D.IgnoreCollision(col, oc, false);
        }
    }
    

}
