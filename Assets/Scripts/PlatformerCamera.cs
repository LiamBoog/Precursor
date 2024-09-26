using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformerCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float verticalOffset = 5f;
    [SerializeField] private ExpMovingAverageFloat horizontalPosition;
    [SerializeField] private ExpMovingAverageFloat verticalPosition;

    private void OnEnable()
    {
        horizontalPosition.AddSample(target.position.x, float.MaxValue);
        verticalPosition.AddSample(target.position.y, float.MaxValue);
    }

    private void Update()
    {
        horizontalPosition.AddSample(target.position.x, Time.deltaTime);
        verticalPosition.AddSample(target.position.y, Time.deltaTime);

        transform.position = new Vector3(horizontalPosition, verticalPosition + verticalOffset, transform.position.z);
    }
}
