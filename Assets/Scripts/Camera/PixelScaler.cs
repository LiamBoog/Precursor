using UnityEngine;

[ExecuteAlways]
public class PixelScaler : MonoBehaviour
{
    [SerializeField] private new Camera camera;
    [SerializeField] private Vector2Int resolution = new(320, 180);
    [SerializeField] private int texelsPerWorldUnit = 10;

    private void Update()
    {
        int texelScale = camera.pixelHeight / resolution.y;
        int pixelsPerWorldUnit = texelsPerWorldUnit * texelScale;
        camera.orthographicSize = (float) (0.5m * camera.pixelHeight / pixelsPerWorldUnit);
        camera.transform.position = WorldToNearestTexel(camera.transform.position);
        
        Debug.Log((texelScale, pixelsPerWorldUnit, camera.orthographicSize));
        Debug.Log($"Resolution: ({camera.pixelWidth} x {camera.pixelHeight}), Texel Scale: {camera.pixelHeight / resolution.y}");
        Debug.Log(WorldToNearestTexel(Vector2.zero));
        Debug.DrawLine(10f * Vector3.left, 10f * Vector3.right, Color.red);
        Debug.DrawLine(10f * Vector3.down, 10f * Vector3.up, Color.red);
    }

    private Vector3 WorldToNearestTexel(Vector3 worldPosition)
    {
        int texelScale = camera.pixelHeight / resolution.y;
        int pixelsPerWorldUnit = texelsPerWorldUnit * texelScale;
        decimal pixelWorldSize = 1m / pixelsPerWorldUnit;
        decimal texelWorldSize = texelScale * pixelWorldSize;
        
        int x = (int) decimal.Round((decimal) worldPosition.x / texelWorldSize);
        int y = (int) decimal.Round((decimal) worldPosition.y / texelWorldSize);
        int z = (int) decimal.Round((decimal) worldPosition.z / texelWorldSize);
        Debug.Log(pixelsPerWorldUnit);
        
        return new((float) (texelWorldSize * x), (float) (texelWorldSize * y), (float) (texelWorldSize * z));
    }
}
