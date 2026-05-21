using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DieTrigger : MonoBehaviour
{
    private NPCFear npc;
    private HealthComponent health;
    private bool yaMurio = false;

    private void Awake()
    {
        npc = GetComponentInParent<NPCFear>();
        health = GetComponentInParent<HealthComponent>();
        
        if (npc == null)
        {
            Debug.LogError("DieTrigger: No se encontró NPCFear en el padre!");
        }
        if (health == null)
        {
            Debug.LogError("DieTrigger: No se encontró HealthComponent en el padre!");
        }
    }

    private void Update()
    {
        // Si ya murió o no hay componentes, no hacer nada
        if (yaMurio || health == null || npc == null) 
            return;

        // Verificar si la salud está baja
        if (health.currentHealth <= 3)
        {
            yaMurio = true; // Marcar para evitar llamar a Die() múltiples veces
            
            Debug.Log($"NPC muriendo. Salud actual: {health.currentHealth}");
            npc.Die();
        }
    }
}
