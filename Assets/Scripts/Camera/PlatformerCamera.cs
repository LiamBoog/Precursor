using System;
using UnityEngine;
using UnityEngine.Serialization;

public interface ICameraTarget
{
    public Vector2 Position { get; }
    public Vector2 Velocity { get; }
}

public abstract class PlatformerCameraBase : MonoBehaviour
{
    [SerializeField] private PlayerController target;

    [SerializeField] protected new Camera camera;
    [SerializeField] protected float verticalOffset = 5f;
    [SerializeField] protected float velocityThreshold = 1f;
    [SerializeField] protected ExpMovingAverageFloat horizontalPosition;
    [SerializeField] protected ExpMovingAverageFloat verticalPosition;
    
    protected ICameraTarget Target => target;
}

public class PlatformerCamera : PlatformerCameraBase
{
    private void OnEnable()
    {
        horizontalPosition.Reset(Target.Position.x);
        verticalPosition.Reset(Target.Position.y);
    }

    private void Update()
    {
        Vector2 previous = new(horizontalPosition, verticalPosition);

        horizontalPosition.AddSample(Target.Position.x, Time.deltaTime);
        float verticalDistance = Mathf.Abs(camera.WorldToScreenPoint(Target.Position + verticalOffset * Vector2.up).y - camera.pixelHeight / 2f);
        verticalPosition.AddSample(Target.Position.y + verticalOffset, Time.deltaTime * FollowStrengthMultiplier(verticalDistance));

        if ((new Vector2(horizontalPosition, verticalPosition) - previous).magnitude / Time.deltaTime < velocityThreshold)
        {
            horizontalPosition.Reset(previous.x);
            verticalPosition.Reset(previous.y);
        }
        
        transform.position = new(horizontalPosition, verticalPosition, transform.position.z);
    }

    private float FollowStrengthMultiplier(float distance)
    {
        return 3f * distance / camera.pixelHeight;
    }
}
