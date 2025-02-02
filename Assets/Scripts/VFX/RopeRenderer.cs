using UnityEngine;

[ExecuteAlways]
public class RopeRenderer : MonoBehaviour
{
    [SerializeField] private ParameterSelector<Material> aProperty;
    [SerializeField] private ParameterSelector<Material> bProperty;
    [SerializeField] private Transform a;
    [SerializeField] private Transform b;
    [SerializeField] private int ppi = 10;

    private void Update()
    {
        aProperty.Target.SetVector(aProperty, a.position);
        bProperty.Target.SetVector(bProperty, b.position);

        transform.position = (a.position + b.position) / 2f;
        Vector2 diff = a.position - b.position;
        transform.localScale = (2f / ppi + Mathf.Max(Mathf.Abs(diff.x), Mathf.Abs(diff.y))) * Vector3.one;
    }
}
