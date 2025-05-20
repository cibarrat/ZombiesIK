using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PostProcessing : MonoBehaviour
{
    [SerializeField] private VolumeProfile damageVolumeProfile;
    [SerializeField] private VolumeProfile healVolumeProfile;

    private float intensity = 0;
    private float sceneTime;
    private float startTime;
    private float reduceIntensityTime;

    private Volume _volume; 
    private Vignette _vignette;

    // Start is called before the first frame update
    void Start()
    {
        startTime = Time.time;
        _volume = GetComponent<Volume>();
        //como el vignette no es un component, tenemos que sacarlo usando el profile que creamos
        _volume.profile.TryGet<Vignette>(out _vignette); //se usa out para guardar el resultado en _vignette, ya que la función como tal devuelve true o false

        _vignette.intensity.value = 0;

        
    }

    // Update is called once per frame
    void Update()
    {
        sceneTime = Time.time - startTime;

        if (intensity > 0)
        {
            if (sceneTime >= reduceIntensityTime)
            {
                reduceIntensityTime += 0.1f;
                intensity -= 0.03f;
                Debug.Log("Intensidad" + intensity);

                if (intensity < 0)
                {
                    intensity = 0;
                }
                _vignette.intensity.value = intensity;
            }
        }

    }

    public void ActivateDamageVignette()
    {
        _volume.profile = damageVolumeProfile;
        _volume.profile.TryGet<Vignette>(out _vignette);
        intensity = 0.4f;
        reduceIntensityTime = sceneTime + 0.4f;
        _vignette.intensity.value = intensity;
    }

    public void ActivateHealVignette()
    {
        _volume.profile = healVolumeProfile;
        _volume.profile.TryGet<Vignette>(out _vignette);
        intensity = 0.4f;
        reduceIntensityTime = sceneTime + 0.4f;
        _vignette.intensity.value = intensity;
    }
}
