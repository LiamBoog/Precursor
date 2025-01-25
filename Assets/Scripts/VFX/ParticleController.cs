using UnityEngine;
using UnityEngine.VFX;

[ExecuteAlways]
public class ParticleController : MonoBehaviour
{
    [SerializeField] private VisualEffect visualEffect;

    public void SpawnParticle()
    {
        visualEffect.Play();
    }
}
