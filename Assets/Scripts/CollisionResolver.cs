using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CollisionResolver : MonoBehaviour
{
    public interface ICollision
    {
        Vector2 Deflection { get; }
        Vector2 Normal { get; }
    }
    
    public abstract bool Touching(Vector2 direction, float overlapDistance = 0f);
    
    public abstract ICollision Collide(Vector2 displacement);
}
