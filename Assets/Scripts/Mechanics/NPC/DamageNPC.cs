using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageNPC : MonoBehaviour
{
    private NPCWander npc;

    private void Awake()
    {
        npc = GetComponentInParent<NPCWander>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            npc.Die();
        }
    }
}
