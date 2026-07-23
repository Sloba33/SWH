using UnityEngine;
using DG.Tweening;
using System.Collections;

public class WinStarAnimator : MonoBehaviour
{
    public RectTransform[] starTargets;   // Slot positions
    public RectTransform[] stars;         // Flying stars
    public WinScreen winScreen;

    public float animationDuration = 1f;
    public float staggerDelay = 0.15f;


    public void Initialize(int starCount)
    {

        Debug.Log("--WINSTARANIMATOR-- Star Count: " + starCount);
        for (int i = 0; i < stars.Length; i++)
        {
            starTargets[i].gameObject.SetActive(true);
            starTargets[i].DOScale(new Vector3(1, 1, 1), 0.4f).Play();
            stars[i].gameObject.SetActive(false);

        }

        for (int i = 0; i < starCount; i++)
        {
            AnimateStar(i);
            StartCoroutine(PlayClickSoundWithDelay(i * staggerDelay));
        }
    }

    void AnimateStar(int index)
{
    RectTransform star = stars[index];
    RectTransform target = starTargets[index];

    star.gameObject.SetActive(true);
    star.localScale = Vector3.zero;
    star.rotation = Quaternion.identity;

    // Store start position for distance checking
    Vector2 startPos = star.anchoredPosition;
    bool soundPlayed = false;

    Sequence seq = DOTween.Sequence();

    seq.AppendInterval(index * staggerDelay);

    // Scale pop-in
    seq.Append(star.DOScale(1.2f, 0.2f).SetEase(Ease.OutBack));
    seq.Append(star.DOScale(1f, 0.1f));

    // Move + rotate simultaneously (EXACTLY like your original)
    seq.Append(
        star.DOAnchorPos(target.anchoredPosition, animationDuration)
            .SetEase(Ease.InOutCubic)
            .OnUpdate(() => {
                // Check if star has traveled 90% of the distance
                if (!soundPlayed)
                {
                    float distanceTraveled = Vector2.Distance(startPos, star.anchoredPosition);
                    float totalDistance = Vector2.Distance(startPos, target.anchoredPosition);
                    
                    if (totalDistance > 0 && distanceTraveled / totalDistance >= 0.9f)
                    {
                        soundPlayed = true;
                        PlayStarSound();
                    }
                }
            })
    );

    // Rotation - EXACTLY like your original (outside the Append)
    star.DORotate(new Vector3(0, 0, 720f), animationDuration, RotateMode.FastBeyond360)
        .SetEase(Ease.OutCubic);

    seq.Play();
}
    void PlayStarSound()
    {
        AudioManager.Instance.PlayUISound("trophy_levelup");
    }
    IEnumerator PlayClickSoundWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        AudioManager.Instance.PlayUISound("click1");
    }
}