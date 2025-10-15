using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(Animator))]
public class SlugEnemy : MonoBehaviour
{
    [Header("Movimiento y salto")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float jumpForce = 20f; 
    [SerializeField] private float horizontalJumpForce = 8f; 
    [SerializeField] private float groundCheckDistance = 1f;

    [Header("Detección del jugador")]
    [SerializeField] private float minDetectionRange = 3f; 
    [SerializeField] private float maxDetectionRange = 6f; 
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Transform player;

    [Header("Suavizado de Movimiento")]
    [SerializeField] private float movementSmoothing = 5f; 

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator anim;

    private bool isGrounded = false;
    private bool isJumping = false;
    
    private float moveDir = 1f; 
    private float targetMoveDir = 0f; 
    private float currentMoveDir = 0f; 

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    private void Update()
    {
        if (player == null) return;

        CheckGrounded();
        HandleMovement();
    }

    private void CheckGrounded()
    {
        RaycastHit2D hit = Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, groundLayer);
        isGrounded = hit.collider != null;

        // Si ya no está saltando y está en el suelo, isJumping es falso.
        if (isGrounded && isJumping)
            isJumping = false;
    }

    private void HandleMovement()
    {
        // Si estamos saltando, el sprite se mantiene fijo y la lógica de movimiento se detiene.
        if (isJumping) return; 

        float distance = Vector2.Distance(transform.position, player.position);
        bool playerOnRight = player.position.x > transform.position.x;
        
        // 1. Determinar la Dirección Objetivo (targetMoveDir)
        if (distance <= maxDetectionRange && distance > minDetectionRange)
        {
            targetMoveDir = playerOnRight ? 0.5f : -0.5f;
            anim.SetBool("IsScared", false);
        }
        else if (distance <= minDetectionRange)
        {
            targetMoveDir = playerOnRight ? -1f : 1f;
            anim.SetBool("IsScared", true);
        }
        else
        {
            targetMoveDir = 0f;
            anim.SetBool("IsScared", false);
        }

        // 2. Suavizar la Dirección (Lerp)
        currentMoveDir = Mathf.Lerp(currentMoveDir, targetMoveDir, Time.deltaTime * movementSmoothing);
        moveDir = currentMoveDir; 

        // 3. Aplicar Movimiento, Volteo y Animación
        if (Mathf.Abs(moveDir) < 0.01f) 
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            anim.SetBool("IsMoving", false);
        }
        else
        {
            rb.velocity = new Vector2(moveDir * walkSpeed, rb.velocity.y);
            // LÓGICA DE VOLTEO INVERTIDA EN TIERRA: Mira la dirección de movimiento.
            sr.flipX = moveDir > 0; 
            anim.SetBool("IsMoving", true);
        }

        // 4. Lógica de Salto
        if (isGrounded)
        {
            if (Mathf.Abs(moveDir) > 0.1f && IsNearEdge())
            {
                // Dirección del salto (siempre hacia el jugador)
                float directionTowardPlayer = playerOnRight ? 1f : -1f;
                StartCoroutine(JumpTowardPlayer(directionTowardPlayer));
            }
        }
    }

    private bool IsNearEdge()
    {
        Vector2 origin = (Vector2)groundCheck.position + Vector2.right * moveDir * 0.5f;
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundLayer);
        Debug.DrawRay(origin, Vector2.down * groundCheckDistance, hit.collider ? Color.green : Color.red);
        return !hit.collider;
    }

    private IEnumerator JumpTowardPlayer(float directionTowardPlayer)
    {
        isJumping = true;

        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer >= 0)
            Physics2D.IgnoreLayerCollision(gameObject.layer, playerLayer, true);

        // Anular inercia horizontal para un arco limpio
        rb.velocity = new Vector2(0f, rb.velocity.y); 

        float horizontalSpeed = directionTowardPlayer * horizontalJumpForce;
        
        // Aplicamos el impulso inicial
        rb.velocity = new Vector2(horizontalSpeed, jumpForce);

        // LÓGICA DE VOLTEO INVERTIDA EN SALTO: Fija la orientación del sprite
        sr.flipX = directionTowardPlayer > 0; 

        yield return new WaitForSeconds(0.1f);

        // ⭐ CORRECCIÓN CLAVE: Dejamos que la física de Unity controle la trayectoria.
        while (!isGrounded)
        {
            // Solo mantenemos la velocidad horizontal constante y dejamos que rb.velocity.y sea gestionada por la gravedad.
            rb.velocity = new Vector2(horizontalSpeed, rb.velocity.y); 
            
            yield return null;
            anim.SetBool("IsJumping", !isGrounded);
        }

        // isJumping se hará false en CheckGrounded() en el siguiente frame.
        // No necesitamos voltear el sprite aquí; HandleMovement() se encarga.

        if (playerLayer >= 0)
            Physics2D.IgnoreLayerCollision(gameObject.layer, playerLayer, false);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(groundCheck.position, groundCheck.position + Vector3.down * groundCheckDistance);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, minDetectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, maxDetectionRange);
    }
}