using UnityEngine;

[ExecuteAlways]
public class RopeRenderer : MonoBehaviour
{
    [SerializeField] private ParameterSelector<Material> aProperty;
    [SerializeField] private ParameterSelector<Material> bProperty;
    [SerializeField] private Transform a;
    [SerializeField] private Transform b;

    private void Update()
    {
        aProperty.Target.SetVector(aProperty, a.position);
        bProperty.Target.SetVector(bProperty, b.position);
    }
}
