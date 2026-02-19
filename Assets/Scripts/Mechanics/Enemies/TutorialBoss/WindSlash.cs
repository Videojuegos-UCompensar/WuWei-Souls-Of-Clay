using UnityEngine;

public class WindSlash : MonoBehaviour
{
    public GameObject owner;
    public float speed = 12f;
    public float lifeTime = 3f;
    public int damage = 1;

    private Vector2 direction;

    public void SetDirection(Vector2 dir)
    {
        direction = dir.normalized;
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HealthComponent hp = other.GetComponent<HealthComponent>();
        if (hp != null)
        {
            hp.TakeDamage(damage, owner);
            Destroy(gameObject);
        }
    }
}
