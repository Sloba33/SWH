using System.Collections;
using UnityEngine;
using DG.Tweening;

public class FloorSquareHole : MonoBehaviour
{
    [Header("Movement")]
    public float moveDuration = 0.5f;
    public float riseHeight = 0.5f;
    public float riseDuration = 0.5f; // Now a fixed, exposed value
    public float riseSpeed = 1f;
    public float fallDepth = -5f; // Negative value for falling down
    public float rotationDuration = 1f;


    [Header("Scaling")]
    public Vector3 punchScale = new Vector3(1.1f, 1.1f, 1.1f);
    public float punchDuration = 0.5f;
    public int punchVibrato = 5;
    public float targetScale = 0.1f;

    public Collider holeTrigger; // Assign this in the Inspector
    private Vector3 initialPosition;
    private Vector3 startScale;
    private GameObject currentObject;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle"))
        {
            other.GetComponent<Obstacle>().enabled = false;
            other.GetComponent<Collider>().enabled = false;
            currentObject = other.gameObject;


            AnimateObstacleFallingIn();
            // StartCoroutine(AnimateObstacleFallingIn());
        }
    }

    public float targetUpscale = 1.15f;

    private void AnimateObstacleFallingIn()
    {

        initialPosition = currentObject.transform.position;
        startScale = currentObject.transform.localScale;

        Vector3 holeCenter = holeTrigger.bounds.center;


        Sequence sequence = DOTween.Sequence();

        // 1. Move towards the hole center
        sequence.Append(currentObject.transform.DOMove(holeCenter, moveDuration));

        // 2. Knock the obstacle up slightly
        Vector3 raisedPosition = holeCenter + Vector3.up * riseHeight;
        // float riseDuration = riseHeight / riseSpeed;
        sequence.Append(currentObject.transform.DOMove(raisedPosition, riseDuration));

        // 3. Punch Scale
        sequence.Append(currentObject.transform.DOPunchScale(punchScale, punchDuration, punchVibrato));

        // 4. Fall and Shrink
        Vector3 targetPosition = new Vector3(holeCenter.x, fallDepth, holeCenter.z);
        sequence.Append(currentObject.transform.DOMove(targetPosition, rotationDuration).SetEase(Ease.Linear));
        sequence.Join(currentObject.transform.DOScale(Vector3.one * targetScale, rotationDuration).SetEase(Ease.Linear));

        // 5. Destroy (after a slight delay, if needed)
        sequence.AppendCallback(() => Destroy(currentObject));

        sequence.Play();
    }
    // private IEnumerator AnimateObstacleFallingIn()

    // {
    //     // Move the obstacle to the center of the hole
    //     Vector3 holeCenter = holeTrigger.bounds.center;

    //     float elapsedTime = 0f;

    //     // Move towards the hole center
    //     while (elapsedTime < moveDuration)
    //     {
    //         currentObject.transform.position = Vector3.Lerp(initialPosition, holeCenter, elapsedTime / moveDuration);
    //         elapsedTime += Time.deltaTime;
    //         yield return null;
    //     }

    //     // Knock the obstacle up slightly with adjustable speed
    //     Vector3 raisedPosition = holeCenter + Vector3.up * riseHeight;
    //     elapsedTime = 0f;
    //     float riseDuration = riseHeight / riseSpeed; // Calculate rise duration based on rise speed
    //     Debug.Log("Rise Duration is : " + riseDuration);
    //     while (elapsedTime < riseDuration)
    //     {
    //         currentObject.transform.position = Vector3.Lerp(holeCenter, raisedPosition, elapsedTime / riseDuration);
    //         // currentObject.transform.localScale = Vector3.Lerp(initialScale, Vector3.one * targetUpscale, elapsedTime / riseDuration);
    //         elapsedTime += Time.deltaTime;
    //         yield return null;
    //     }
    //     currentObject.transform.DOPunchScale(new Vector3(1.1f, 1.1f, 1.1f), 0.5f, 5).Play();
    //     // Start falling into the hole: spin, shrink, and fall
    //     elapsedTime = 0f;
    //     Vector3 targetPosition = new Vector3(holeCenter.x, fallDepth, holeCenter.z);
    //     Vector3 startPosition = currentObject.transform.position;
    //     int iteration = 0;
    //     while (elapsedTime < rotationDuration)
    //     {
    //         // Spin around Y-axis
    //         // currentObject.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

    //         // Move down into the hole
    //         currentObject.transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / rotationDuration);

    //         // Shrink the obstacle

    //         Debug.Log("Current Object Scale :" + currentObject.transform.localScale + "  for iteration " + iteration);
    //         iteration++;
    //         currentObject.transform.localScale = Vector3.Lerp(startScale, Vector3.one * targetScale, elapsedTime / rotationDuration);

    //         elapsedTime += Time.deltaTime;
    //         yield return null;
    //     }

    //     // Ensure it's completely scaled down and out of view
    //     currentObject.transform.localScale = Vector3.one * targetScale;
    //     currentObject.transform.position = targetPosition;

    //     // Optionally, destroy the object after falling in
    //     Destroy(currentObject);
    // }
}
