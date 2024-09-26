using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformerCamera : MonoBehaviour
{
    [SerializeField] private new Camera camera;
    [SerializeField] private Transform target;
    [SerializeField] private float verticalOffset = 5f;
    [SerializeField] private float velocityThreshold = 1f;
    [SerializeField] private ExpMovingAverageFloat horizontalPosition;
    [SerializeField] private ExpMovingAverageFloat verticalPosition;

    private float lastUpdateTime;

    private void OnEnable()
    {
        horizontalPosition.Reset(target.position.x);
        verticalPosition.Reset(target.position.y);
    }

    private void Update()
    {
        float previous = horizontalPosition;
        horizontalPosition.AddSample(target.position.x, Time.deltaTime);
        if (Mathf.Abs(horizontalPosition - previous) / Time.deltaTime < velocityThreshold)
        {
            horizontalPosition.Reset(previous);
        }

        previous = verticalPosition;
        float verticalDistance = Mathf.Abs(camera.WorldToScreenPoint(target.position + verticalOffset * Vector3.up).y - camera.pixelHeight / 2f);
        verticalPosition.AddSample(verticalOffset + target.position.y, Time.deltaTime * FollowStrengthMultiplier(verticalDistance));
        if (Mathf.Abs(verticalPosition - previous) / Time.deltaTime < velocityThreshold)
        {
            verticalPosition.Reset(previous);
        }

        transform.position = new(horizontalPosition, verticalPosition, transform.position.z);
    }

    private float FollowStrengthMultiplier(float distance)
    {
        return 3f * distance / camera.pixelHeight;
    }
}
