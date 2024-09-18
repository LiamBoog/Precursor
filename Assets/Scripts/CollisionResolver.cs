using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CollisionResolver : MonoBehaviour
{
    private readonly struct Raycast
    {
        public readonly Vector2 start;
        public readonly Vector2 dir;
        public readonly Vector2 end;

        public Raycast(Vector2 start, Vector2 dir)
        {
            this.start = start;
            this.dir = dir;
            end = this.start + this.dir;
        }
    }
    public enum Edge
    {
        Left,
        Right,
        Top,
        Bottom
    }
    
    [SerializeField] private new BoxCollider2D collider;
    [SerializeField] private float skin = 0.001f;
    [SerializeField] private LayerMask collisionLayers;

    [SerializeField] private float maxRaycastOffset = 1f;
    [SerializeField] private float maxNudgeDistance = 0.5f;

    public delegate void CollisionResponse(Vector2 deflection);
    public event CollisionResponse Collision;
    
    public bool Touching(Edge edge, float overlapDistance = 0f)
    {
        Bounds bounds = collider.bounds;
        Vector3 extents = bounds.extents;
        Vector3 center = bounds.center;

        overlapDistance += 2f * skin;
        Raycast raycast = edge switch
        {
            Edge.Left => new(
                center - extents - new Vector3(overlapDistance, 0f), 
                new(0f, 2f * extents.y)),
            Edge.Right => new(
                center + extents + new Vector3(overlapDistance, 0f), 
                new(0f, -2f * extents.y)),
            Edge.Top => new(
                center + extents + new Vector3(0, overlapDistance), 
                new(-2f * extents.x, 0f)),
            _ => new(
                center - extents - new Vector3(0f, overlapDistance), 
                new(2f * extents.x, 0f))
        };
        
        return Physics2D.Linecast(raycast.start, raycast.end, collisionLayers);
    }

    public void Collide(Vector2 displacement)
    {
        Vector2 deflection = MoveWithNudge(displacement);

        if (deflection == default) 
            return;

        Collision?.Invoke(deflection);
    }

    private Vector2 MoveWithNudge(Vector2 displacement)
    {
        Vector2 totalDeflection = GetMovementDeflection(displacement);

        if (totalDeflection is not { y: < 0f, x: 0f })
            return Default();

        float verticalBeforeNudge = displacement.y + totalDeflection.y;
        var (fromRight, fromLeft) = GetLineCasts();
        float hitDistance = Mathf.Max(fromRight.distance, fromLeft.distance);
        
        // Check if the vertical collision was on the corner of the collider
        if (hitDistance <= 0f)
            return Default();

        hitDistance = 2f * collider.bounds.extents.x - hitDistance;
        Vector2 displacementBeforeNudge = new(verticalBeforeNudge / displacement.y * displacement.x, verticalBeforeNudge);
        Vector2 nudge = GetNudge();
        
        // Check if the nudge distance is within the allowable range and in the direction of movement
        if (Mathf.Abs(nudge.x) > maxNudgeDistance || displacement.x * nudge.x < 0f)
            return Default();
            
        Move(displacementBeforeNudge);

        // Nudge to the corner of the collider if possible
        if (GetMovementDeflection(nudge) is var deflection && deflection.x != 0f)
        {
            Move(nudge + deflection);
            return deflection + new Vector2(0f, totalDeflection.y);
        }
        Move(nudge);
        
        // Move along the remaining displacement if possible, nudging again if necessary
        Vector2 displacementAfterNudge = new(-totalDeflection.y / displacement.y * displacement.x, -totalDeflection.y);
        return MoveWithNudge(displacementAfterNudge);

        Vector2 Default()
        {
            Move(displacement + totalDeflection);
            return totalDeflection;
        }

        Vector2 GetNudge()
        {
            float edgeDistance = fromRight.distance > fromLeft.distance ? hitDistance + skin : -hitDistance - skin;
            return new(edgeDistance - displacementBeforeNudge.x, 0f);
        }

        (RaycastHit2D, RaycastHit2D) GetLineCasts()
        {
            Vector2 topRight = collider.bounds.center + collider.bounds.extents + new Vector3(0f, verticalBeforeNudge + 2f * skin);
            Vector2 topLeft = topRight - new Vector2(2f * collider.bounds.extents.x, 0f);
            
            RaycastHit2D fromRight = Physics2D.Linecast(topRight, topLeft, collisionLayers);
            RaycastHit2D fromLeft = Physics2D.Linecast(topLeft, topRight, collisionLayers);

            return (fromRight, fromLeft);
        }
    }

    private void Move(Vector2 displacement)
    {
        Debug.DrawRay(transform.position, displacement, Color.blue, 3f);
        transform.position += (Vector3) displacement;
        Physics2D.SyncTransforms();
    }
    
    private Vector2 GetMovementDeflection(Vector2 displacement)
    {
        IEnumerable<Vector2> deflections = GetAxisAlignedCollisionDeflections(displacement);
        if (displacement.x * displacement.y != 0f && LeadingCornerLineCast(displacement, out Vector2 deflection))
        {
            deflections = deflections.Append(deflection);
        }

        return new Vector2
        {
            x = deflections.OrderBy(d => Mathf.Abs(d.x)).Select(d => d.x).LastOrDefault(),
            y = deflections.OrderBy(d => Mathf.Abs(d.y)).Select(d => d.y).LastOrDefault()
        };
    }

    private bool LeadingCornerLineCast(Vector2 direction, out Vector2 deflection)
    {
        Vector2 directionSign = new Vector2(Mathf.Sign(direction.x), Mathf.Sign(direction.y));
        Vector2 leadingCorner = (Vector2) collider.bounds.center + directionSign * collider.bounds.extents;
        Raycast cornerCast = new Raycast(leadingCorner, direction);

        if (LineCast(cornerCast, out Vector2 normal, out Vector2 penetration))
        {
            deflection = Vector2.Dot(penetration, normal) * normal + skin * normal;
            return true;
        }

        Raycast[] raycasts =
        {
            new(cornerCast.end, new(directionSign.x * skin, 0f)),
            new(cornerCast.end, new(0f, directionSign.y * skin))
        };
        foreach (Raycast raycast in raycasts)
        {
            if (!LineCast(raycast, out normal, out penetration))
                continue;

            deflection = Vector2.Dot(penetration, normal) * normal;
            return true;
        }

        deflection = Vector2.zero;
        return false;
    }
    
    private IEnumerable<Vector2> GetAxisAlignedCollisionDeflections(Vector2 direction)
    {
        List<Vector2> deflections = new();
        foreach (Raycast raycast in GetAxisAlignedLinecasts(direction))
        {
            if (!LineCast(raycast, out Vector2 normal, out Vector2 penetration))
                continue;

            Vector2 deflection = Vector2.Dot(penetration, normal) * normal;
            deflections.Add(deflection);
        }

        return deflections;
    }
    
    private bool LineCast(Raycast raycast, out Vector2 normal, out Vector2 penetration)
    {
        RaycastHit2D hit = Physics2D.Linecast(raycast.start, raycast.end, collisionLayers);
        normal = new(Mathf.RoundToInt(hit.normal.x), Mathf.RoundToInt(hit.normal.y)); // in case the normal isn't perfectly axis-aligned bcuz of floating point error
        penetration = hit.point - raycast.end;

        return hit.collider != null;
    }

    private IEnumerable<Raycast> GetAxisAlignedLinecasts(Vector2 direction)
    {
        Vector2 directionSign = new Vector2(Mathf.Sign(direction.x), Mathf.Sign(direction.y));
        Vector2 leadingCorner = (Vector2) collider.bounds.center + directionSign * collider.bounds.extents;
        Vector2 colliderSize = 2f * collider.bounds.extents;

        int numHorizontalCasts = GetNumCasts(colliderSize.y);
        float maxHorizontalOffset = GetCastOffset(colliderSize.y, numHorizontalCasts);
        for (int i = 0; i < numHorizontalCasts; i++)
        {
            float offset = Mathf.Min(colliderSize.y, i * maxHorizontalOffset);
            yield return new Raycast(
                leadingCorner + new Vector2(0f, -directionSign.y * offset),
                new Vector2(direction.x + directionSign.x * skin, 0f));
        }

        int numVerticalCasts = GetNumCasts(colliderSize.x);
        float maxVerticalOffset = GetCastOffset(colliderSize.x, numVerticalCasts);
        for (int i = 0; i < numVerticalCasts; i++)
        {
            float offset = Mathf.Min(colliderSize.x, i * maxVerticalOffset);
            yield return new Raycast(
                leadingCorner + new Vector2(-directionSign.x * offset, 0f),
                new Vector2(0f, direction.y + directionSign.y * skin));
        }

        int GetNumCasts(float size) => Mathf.CeilToInt(size / maxRaycastOffset) + 1;
        float GetCastOffset(float size, int numCasts) => size / (numCasts - 1);
    }
}
