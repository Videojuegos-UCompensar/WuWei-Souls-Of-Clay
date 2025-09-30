using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathZone : MonoBehaviour
{
    [Header("Configuración")]
    public bool instantKill = true;       // Mata instantáneamente al jugador
    public int damage = 999;              // Daño aplicado si no es muerte instantánea
    public bool restartLevelOnTouch = true; // Reiniciar nivel al tocar esta zona
    
    [Header("Efectos")]
    public bool useDeathEffect = true;    // Usar efecto visual al morir
    public GameObject deathEffectPrefab;  // Prefab con efecto visual (opcional)
    public AudioClip deathSound;          // Sonido al morir (opcional)
    
    private AudioSource audioSource;
    
    private void Start()
    {
        // Asegurarse de que tiene un Collider2D y está configurado como trigger
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null && !collider.isTrigger)
        {
            Debug.LogWarning("DeathZone debería usar un Collider2D configurado como Trigger");
            collider.isTrigger = true;
        }
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && deathSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Verificar si es el jugador
        if (other.CompareTag("Player"))
        {
            // Encontrar componente de salud
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            
            // Reproducir sonido si existe
            if (audioSource != null && deathSound != null)
            {
                audioSource.PlayOneShot(deathSound);
            }
            
            // Instanciar efecto visual si existe
            if (useDeathEffect && deathEffectPrefab != null)
            {
                Instantiate(deathEffectPrefab, other.transform.position, Quaternion.identity);
            }
            
            // Aplicar daño si el jugador tiene componente de salud
            if (playerHealth != null)
            {
                if (instantKill)
                {
                    playerHealth.Die();
                }
                else
                {
                    playerHealth.TakeDamage(damage);
                }
            }
            else
            {
                // Si no tiene componente de salud, simplemente desactivar el objeto
                Debug.LogWarning("El jugador no tiene componente PlayerHealth. Desactivando objeto.");
                other.gameObject.SetActive(false);
            }
            
            // Reiniciar nivel si está configurado así
            if (restartLevelOnTouch)
            {
                // Verificar si existe el LevelManager
                if (LevelManager.Instance != null)
                {
                    LevelManager.Instance.RestartLevel();
                }
                else
                {
                    Debug.LogError("No se encontró LevelManager en la escena. Necesario para reiniciar el nivel.");
                }
            }
        }
    }
    
    // Para visualizar la zona de muerte en el editor
    private void OnDrawGizmos()
    {
        // Dibujar un contorno rojo para visualizar la zona de muerte
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f); // Rojo semi-transparente
        
        // Obtener la forma del collider
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            // Dibujar según el tipo de collider
            if (collider is BoxCollider2D)
            {
                BoxCollider2D boxCollider = collider as BoxCollider2D;
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(boxCollider.offset, boxCollider.size);
            }
            else if (collider is CircleCollider2D)
            {
                CircleCollider2D circleCollider = collider as CircleCollider2D;
                Gizmos.DrawSphere(transform.position + (Vector3)circleCollider.offset, circleCollider.radius);
            }
            // Puedes agregar más tipos de colliders según sea necesario
        }
        else
        {
            // Si no hay collider, dibujar un cubo simple
            Gizmos.DrawCube(transform.position, Vector3.one);
        }
    }
}