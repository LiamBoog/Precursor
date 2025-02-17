using UnityEngine;

public class DeathReset : MonoBehaviour
{
    private void Start()
    {
        Vector2 initialPosition = transform.position;
        foreach (DeathBox deathBox in FindObjectsByType<DeathBox>(FindObjectsSortMode.None))
        {
            deathBox.OnDeath += () => transform.position = initialPosition;
        }
    }
}
