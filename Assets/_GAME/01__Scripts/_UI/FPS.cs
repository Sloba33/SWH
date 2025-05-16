using UnityEngine;
using System.Collections;
using TMPro;
public class FPS : MonoBehaviour
{
    private float count;
    public TextMeshProUGUI fpsText;
    private IEnumerator Start()
    {
        while (true)
        {
            count = 1f / Time.unscaledDeltaTime;
            yield return new WaitForSeconds(0.15f);
        }
    }

    private void Update()
    {
        if (Mathf.Round(count) >= 50)
        {
            fpsText.color = Color.green;
        }
        else if (Mathf.Round(count) < 50 && (Mathf.Round(count) > 25))
        {
            fpsText.color = Color.yellow;
        }
        else fpsText.color = Color.red;
        fpsText.text = "FPS: " + Mathf.Round(count);
    }
}