using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class Movimiento2D : MonoBehaviour
{
    public Controles Controles;
    public Vector2 direccion;
    public Rigidbody2D rbd;
    public float velmove;
    public bool mirandoDrecha = true;
    public float fuerzaSalto = 5f;
    public float saltoSostenido = 0.5f; // tiempo maximo que se puede sostener el salto
    public float fuerzaSaltoExtra = 5f; // fuerza adicional mientras mantienes presionado
    private bool manteniendoSalto;
    private float tiempoSalto = 1f;
    public LayerMask queEsSuelo;
    public Transform controladorSuelo;
    public Vector3 dimecionesCaja;
    public bool enSuelo;
    public bool sePuedeMover = true;
    [SerializeField] private Vector2 velocidadRebote;

    // Variables para el dash
    public float velocidadDashHorizontal = 30f;
    public float velocidadDashVertical = 22f;
    public float tiempoDash = 0.2f;
    public float tiempoEntreDashes = 1f;
    private bool puedeDashear = true;
    private bool estaDasheando = false;
    private float direccionDash;    
    private bool estabaMoviendose = false;

    public Animator animator;

    public ParticleSystem particulaCorrer;
    public ParticleSystem particulaCorriendo;
    public ParticleSystem particulaSaltar;

    private void Start()
    {
        rbd = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }
    private void Awake()
    {

        Controles = new();
    }

    private void OnEnable()
    {
        Controles.Enable();
        Controles.Base.Jump.started += _ => IniciarSalto();
        Controles.Base.Jump.canceled += _ => FinalizarSalto();
        Controles.Base.Dash.performed += _ => RealizarDash();
    }

    private void OnDisable()
    {
        Controles.Disable();
        Controles.Base.Jump.started -= _ => IniciarSalto();
        Controles.Base.Jump.canceled -= _ => FinalizarSalto();
        Controles.Base.Dash.performed -= _ => RealizarDash();
    }

    private void Update()
    {

        enSuelo = Physics2D.OverlapBox(controladorSuelo.position, dimecionesCaja, 0f, queEsSuelo);
        animator.SetBool("enSuelo", enSuelo);

        // Verificar si el jugador sigue presionando el salto
        bool saltoPresionado = Controles.Base.Jump.ReadValue<float>() > 0f;

        // Si se mantiene presionado y está en la ventana del salto sostenido
        if (manteniendoSalto && saltoPresionado && tiempoSalto < saltoSostenido && rbd.velocity.y > 0)
        {
            tiempoSalto += Time.deltaTime;

            // Aplicamos una fuerza proporcional al tiempo sostenido, cada frame más pequeña
            float factor = 1f - (tiempoSalto / saltoSostenido);
            rbd.AddForce(Vector2.up * fuerzaSaltoExtra * factor, ForceMode2D.Force);

            if (rbd.velocity.y > 8f)
            {
                rbd.velocity = new Vector2(rbd.velocity.x, 8f);
            }

        }
        else if (!saltoPresionado || enSuelo)
        {
            manteniendoSalto = false;
        }

        if (!sePuedeMover && !manteniendoSalto)
        {
            // Fuerza la animación de quieto mientras ataca o no puede moverse
            animator.SetFloat("Vel", 0);
            return;
        }

        direccion = Controles.Base.Move.ReadValue<Vector2>();
        AjustarRotacion(direccion.x);
        animator.SetFloat("Vel", Mathf.Abs(direccion.x));

        bool moviendose = Mathf.Abs(direccion.x) > 0.1f && enSuelo;

        // Cuando empieza a moverse
        if (moviendose && !estabaMoviendose)
        {
            Correr();       // polvo inicial
            Corriendo();    // inicia el loop de correr
        }

        // Cuando deja de moverse
        if (!moviendose && estabaMoviendose)
        {
            DetenerCorriendo();
        }

        estabaMoviendose = moviendose;
    }

    private void FixedUpdate()
    {
        if (sePuedeMover && !estaDasheando)
        {
            rbd.velocity = new Vector2(direccion.x * velmove, rbd.velocity.y);
        }

        if (!sePuedeMover && manteniendoSalto)
        {
            rbd.velocity = new Vector2(direccion.x * velmove, rbd.velocity.y);
        }
    }

    private void AjustarRotacion(float direccionX)
    {
        if (direccionX > 0 && !mirandoDrecha)
        {
            Girar();
        }
        else if (direccionX < 0 && mirandoDrecha)
        {
            Girar();
        }
    }
    private void Girar()
    {
        mirandoDrecha = !mirandoDrecha;
        Vector3 escala = transform.localScale;
        escala.x *= -1;
        transform.localScale = escala;

        // Aplicar efecto espejo en el material de las partículas
        if (particulaCorrer != null)
        {
            var renderer = particulaCorrer.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.flip = new Vector3(mirandoDrecha ? 0 : 1, 0, 0);
            }
        }

        if (particulaCorriendo != null)
        {
            var renderer = particulaCorriendo.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.flip = new Vector3(mirandoDrecha ? 0 : 1, 0, 0);
            }
        }
    }

    private void IniciarSalto()
    {
        if (enSuelo)
        {
            rbd.velocity = new Vector2(rbd.velocity.x, 0);
            rbd.AddForce(Vector2.up * fuerzaSalto, ForceMode2D.Impulse);
            manteniendoSalto = true;
            tiempoSalto = 0f;
            Saltar();
        }
    }

    private void FinalizarSalto()
    {
        manteniendoSalto = false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(controladorSuelo.position, dimecionesCaja);
    }

    public void Rebote(Vector2 puntoGolpe)
    {
        rbd.velocity = new Vector2(-velocidadRebote.x * puntoGolpe.x, velocidadRebote.y);
    }

    private void RealizarDash()
    {
        if (puedeDashear && !estaDasheando)
        {
            StartCoroutine(Dash());
        }
    }

    private IEnumerator Dash()
    {
        puedeDashear = false;
        estaDasheando = true;

        float gravityOriginal = rbd.gravityScale;
        rbd.gravityScale = 0;

        float direccionX = direccion.x;
        float direccionY = direccion.y;

        // Si no hay input horizontal ni vertical, dash hacia donde mira
        if (direccionX == 0 && direccionY == 0)
            direccionX = mirandoDrecha ? 1 : -1;

        // Determinar animación y velocidad
        Vector2 dashDir;

        if (Mathf.Abs(direccionY) > Mathf.Abs(direccionX))
        {
            // DASH VERTICAL
            dashDir = new Vector2(0, Mathf.Sign(direccionY));

            if (direccionY > 0)
                animator.SetTrigger("DashUp");
            else
                animator.SetTrigger("DashDown");
        }
        else
        {
            // DASH HORIZONTAL
            dashDir = new Vector2(Mathf.Sign(direccionX), 0);
            animator.SetTrigger("Dash");
        }

        // Aplicar velocidad
        if (dashDir.x != 0)
            rbd.velocity = new Vector2(dashDir.x * velocidadDashHorizontal, 0);
        else
            rbd.velocity = new Vector2(0, dashDir.y * velocidadDashVertical);

        yield return new WaitForSeconds(tiempoDash);

        // Restaurar estado
        rbd.gravityScale = gravityOriginal;
        rbd.velocity = Vector2.zero;
        animator.SetBool("Dash", false);
        animator.SetBool("DashUp", false);
        animator.SetBool("DashDown", false);

        estaDasheando = false;

        yield return new WaitForSeconds(tiempoEntreDashes);
        puedeDashear = true;
    }

    private void Correr()
    {
        if (particulaCorrer != null && !particulaCorrer.isPlaying)
        {
            particulaCorrer.Play();
        }
    }

        private void Corriendo()
    {
        if (particulaCorriendo != null)
            particulaCorriendo.Play();
    }

    private void DetenerCorriendo()
    {
        if (particulaCorriendo != null)
        {
            particulaCorriendo.Stop();
            particulaCorriendo.Clear(); // Borrar las partículas existentes inmediatamente
        }
    }


    private void Saltar()
    {
        if (particulaSaltar != null && !particulaSaltar.isPlaying)
        {
            particulaSaltar.Play();
        }
    }

}