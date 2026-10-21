using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Light))]
public class ParpadeoLampara : MonoBehaviour
{
    public Renderer panel;
    [Min(1f)] public float pausaMinima = 6f;
    [Min(1f)] public float pausaMaxima = 18f;

    private Light foco;
    private float intensidadOriginal;
    private bool encendidoOriginal;
    private Color emisionOriginal;
    private Color colorOriginal;
    private MaterialPropertyBlock propiedades;
    private MaterialPropertyBlock propiedadesOriginales;
    private System.Random azar;
    private bool preparado;

    void OnEnable()
    {
        foco = GetComponent<Light>();
        intensidadOriginal = foco.intensity;
        encendidoOriginal = foco.enabled;
        azar = new System.Random(unchecked(GetEntityId().GetHashCode() ^ System.Environment.TickCount));

        if (panel != null && panel.sharedMaterial != null)
        {
            propiedades = new MaterialPropertyBlock();
            propiedadesOriginales = new MaterialPropertyBlock();
            panel.GetPropertyBlock(propiedadesOriginales);
            panel.GetPropertyBlock(propiedades);
            emisionOriginal = panel.sharedMaterial.GetColor("_EmissionColor");
            colorOriginal = panel.sharedMaterial.GetColor("_BaseColor");
        }

        preparado = true;
        StartCoroutine(Parpadear());
    }

    IEnumerator Parpadear()
    {
        // Cada lampara comienza en un momento diferente.
        yield return new WaitForSeconds(Entre(1f, pausaMaxima));
        while (true)
        {
            int destellos = azar.Next(2, 5);
            for (int i = 0; i < destellos; i++)
            {
                AplicarIntensidad(0f);
                yield return new WaitForSeconds(Entre(0.08f, 0.18f));
                AplicarIntensidad(Entre(0.55f, 1f));
                yield return new WaitForSeconds(Entre(0.10f, 0.28f));
            }

            if (azar.NextDouble() < 0.3)
            {
                AplicarIntensidad(0.12f);
                yield return new WaitForSeconds(Entre(0.3f, 0.7f));
            }

            AplicarIntensidad(1f);
            yield return new WaitForSeconds(Entre(pausaMinima, pausaMaxima));
        }
    }

    float Entre(float minimo, float maximo)
    {
        maximo = Mathf.Max(minimo, maximo);
        return Mathf.Lerp(minimo, maximo, (float)azar.NextDouble());
    }

    void AplicarIntensidad(float factor)
    {
        foco.enabled = encendidoOriginal && factor > 0f;
        foco.intensity = intensidadOriginal * factor;
        if (panel == null || propiedades == null)
            return;

        // Cambia solo este panel; las otras lamparas comparten el material.
        propiedades.SetColor("_EmissionColor", emisionOriginal * factor);
        propiedades.SetColor("_BaseColor", colorOriginal * Mathf.Lerp(0.12f, 1f, factor));
        panel.SetPropertyBlock(propiedades);
    }

    void OnDisable()
    {
        StopAllCoroutines();
        if (!preparado)
            return;

        foco.enabled = encendidoOriginal;
        foco.intensity = intensidadOriginal;
        if (panel != null && propiedadesOriginales != null)
            panel.SetPropertyBlock(propiedadesOriginales);
        preparado = false;
    }
}
