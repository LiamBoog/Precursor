using UnityEngine;

public static class CustomDebug
{
    private const int LINEAR_RESOLUTION = 100;
    
    /// <summary>
    /// Draw an arc about center. Rotation direction is right-handed with respect to the plane vector.
    /// </summary>
    /// <param name="origin">The starting point of the arc.</param>
    /// <param name="center">The center of the arc.</param>
    /// <param name="plane">The plane the arc lies upon.</param>
    /// <param name="radius">The radius of the arc.</param>
    /// <param name="angle">The angle of the arc in degrees.</param>
    /// <param name="colour">The colour of the arc</param>
    /// <param name="duration">The duration of the arc.</param>
    public static void DrawArc(Vector3 origin, Vector3 center, Vector3 plane, float radius, float angle, Color colour, float duration = 0f)
    {
        angle *= Mathf.Deg2Rad;
        int numSegments = Mathf.RoundToInt(angle * radius * LINEAR_RESOLUTION);
        float angleIncrement = angle / numSegments;
        Vector3 currentPoint = origin;
        for (int i = 1; i <= numSegments; i++)
        {
            Vector3 nextPoint = GetPointOnCircumference(i * angleIncrement);
            Debug.DrawLine(currentPoint, nextPoint, colour, duration);
            currentPoint = nextPoint;

        }

        Vector3 GetPointOnCircumference(float angle)
        {
            Vector3 right = (origin - center).normalized;
            Vector3 up = Vector3.Cross(plane.normalized, right);
            return center + radius * (Mathf.Sin(angle) * up + Mathf.Cos(angle) * right);
        }
    }

    /// <summary>
    /// Draw an arc in the x-y plane. Rotation direction is right-handed about the -z axis.
    /// </summary>
    /// <param name="origin">The starting point of the arc.</param>
    /// <param name="center">The center of the arc.</param>
    /// <param name="clockwise">Whether to rotate clockwise or counter-clockwise.</param>
    /// <param name="radius">The radius of the arc.</param>
    /// <param name="angle">The angle of the arc in degrees.</param>
    /// <param name="colour">The colour of the arc</param>
    /// <param name="duration">The duration of the arc.</param>
    public static void DrawArc2D(Vector3 origin, Vector3 center, float radius, float angle, Color colour, bool clockwise = true, float duration = 0f)
    {
        DrawArc(origin, center, clockwise ? -Vector3.forward : Vector3.forward, radius, angle, colour, duration);
    }

    /// <summary>
    /// Draw a circle in the x-y plane.
    /// </summary>
    /// <param name="center">The center of the circle.</param>
    /// <param name="radius">The radius of the circle.</param>
    /// <param name="colour">The colour of the circle.</param>
    /// <param name="duration">The duration of the circle.</param>
    public static void DrawCircle2D(Vector3 center, float radius, Color colour, float duration = 0f)
    {
        DrawArc2D(center + radius * Vector3.up, center, radius, 360f, colour, true, duration);
    }

    /// <summary>
    /// Draw a curve defined by the given array of points.
    /// </summary>
    /// <param name="curve">The curve to draw.</param>
    /// <param name="colour">The colour of the curve.</param>
    /// <param name="duration">The duration of the curve.</param>
    public static void DrawCurve(Vector3[] curve, Color colour, float duration = 0f)
    {
        for (int i = 1; i < curve.Length; i++)
        {
            Debug.DrawLine(curve[i], curve[i - 1], colour, duration);
        }
    }

    /// <summary>
    /// Draw a contour defined by the given array of points.
    /// </summary>
    /// <param name="contour">The contour to draw.</param>
    /// <param name="colour">The colour of the contour.</param>
    /// <param name="duration">The duration of the contour.</param>
    public static void DrawContour(Vector3[] contour, Color colour, float duration = 0f)
    {
        DrawCurve(contour, colour, duration);
        Debug.DrawLine(contour[0], contour[^1], colour, duration);
    }
}
