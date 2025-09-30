using UnityEngine;
using UnityEngine.InputSystem;

public class plat_atravesable : MonoBehaviour
{
    private BoxCollider2D platformCollider;
    private GameObject player;
    private Collider2D playerCollider;
    private Rigidbody2D playerRb;
    private float platformTop;
    
    // Layer masks for collision checking
    private int enemyLayer;

    void Start()
    {
        // Obtener referencias necesarias
        platformCollider = GetComponent<BoxCollider2D>();
        player = GameObject.FindGameObjectWithTag("Player");
        
        if (player != null)
        {
            playerCollider = player.GetComponent<Collider2D>();
            playerRb = player.GetComponent<Rigidbody2D>();
        }

        // Calcular la parte superior de la plataforma
        platformTop = platformCollider.bounds.max.y;
        
        // Cache layer information
        enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer == -1)
        {
            Debug.LogWarning("'Enemy' layer not found. Make sure to create this layer in Unity's Layer settings.");
        }
    }

    void Update()
    {
        if (player == null) return;

        float playerBottom = playerCollider.bounds.min.y;
        
        // Verificar si el jugador está sobre la plataforma
        bool playerAbove = playerBottom >= platformTop - 0.05f;

        // Verificar input para bajar (tecla S o flecha abajo)
        bool pressingDown = Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed;

        // Gestionar la colisión de la plataforma para el jugador
        if (playerAbove && !pressingDown)
        {
            // Solo habilite la colisión para el jugador cuando está encima
            Physics2D.IgnoreCollision(platformCollider, playerCollider, false);
        }
        else if (pressingDown && playerAbove)
        {
            // Permitir al jugador caer a través de la plataforma
            Physics2D.IgnoreCollision(platformCollider, playerCollider, true);
            // Aplicar una pequeña fuerza hacia abajo para iniciar el descenso
            playerRb.velocity = new Vector2(playerRb.velocity.x, -1f);
        }
        else if (playerCollider.bounds.max.y < platformTop - 0.1f)
        {
            // Si el jugador está debajo de la plataforma, ignorar colisiones
            Physics2D.IgnoreCollision(platformCollider, playerCollider, true);
        }
    }

    // Este método se ejecuta cuando un collider entra en contacto con el trigger
    void OnTriggerStay2D(Collider2D other)
    {
        ManageCollision(other);
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        ManageCollision(other);
    }
    
    void OnCollisionStay2D(Collision2D collision) 
    {
        if (collision.gameObject.CompareTag("Player") && 
            (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed))
        {
            Physics2D.IgnoreCollision(platformCollider, collision.collider, true);
        }
    }

    private void ManageCollision(Collider2D other)
    {
        // Manejar colisiones para enemigos
        if (other.CompareTag("Enemy"))
        {
            float enemyBottom = other.bounds.min.y;
            float platformTopPosition = platformCollider.bounds.max.y;
            
            // Si el enemigo está encima de la plataforma, permitir que se pare en ella
            if (enemyBottom >= platformTopPosition - 0.05f)
            {
                Physics2D.IgnoreCollision(platformCollider, other, false);
            }
            else if (other.bounds.max.y < platformTopPosition - 0.1f)
            {
                // Si está debajo, ignorar la colisión
                Physics2D.IgnoreCollision(platformCollider, other, true);
            }
        }
        
        // Mantener la lógica del jugador
        if (other.CompareTag("Player"))
        {
            float playerBottom = other.bounds.min.y;
            bool pressingDown = Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed;
            
            if (playerBottom >= platformTop - 0.05f && !pressingDown)
            {
                Physics2D.IgnoreCollision(platformCollider, other, false);
            }
        }
    }
}