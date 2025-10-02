using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
public class EjecutarCinematica : MonoBehaviour
{
    public PlayableDirector playableDirector;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            gameObject.SetActive(false);

            playableDirector.Play();
        }
    }

}
