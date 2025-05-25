using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlashingLights : MonoBehaviour
{
    public Light targetLight; // Arrastra tu luz aquí en el Inspector
    public float onDuration = 0.5f; // Duración que la luz estará encendida
    public float offDuration = 0.5f; // Duración que la luz estará apagada

    void Start()
    {
        if (targetLight == null)
        {
            Debug.LogError("No se ha asignado una luz al script FlashingLightSimple. Por favor, arrastra tu luz al Inspector.");
            enabled = false; // Deshabilita el script si no hay luz
            return;
        }

        // Inicia la rutina de parpadeo
        StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        while (true) // Bucle infinito para un parpadeo continuo
        {
            targetLight.enabled = true; // Enciende la luz
            yield return new WaitForSeconds(onDuration); // Espera la duración encendida

            targetLight.enabled = false; // Apaga la luz
            yield return new WaitForSeconds(offDuration); // Espera la duración apagada
        }
    }
}
