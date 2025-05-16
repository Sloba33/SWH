using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightingManager : MonoBehaviour
{
    [SerializeField] List<GameObject> lightGameObjects = new();
    List<Light> lights = new();
    public int maxIntensityDivider;
    public int flickerFrequency, flickerTimeAfterGameStartMin, flickerTimeAfterGameStartMax;
    public float firstFlickerDelay, secondFlickerDelay, thirdFlickerDelay;

    private void Start()
    {
        foreach (GameObject light in lightGameObjects)
        {
            lights.Add(light.GetComponent<Light>());
        }
        InvokeRepeating("FlickerLights", Random.Range(flickerTimeAfterGameStartMin, flickerTimeAfterGameStartMax + 1), flickerFrequency);
    }

    public void FlickerLights()
    {
        int random = Random.Range(0, lights.Count);
        float startIntensity = lights[random].intensity;
        float newIntensity = lights[random].intensity / Random.Range(2, maxIntensityDivider);
        lights[random].intensity = newIntensity;
        StartCoroutine(Flicker(startIntensity, newIntensity, random));
    }
    private IEnumerator Flicker(float startIntensity, float newIntensity, int random)
    {
        yield return new WaitForSeconds(firstFlickerDelay);
        lights[random].intensity = startIntensity;
        yield return new WaitForSeconds(secondFlickerDelay);
        lights[random].intensity = newIntensity;
        yield return new WaitForSeconds(thirdFlickerDelay);
        lights[random].intensity = startIntensity;
        yield return new WaitForSeconds(thirdFlickerDelay);
        lights[random].intensity = newIntensity;
        yield return new WaitForSeconds(thirdFlickerDelay);
        lights[random].intensity = startIntensity;
    }
}
