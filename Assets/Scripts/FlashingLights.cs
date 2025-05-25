using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlashingLights : MonoBehaviour
{
    public Light targetLight; // Arrastra tu luz aqu� en el Inspector
    public float onDuration = 0.5f; // Duraci�n que la luz estar� encendida
    public float offDuration = 0.5f; // Duraci�n que la luz estar� apagada

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
            yield return new WaitForSeconds(onDuration); // Espera la duraci�n encendida

            targetLight.enabled = false; // Apaga la luz
            yield return new WaitForSeconds(offDuration); // Espera la duraci�n apagada
        }
    }
}
