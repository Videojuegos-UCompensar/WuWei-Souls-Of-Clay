using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class HomingOrb : MonoBehaviour
{
    private Transform target;
    private Rigidbody2D rb;

    public GameObject owner;

    public float speed = 6f;
    public float rotateSpeed = 200f;
    public int damage = 1;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void SetTarget(Transform t)
    {
        target = t;
    }

    void FixedUpdate()
    {
        if (target == null) return;

        Vector2 direction = (Vector2)target.position - rb.position;
        direction.Normalize();

        float rotateAmount = Vector3.Cross(direction, transform.up).z;

        rb.angularVelocity = -rotateAmount * rotateSpeed;
        rb.velocity = transform.up * speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
{
    // 🔥 Ignorar al owner
    if (owner != null && other.gameObject == owner)
        return;

    HealthComponent hp = other.GetComponent<HealthComponent>();
    if (hp != null)
    {
        hp.TakeDamage(damage, owner);
        Destroy(gameObject);
    }
}

}
