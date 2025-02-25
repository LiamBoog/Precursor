using UnityEngine;

public class Rope : MonoBehaviour
{
    [SerializeField] private MaterialPropertySelector aProperty;
    [SerializeField] private MaterialPropertySelector bProperty;
    [SerializeField] private MaterialPropertySelector ppiProperty;
    [SerializeField] private new Renderer renderer;

    public void SetPositions(Vector2 a, Vector2 b)
    {
        renderer.material.SetVector(aProperty, a);
        renderer.material.SetVector(bProperty, b);

        transform.position = (a + b) / 2f;
        Vector2 diff = a - b;
        transform.localScale = (2f / ppiProperty.Target.GetInt(ppiProperty) + Mathf.Max(Mathf.Abs(diff.x), Mathf.Abs(diff.y))) * Vector3.one;
    }

    public void Show(bool show)
    {
        gameObject.SetActive(show);
    }
}