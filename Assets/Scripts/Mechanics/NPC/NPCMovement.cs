using UnityEngine;

public class NPCFear : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidadHuida = 4f;
    public float aceleracion = 6f;

    [Header("Detección")]
    public float radioMiedo = 5f;

    public LayerMask enemigoLayer;
    public LayerMask sueloLayer;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float radioGroundCheck = 0.2f;

    [Header("Referencias")]
    public Rigidbody2D rb;
    public Animator animator;

    private Transform enemigoActual;

    private bool mirandoDerecha = true;

    // Dirección fija mientras huye
    private float direccionHuida = 0f;

    // Estado
    private bool huyendo = false;

    void Start()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (animator == null)
            animator = GetComponent<Animator>();

        rb.freezeRotation = true;
    }

    void Update()
    {
        DetectarEnemigo();

        if (huyendo)
        {
            animator.SetBool("Asustado", true);
            Huir();
        }
        else
        {
            animator.SetBool("Asustado", false);

            // Frenado suave
            rb.velocity = new Vector2(
                Mathf.Lerp(rb.velocity.x, 0f, Time.deltaTime * 8f),
                rb.velocity.y
            );
        }
    }

    void DetectarEnemigo()
    {
        Collider2D enemigo = Physics2D.OverlapCircle(
            transform.position,
            radioMiedo,
            enemigoLayer
        );

        // Detectó enemigo NUEVO
        if (enemigo != null && !huyendo)
        {
            enemigoActual = enemigo.transform;

            // Decide dirección UNA sola vez
            if (enemigoActual.position.x > transform.position.x)
                direccionHuida = -1f;
            else
                direccionHuida = 1f;

            huyendo = true;
        }

        // Ya no hay enemigo
        if (enemigo == null)
        {
            huyendo = false;
            enemigoActual = null;
        }
    }

    void Huir()
    {
        // Girar sprite visualmente
        if (direccionHuida > 0 && !mirandoDerecha)
            Girar();

        else if (direccionHuida < 0 && mirandoDerecha)
            Girar();

        // Detectar suelo adelante
        bool haySuelo = Physics2D.OverlapCircle(
            groundCheck.position,
            radioGroundCheck,
            sueloLayer
        );

        // Si no hay suelo -> detenerse
        if (!haySuelo)
        {
            rb.velocity = new Vector2(
                Mathf.Lerp(rb.velocity.x, 0f, Time.deltaTime * 20f),
                rb.velocity.y
            );

            return;
        }

        // Movimiento suave
        float velocidadObjetivo =
            direccionHuida * velocidadHuida;

        float velocidadX = Mathf.Lerp(
            rb.velocity.x,
            velocidadObjetivo,
            Time.deltaTime * aceleracion
        );

        rb.velocity = new Vector2(
            velocidadX,
            rb.velocity.y
        );
    }

    void Girar()
    {
        mirandoDerecha = !mirandoDerecha;

        Vector3 escala = transform.localScale;
        escala.x *= -1f;
        transform.localScale = escala;

        // IMPORTANTE:
        // NO mover el groundCheck manualmente.
        // Debe ser hijo del NPC para girar automáticamente.
    }

    void OnDrawGizmosSelected()
    {
        // Radio miedo
        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            radioMiedo
        );

        // Detector suelo
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;

            Gizmos.DrawWireSphere(
                groundCheck.position,
                radioGroundCheck
            );
        }
    }
}