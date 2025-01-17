using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

[ExecuteAlways]
public class ParticleController : MonoBehaviour
{
    [SerializeField] private VisualEffect visualEffect;

    private void OnEnable()
    {
        StartCoroutine(Routine());
        List<VFXExposedProperty> properties = new();
        visualEffect.visualEffectAsset.GetExposedProperties(properties);
        foreach (var property in properties)
        {
            Debug.Log(property.name);
        }
    }

    private IEnumerator Routine()
    {
        int n = 4;
        while (n-- > 0)
        {
            visualEffect.Play();
            yield return new WaitForSeconds(0.4f);
        }
    }
}
