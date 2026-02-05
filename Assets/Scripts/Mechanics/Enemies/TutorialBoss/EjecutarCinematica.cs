using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class EjecutarCinematica : MonoBehaviour
{
    public PlayableDirector playableDirector;

    private void Start()
    {
        // Nos suscribimos al evento cuando la timeline termina
        playableDirector.stopped += OnCinematicaTerminada;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            gameObject.SetActive(false);
            playableDirector.Play();
        }
    }

    void OnCinematicaTerminada(PlayableDirector director)
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.RestartLevel();
        }
        else
        {
            Debug.LogError("No se encontró LevelManager en la escena. Necesario para reiniciar el nivel.");
        }
    }
}