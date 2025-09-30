using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Parallax : MonoBehaviour
{
    [SerializeField] private float amountOfParallax = 0.5f;
    [SerializeField] private Camera mainCamera;

    private float lengthOfSprite;
    private Vector3 startPosition;

    private void Start()
    {
        startPosition = transform.position;
        lengthOfSprite = GetComponent<SpriteRenderer>().bounds.size.x;

        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void Update()
    {
        float camPosX = mainCamera.transform.position.x;
        float distance = camPosX * amountOfParallax;
        transform.position = new Vector3(startPosition.x + distance, startPosition.y, startPosition.z);

        if (camPosX - transform.position.x > lengthOfSprite)
        {
            startPosition.x += lengthOfSprite * 2;
        }
        else if (transform.position.x - camPosX > lengthOfSprite)
        {
            startPosition.x -= lengthOfSprite * 2;
        }
    }
}