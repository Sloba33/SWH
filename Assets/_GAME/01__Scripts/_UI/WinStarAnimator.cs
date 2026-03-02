using UnityEngine;
using DG.Tweening;

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
            starTargets[i].DOScale(new Vector3(1,1,1), 0.4f).Play();
            stars[i].gameObject.SetActive(false);
        }

        for (int i = 0; i < starCount; i++)
        {
            AnimateStar(i);
        }
    }

    void AnimateStar(int index)
    {
        RectTransform star = stars[index];
        RectTransform target = starTargets[index];

        star.gameObject.SetActive(true);

        // Start slightly scaled down
        star.localScale = Vector3.zero;
        star.rotation = Quaternion.identity;

        Sequence seq = DOTween.Sequence();

        seq.AppendInterval(index * staggerDelay);

        // Scale pop-in
        seq.Append(star.DOScale(1.2f, 0.2f).SetEase(Ease.OutBack));
        seq.Append(star.DOScale(1f, 0.1f));

        // Move + rotate simultaneously
        seq.Append(
            star.DOAnchorPos(target.anchoredPosition, animationDuration)
                .SetEase(Ease.InOutCubic)
        );

        star.DORotate(new Vector3(0, 0, 720f), animationDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.OutCubic);

        seq.Play();
    }
}