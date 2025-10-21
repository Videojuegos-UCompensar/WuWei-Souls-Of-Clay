using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementComponent : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 3f;

    [Header("Detección de suelo")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckDistance = 1f;
    [SerializeField] private float ledgeCheckDistance = 0.5f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator anim;

    private Vector2 moveDirection;
    private bool facingRight = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    private void FixedUpdate()
    {
        rb.velocity = new Vector2(moveDirection.x * moveSpeed, rb.velocity.y);
        anim.SetBool("IsMoving", moveDirection != Vector2.zero);

        Debug.Log($"Velocity: {rb.velocity}");
    }

    public void MoveTowards(Vector2 targetPosition)
    {
        moveDirection = (targetPosition - (Vector2)transform.position).normalized;
        moveDirection = new Vector2(moveDirection.x, 0f);
        FaceDirection(moveDirection.x);
    }

    public void Idle()
    {
        moveDirection = Vector2.zero;
        anim.SetBool("IsMoving", false);
    }

    public void MoveAwayFrom(Vector2 target)
    {
        Vector2 direction = ((Vector2)transform.position - target).normalized;
        moveDirection = new Vector2(direction.x, 0);
        FaceDirection(moveDirection.x);
    }

    public void FaceDirection(float directionX)
    {
        if (directionX > 0 && !facingRight)
        {
            facingRight = true;
            Flip(true);
        }
        else if (directionX < 0 && facingRight)
        {
            facingRight = false;
            Flip(false);
        }
    }

    private void Flip(bool right)
    {
        Vector3 scale = transform.localScale;
        scale.x = right ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
        transform.localScale = scale;
    }

    public bool IsGroundAhead()
    {
        if (groundCheck == null) return true;

        Vector2 dir = facingRight ? Vector2.right : Vector2.left;

        // Revisión directa hacia abajo
        bool groundBelow = Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, groundLayer);

        // Revisión hacia abajo un poco más adelante
        Vector2 ledgeOrigin = groundCheck.position + (Vector3)(dir * 0.4f);
        bool ledgeAhead = Physics2D.Raycast(ledgeOrigin, Vector2.down, ledgeCheckDistance, groundLayer);

        return groundBelow && ledgeAhead;
    }

    /// <summary>
    /// Comprueba si hay suelo en la dirección X que indica 'direction'. Útil para otros componentes que
    /// necesiten saber si es seguro moverse hacia/desde una posición (por ejemplo IA).
    /// </summary>
    public bool IsGroundAheadInDirection(Vector2 direction)
    {
        if (groundCheck == null) return true;

        float signX = Mathf.Sign(direction.x);
        Vector2 dir = signX >= 0 ? Vector2.right : Vector2.left;

        bool groundBelow = Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, groundLayer);
        Vector2 ledgeOrigin = groundCheck.position + (Vector3)(dir * 0.4f);
        bool ledgeAhead = Physics2D.Raycast(ledgeOrigin, Vector2.down, ledgeCheckDistance, groundLayer);

        return groundBelow && ledgeAhead;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(groundCheck.position, groundCheck.position + Vector3.down * groundCheckDistance);

        Vector3 dir = facingRight ? Vector3.right : Vector3.left;
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(groundCheck.position + dir * 0.4f, groundCheck.position + dir * 0.4f + Vector3.down * ledgeCheckDistance);
    }
}
