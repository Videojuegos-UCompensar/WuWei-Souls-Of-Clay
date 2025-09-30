using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FallingPlatform : MonoBehaviour, IRestartable
{
    [Header("Configuración")]
    public float fallDelay = 1.0f;       // Tiempo antes de empezar a caer
    public float initialShakeTime = 0.5f; // Tiempo de agitación antes de caer
    public float shakeAmount = 0.1f;     // Cantidad de agitación
    public float fallSpeed = 9.8f;       // Velocidad de caída (aceleración)
    public float maxFallSpeed = 15f;     // Velocidad máxima de caída
    public bool deactivateOnFall = true; // Desactivar la plataforma después de caer
    public float deactivateDelay = 2.0f; // Tiempo antes de desactivar

    [Header("Detección del Jugador")]
    public bool useTrigger = false;      // Usar OnTriggerEnter o OnCollisionEnter
    public string playerTag = "Player";  // Tag del jugador a detectar

    [Header("Efectos")]
    public bool useShakeEffect = true;   // Usar efecto de agitación
    public bool useFallEffect = true;    // Usar efecto al caer
    public GameObject fallEffectPrefab;  // Prefab con efecto visual (opcional)
    public AudioClip shakeSound;         // Sonido de agitación (opcional)
    public AudioClip fallSound;          // Sonido al caer (opcional)

    // Variables privadas
    private Vector3 initialPosition;     // Posición inicial
    private Quaternion initialRotation;  // Rotación inicial
    private bool isFalling = false;      // Si está cayendo
    private bool isShaking = false;      // Si está agitándose
    private bool hasBeenActivated = false; // Si ya ha sido activada
    private Rigidbody2D rb;              // Componente Rigidbody2D
    private AudioSource audioSource;     // Componente AudioSource
    private Collider2D platformCollider; // Collider de la plataforma
    private Vector3 originalPosition;    // Posición original para la agitación

    void Awake()
    {
        // Guardar posición y rotación inicial
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        
        // Obtener componentes
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();
        platformCollider = GetComponent<Collider2D>();
        
        // Asegurarse de que tiene Rigidbody2D
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        
        // Configurar el Rigidbody2D correctamente
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true; // Importante para detectar colisiones en modo Kinematic
        
        // Verificar/configurar el collider
        if (platformCollider == null)
        {
            // Añadir un BoxCollider2D si no existe
            platformCollider = gameObject.AddComponent<BoxCollider2D>();
        }
        
        // Configurar el collider según el modo de detección
        platformCollider.isTrigger = useTrigger;
        
        // Asegurar que tiene AudioSource si se usan sonidos
        if (audioSource == null && (shakeSound != null || fallSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Log para depuración
        Debug.Log($"Plataforma inicializada: {gameObject.name}, useTrigger: {useTrigger}, Collider: {platformCollider.GetType().Name}");
    }

    void Start()
    {
        // Inicializar en estado no caído
        ResetPlatform();
    }

    // Detección con Trigger (OnTriggerEnter2D)
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!useTrigger) return; // No usar si no está configurado para triggers
        
        Debug.Log($"Trigger detectado: {other.gameObject.name}, tag: {other.tag}");
        
        // Solo activar si es el jugador y si la plataforma no está cayendo o agitándose
        if (other.CompareTag(playerTag) && !isFalling && !isShaking && !hasBeenActivated)
        {
            Debug.Log($"Plataforma activada por trigger: {gameObject.name}");
            originalPosition = transform.position; // Guardar posición para la agitación
            StartCoroutine(FallSequence());
        }
    }
    
    // Detección con Collider normal (OnCollisionEnter2D)
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (useTrigger) return; // No usar si está configurado para triggers
        
        Debug.Log($"Colisión detectada: {collision.gameObject.name}, tag: {collision.gameObject.tag}");
        
        // Solo activar si es el jugador y si la plataforma no está cayendo o agitándose
        // Y solo si el contacto viene desde arriba
        if (collision.gameObject.CompareTag(playerTag) && !isFalling && !isShaking && !hasBeenActivated)
        {
            // Verificar si el jugador está encima (contacto desde arriba)
            foreach (ContactPoint2D contact in collision.contacts)
            {
                // Si el punto de contacto tiene dirección hacia abajo (desde el punto de vista de la plataforma)
                if (contact.normal.y < -0.5f)
                {
                    Debug.Log($"Plataforma activada por colisión: {gameObject.name}");
                    originalPosition = transform.position; // Guardar posición para la agitación
                    StartCoroutine(FallSequence());
                    break;
                }
            }
        }
    }

    // Secuencia de caída de la plataforma
    private IEnumerator FallSequence()
    {
        hasBeenActivated = true;
        
        // Fase de agitación
        if (useShakeEffect)
        {
            isShaking = true;
            
            // Reproducir sonido de agitación
            if (audioSource != null && shakeSound != null)
            {
                audioSource.PlayOneShot(shakeSound);
            }
            
            // Efecto de agitación
            float elapsed = 0;
            while (elapsed < initialShakeTime)
            {
                transform.position = originalPosition + Random.insideUnitSphere * shakeAmount;
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Restaurar posición después de agitarse
            transform.position = originalPosition;
            isShaking = false;
        }
        
        // Esperar antes de caer
        yield return new WaitForSeconds(fallDelay);
        
        // Empezar a caer
        isFalling = true;
        
        // Cambiar el Rigidbody2D a dinámico para que caiga
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.mass = 1.0f;
        rb.gravityScale = fallSpeed / 9.8f; // Ajustar gravedad según la velocidad deseada
        rb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX; // Congelar rotación y movimiento horizontal
        
        // Reproducir sonido de caída
        if (audioSource != null && fallSound != null)
        {
            audioSource.PlayOneShot(fallSound);
        }
        
        // Instanciar efecto visual
        if (useFallEffect && fallEffectPrefab != null)
        {
            Instantiate(fallEffectPrefab, transform.position, Quaternion.identity);
        }
        
        Debug.Log($"Plataforma cayendo: {gameObject.name}");
        
        // Si está configurado para desactivarse después de caer
        if (deactivateOnFall)
        {
            yield return new WaitForSeconds(deactivateDelay);
            gameObject.SetActive(false);
        }
    }
    
    // Método para limitar la velocidad máxima de caída
    void FixedUpdate()
    {
        if (isFalling && rb != null)
        {
            // Limitar velocidad de caída
            if (rb.velocity.magnitude > maxFallSpeed)
            {
                rb.velocity = rb.velocity.normalized * maxFallSpeed;
            }
        }
    }
    
    // Implementación de IRestartable
    public void OnLevelRestart()
    {
        ResetPlatform();
    }
    
    // Método para reiniciar la plataforma
    private void ResetPlatform()
    {
        // Detener todas las corrutinas
        StopAllCoroutines();
        
        // Reactivar el objeto si estaba desactivado
        gameObject.SetActive(true);
        
        // Reiniciar estados
        isFalling = false;
        isShaking = false;
        hasBeenActivated = false;
        
        // Restaurar posición y rotación
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        
        // Importante: Reiniciar el Rigidbody2D
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f; // Quitar la gravedad
            rb.Sleep(); // Poner el cuerpo a dormir
        }
        
        // Asegurarse de que el collider está activo
        if (platformCollider != null)
        {
            platformCollider.enabled = true;
            platformCollider.isTrigger = useTrigger; // Reestablecer el estado del trigger
        }
        
        Debug.Log($"Plataforma reiniciada: {gameObject.name}");
    }
    
    // Para visualización en el editor
    void OnDrawGizmos()
    {
        Gizmos.color = isFalling ? Color.red : (isShaking ? Color.yellow : Color.green);
        Gizmos.DrawWireCube(transform.position, GetComponent<Renderer>() ? 
            GetComponent<Renderer>().bounds.size : new Vector3(1f, 0.2f, 1f));
    }
}