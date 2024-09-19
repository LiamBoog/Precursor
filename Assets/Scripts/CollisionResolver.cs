using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICollision : IInterrupt
{
    Vector2 Penetration { get; }
    Vector2 Deflection { get; }
    Vector2 Normal { get; }
}

public abstract class CollisionResolver : MonoBehaviour
{
    public abstract bool Touching(Vector2 direction, float overlapDistance = 0f);
    
    public abstract ICollision Collide(Vector2 displacement);
}