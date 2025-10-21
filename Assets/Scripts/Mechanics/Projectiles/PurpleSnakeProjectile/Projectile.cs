using UnityEngine;

public class projectile : MonoBehaviour
{
    private int damage = 10;
    [SerializeField] private float lifeTime = 10f;
    [SerializeField] private LayerMask hitLayers; // opcional, filtrar qué puede golpear

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    public void SetDamage(int d)
    {
        damage = d;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // opcional: filtrar por capas
        if (hitLayers != (hitLayers | (1 << other.gameObject.layer)))
        {
            // la capa del "other" no está en hitLayers (si hitLayers == 0 ignora este if)
        }

        HealthComponent h = other.GetComponent<HealthComponent>();
        if (h != null)
        {
            h.takeDamage(damage);
        }

        // destruir en impacto
        Destroy(gameObject);
    }
    

}
