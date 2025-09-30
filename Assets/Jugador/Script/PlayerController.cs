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
public float fuerzaSalto;
public LayerMask queEsSuelo;
public Transform controladorSuelo;
public Vector3 dimecionesCaja;
public bool enSuelo;
public bool sePuedeMover = true;
[SerializeField]private Vector2 velocidadRebote;

// Variables para el dash
public float velocidadDash = 20f;
public float tiempoDash = 0.2f;
public float tiempoEntreDashes = 1f;
private bool puedeDashear = true;
private bool estaDasheando = false;
private float direccionDash;

public Animator animator;
    private void Start()
    {
        rbd = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }
    private void Awake(){

    Controles = new();
}

private void OnEnable() {
    Controles.Enable();
    Controles.Base.Jump.started += _ => Saltar();
    Controles.Base.Dash.performed += _ => RealizarDash();
}

private void OnDisable() {
    Controles.Disable();
    Controles.Base.Jump.started -= _ => Saltar();
    Controles.Base.Dash.performed -= _ => RealizarDash();
}

private void Update(){
    direccion = Controles.Base.Move.ReadValue<Vector2>();
    AjustarRotacion(direccion.x);
    enSuelo = Physics2D.OverlapBox(controladorSuelo.position, dimecionesCaja, 0f, queEsSuelo);
    animator.SetFloat("Vel", Mathf.Abs(direccion.x));
    animator.SetBool("enSuelo", enSuelo);
}

    private void FixedUpdate(){
        if(sePuedeMover && !estaDasheando)
        {
            rbd.velocity = new Vector2(direccion.x * velmove, rbd.velocity.y);
        }
        
    }

    private void AjustarRotacion(float direccionX){
        if (direccionX > 0 && !mirandoDrecha)
        {
            Girar();
        }else if (direccionX < 0 && mirandoDrecha)
        {
            Girar();
        }
    }
    private void Girar(){
        mirandoDrecha = !mirandoDrecha;
        Vector3 escala = transform.localScale;
        escala.x *= -1;
        transform.localScale=escala;
    }

    private void Saltar(){
        if(enSuelo){
            rbd.AddForce(new Vector2(0, fuerzaSalto), ForceMode2D.Impulse);
            }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(controladorSuelo.position, dimecionesCaja);
    }

    public void Rebote(Vector2 puntoGolpe){
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
        
        // Determinar la dirección del dash
        float direccionX = direccion.x;
        if (direccionX == 0)
        {
            // Si no hay input horizontal, dash en la dirección a la que mira el personaje
            direccionX = mirandoDrecha ? 1 : -1;
        }
        
        // Aplicar velocidad de dash
        rbd.velocity = new Vector2(direccionX * velocidadDash, 0);
        
        // Activar efecto visual si tienes alguno
        animator.SetBool("Dash", true);
        
        // Esperar el tiempo del dash
        yield return new WaitForSeconds(tiempoDash);
        
        // Restaurar valores
        rbd.gravityScale = gravityOriginal;
        estaDasheando = false;
        animator.SetBool("Dash", false);
        
        // Esperar el cooldown entre dashes
        yield return new WaitForSeconds(tiempoEntreDashes - tiempoDash);
        puedeDashear = true;
    }
}