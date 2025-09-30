using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueTrigger : MonoBehaviour
{
    [Header("Configuración del Diálogo")]
    [TextArea(3, 10)]
    public string mensaje;
    public float distanciaActivacion = 2f;
    
    [Header("Referencias")]
    public GameObject panelDialogo;
    public TMP_Text textoDialogo;
    
    private GameObject player;
    private bool dialogoActivo = false;
    
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        
        if (panelDialogo != null)
        {
            panelDialogo.SetActive(false);
        }
    }
    
    void Update()
    {
        if (player == null) return;
        
        float distancia = Vector2.Distance(transform.position, player.transform.position);
        
        if (distancia <= distanciaActivacion && !dialogoActivo)
        {
            MostrarDialogo();
        }
        else if (distancia > distanciaActivacion && dialogoActivo)
        {
            OcultarDialogo();
        }
    }
    
    void MostrarDialogo()
    {
        dialogoActivo = true;
        if (panelDialogo != null)
        {
            panelDialogo.SetActive(true);
            textoDialogo.text = mensaje;
        }
    }
    
    void OcultarDialogo()
    {
        dialogoActivo = false;
        if (panelDialogo != null)
        {
            panelDialogo.SetActive(false);
        }
    }
    
    // Para visualizar el radio de activación en el editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, distanciaActivacion);
    }
}