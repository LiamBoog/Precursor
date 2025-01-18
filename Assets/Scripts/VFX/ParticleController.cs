using UnityEngine;
using UnityEngine.VFX;

[ExecuteAlways]
public class ParticleController : MonoBehaviour
{
    [SerializeField] private VisualEffect visualEffect;
    [SerializeField] private ParameterSelector<VisualEffect> positionProperty;
    [SerializeField] private Vector2 position;

    private void OnEnable()
    {
        SpawnParticle(position);
    }

    public void SpawnParticle(Vector2 position)
    {
        visualEffect.SetVector2(positionProperty, position);
        visualEffect.Play();
    }
}
