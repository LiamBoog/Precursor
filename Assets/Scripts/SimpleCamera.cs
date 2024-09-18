using UnityEngine;

public class SimpleCamera : MonoBehaviour
{
    [SerializeField] private Transform player;

    private void Update()
    {
        transform.position += new Vector3(player.position.x - transform.position.x, 0f);
    }
}