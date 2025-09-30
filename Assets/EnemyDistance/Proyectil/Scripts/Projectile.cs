using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private float lifeTime = 5f;
    [SerializeField] private GameObject impactEffect;
    [SerializeField] private LayerMask targetLayers;
    [SerializeField] private bool rotateToDirection = true;
    [SerializeField] private float rotationOffset = 0f; // Useful if sprite is not oriented correctly
    [SerializeField] private bool fadeOutBeforeDestroy = true;
    [SerializeField] private float fadeOutDuration = 0.5f;
    
    // Private variables
    private int damage = 10;
    private bool hasHit = false;
    private float creationTime;
    
    // References
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private TrailRenderer trailRenderer;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        trailRenderer = GetComponent<TrailRenderer>();
        
        creationTime = Time.time;
        
        // Start lifetime countdown
        if (fadeOutBeforeDestroy)
        {
            StartCoroutine(DestroyAfterLifetime());
        }
        else
        {
            // Simple destroy after lifetime
            Destroy(gameObject, lifeTime);
        }
    }
    
    private void Start()
    {
        // Initial rotation based on velocity direction
        if (rotateToDirection && rb.velocity != Vector2.zero)
        {
            float angle = Mathf.Atan2(rb.velocity.y, rb.velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle + rotationOffset, Vector3.forward);
        }
    }
    
    // Method to set damage from EnemyController
    public void SetDamage(int newDamage)
    {
        damage = newDamage;
    }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Prevent multiple collisions
        if (hasHit) return;
        
        // Check if colliding with player
        if (collision.CompareTag("Player"))
        {
            hasHit = true;
            
            // Try to apply damage to player
            PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
            
            // Create impact effect if it exists
            CreateImpactEffect();
            
            // Destroy the projectile
            DestroyProjectile();
        }
        // Check if colliding with environment (walls, ground, etc.)
        else if (((1 << collision.gameObject.layer) & targetLayers) != 0)
        {
            hasHit = true;
            CreateImpactEffect();
            DestroyProjectile();
        }
    }
    
    private void CreateImpactEffect()
    {
        if (impactEffect != null)
        {
            Instantiate(impactEffect, transform.position, Quaternion.identity);
        }
    }
    
    // Rotate projectile based on movement direction
    private void Update()
    {
        // Only update rotation if we're set to do so and we have velocity
        if (rotateToDirection && !hasHit && rb.velocity != Vector2.zero)
        {
            float angle = Mathf.Atan2(rb.velocity.y, rb.velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle + rotationOffset, Vector3.forward);
        }
        
        // Check if we're about to reach lifetime and haven't started fading
        if (fadeOutBeforeDestroy && Time.time > creationTime + lifeTime - fadeOutDuration && !hasHit)
        {
            StartCoroutine(FadeOut());
            hasHit = true; // Prevent further collisions during fade out
        }
    }
    
    private IEnumerator DestroyAfterLifetime()
    {
        // Wait until it's time to start fading
        yield return new WaitForSeconds(lifeTime - fadeOutDuration);
        
        // If we haven't hit anything yet, fade out
        if (!hasHit)
        {
            yield return StartCoroutine(FadeOut());
        }
        
        // Destroy after fade out
        Destroy(gameObject);
    }
    
    private IEnumerator FadeOut()
    {
        // Disable collider during fade out
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }
        
        // Fade out sprite
        if (spriteRenderer != null)
        {
            Color startColor = spriteRenderer.color;
            Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0);
            
            // Fade out trail if it exists
            if (trailRenderer != null)
            {
                trailRenderer.time = fadeOutDuration;
            }
            
            float elapsedTime = 0;
            while (elapsedTime < fadeOutDuration)
            {
                elapsedTime += Time.deltaTime;
                float normalizedTime = elapsedTime / fadeOutDuration;
                
                spriteRenderer.color = Color.Lerp(startColor, endColor, normalizedTime);
                
                yield return null;
            }
        }
    }
    
    private void DestroyProjectile()
    {
        // Disable collider immediately
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }
        
        // Stop movement
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }
        
        // If we want to fade out
        if (fadeOutBeforeDestroy)
        {
            StartCoroutine(FadeOut());
            Destroy(gameObject, fadeOutDuration);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    // For visualization in the editor
    private void OnDrawGizmosSelected()
    {
        // Check that rb is not null before using it
        if (rb != null && rb.velocity != Vector2.zero)
        {
            // Visualize projectile direction
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, rb.velocity.normalized * 2f);
        }
        else
        {
            // If rb is null or velocity is zero, draw a simple gizmo
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
}