using UnityEngine;
using UnityEngine.VFX;

[ExecuteAlways]
public class ParticleController : MonoBehaviour
{
    [SerializeField] private VisualEffect visualEffect;

    private void OnEnable()
    {
        SpawnParticle();
    }

    public void SpawnParticle()
    {
        visualEffect.Play();
    }
}
