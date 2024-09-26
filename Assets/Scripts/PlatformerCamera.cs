using UnityEngine;

public class PlatformerCamera : MonoBehaviour
{
    [SerializeField] private new Camera camera;
    [SerializeField] private Transform target;
    [SerializeField] private float verticalOffset = 5f;
    [SerializeField] private float velocityThreshold = 1f;
    [SerializeField] private ExpMovingAverageFloat horizontalPosition;
    [SerializeField] private ExpMovingAverageFloat verticalPosition;

    private void OnEnable()
    {
        horizontalPosition.Reset(target.position.x);
        verticalPosition.Reset(target.position.y);
    }

    private void Update()
    {
        Vector2 previous = new(horizontalPosition, verticalPosition);
        
        horizontalPosition.AddSample(target.position.x, Time.deltaTime);
        float verticalDistance = Mathf.Abs(camera.WorldToScreenPoint(target.position + verticalOffset * Vector3.up).y - camera.pixelHeight / 2f);
        verticalPosition.AddSample(target.position.y + verticalOffset, Time.deltaTime * FollowStrengthMultiplier(verticalDistance));

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
