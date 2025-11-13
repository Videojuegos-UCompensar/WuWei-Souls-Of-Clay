using UnityEngine;

public class projectile : MonoBehaviour
{
    private int damage = 10;
    [SerializeField] private float lifeTime = 10f;
    [SerializeField] private LayerMask hitLayers; // opcional, filtrar qué puede golpear
    private GameObject owner;
    private Collider2D col;
    [SerializeField] private bool debugProjectile = false;

    private void Start()
    {
        col = GetComponent<Collider2D>();

        // Si SetOwner fue llamado antes de Start, aplicar IgnoreCollision ahora
        if (owner != null && col != null)
        {
            ApplyIgnoreWithOwner();
        }

        Destroy(gameObject, lifeTime);
    }

    public void SetDamage(int d)
    {
        damage = d;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (debugProjectile) Debug.Log($"{name}.OnTriggerEnter2D: other={other.name}, owner={(owner!=null?owner.name:"null")}");

    // Si hitLayers está configurado (no cero), ignorar colisiones con capas fuera del mask
        if (hitLayers.value != 0)
        {
            int otherLayer = 1 << other.gameObject.layer;
            if ((hitLayers.value & otherLayer) == 0)
            {
                if (debugProjectile) Debug.Log($"{name}: other {other.name} layer not in hitLayers");
                return;
            }
        }

        // Evitar colisionar con el owner (si no pudimos ignorar las colisiones por alguna razón)
        if (owner != null && (other.transform.IsChildOf(owner.transform) || other.gameObject == owner))
        {
            if (debugProjectile) Debug.Log($"{name}: ignored collision with owner {other.name}");
            return;
        }

        HealthComponent h = other.GetComponent<HealthComponent>();
        if (h != null)
        {
            h.takeDamage(damage);
        }
        else
        {
            var ph = other.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(damage);
            }
            else if (debugProjectile)
            {
                Debug.Log($"{name}: hit {other.name} but no HealthComponent or PlayerHealth found");
            }
        }

        // destruir en impacto
        Destroy(gameObject);
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
