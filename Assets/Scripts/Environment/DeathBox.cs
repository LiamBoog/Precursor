using System;
using UnityEngine;

public class DeathBox : MonoBehaviour
{
    public event Action OnDeath;
    
    public void OnTriggerEnter2D(Collider2D other)
    {
        OnDeath?.Invoke();
    }
}
