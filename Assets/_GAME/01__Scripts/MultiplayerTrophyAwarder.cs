using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Multiplayer counterpart to <see cref="TrophyAwarder"/>. Lives on the main
/// menu and runs on <see cref="Start"/> when the player returns from a
/// multiplayer match, mirroring the single-player flow:
///
/// * <see cref="MultiplayerWinScreen"/> / <see cref="MultiplayerLoseScreen"/>
///   write a signed delta to <see cref="PREF_MP_TROPHY_GAIN"/>.
/// * On the next main-menu load this component reads that delta, animates a
///   pile of trophies into (gain) or out of (loss) the counter, ticks the
///   visible total, applies the storage change via <see cref="TrophyUtility"/>,
///   and clears the pref.
///
/// Display rules:
/// * The counter always shows the combined SP + MP total.
/// * On a positive delta: trophies fly into the counter, total ticks up.
/// * On a negative delta: trophies fly out of the counter, fade away, total
///   ticks down. Loss is clamped against the MP balance so MP trophies can
///   never go negative; if the effective loss is 0 nothing animates.
/// </summary>
public class MultiplayerTrophyAwarder : MonoBehaviour
{
    /// <summary>
    /// Signed delta queued by the multiplayer end screens for the next main
    /// menu load to animate. Positive = gain, negative = loss. Accumulates
    /// across matches if the player chains "Play Again" without ever returning
    /// to the main menu.
    /// </summary>
    public const string PREF_MP_TROPHY_GAIN = "MP_TrophyGain";

    public AudioSource audioSource;
    public TrophyRoadManager trophyRoadManager;
    public TextMeshProUGUI trophyCountText;
    public Image fillBar;
    public Transform target;
    public RectTransform startPosition;
    public GameObject pileOfCurrency;
    public TextMeshProUGUI counter;

    private void Start()
    {
        if (trophyCountText != null)
            trophyCountText.text = TrophyUtility.GetDisplayedTrophies().ToString();

        if (fillBar != null && trophyRoadManager != null && trophyRoadManager.currentFillBar != null)
            fillBar.fillAmount = trophyRoadManager.currentFillBar.fillBar.fillAmount;

        int delta = PlayerPrefs.GetInt(PREF_MP_TROPHY_GAIN, 0);
        if (delta == 0) return;

        if (delta > 0)
        {
            StartCoroutine(RunGain(delta));
            return;
        }

        int effectiveLoss = TrophyUtility.GetEffectiveLossAmount(-delta);
        if (effectiveLoss <= 0)
        {
            // Nothing to lose — drop the queued delta so it can't fire later.
            PlayerPrefs.SetInt(PREF_MP_TROPHY_GAIN, 0);
            PlayerPrefs.Save();
            return;
        }
        StartCoroutine(RunLoss(effectiveLoss));
    }

    private IEnumerator RunGain(int amount)
    {
        yield return new WaitForSeconds(0.2f);
        int displayCount = Mathf.Clamp(GetVisualSpawnCount(amount), 1, pileOfCurrency.transform.childCount);
        AnimateInflow(displayCount);
        StartCoroutine(TickCounter(amount, isGain: true));
    }

    private IEnumerator RunLoss(int amount)
    {
        yield return new WaitForSeconds(0.2f);
        int displayCount = Mathf.Clamp(GetVisualSpawnCount(amount), 1, pileOfCurrency.transform.childCount);
        AnimateOutflow(displayCount);
        StartCoroutine(TickCounter(amount, isGain: false));
    }

    private int GetVisualSpawnCount(int amount)
    {
        if (amount <= 5) return 5;
        if (amount <= 10) return 10;
        return 15;
    }

    private void AnimateInflow(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Transform child = pileOfCurrency.transform.GetChild(i);
            Vector3 restPos = child.GetComponent<RectTransform>().position;
            child.GetComponent<RectTransform>().position = startPosition.position;
            child.gameObject.SetActive(true);
            child.DOMove(restPos, 0.4f).Play();
        }

        float delay = 0f;
        for (int i = 0; i < count; i++)
        {
            Transform child = pileOfCurrency.transform.GetChild(i);
            child.DOScale(1.2f, 0.3f).SetDelay(delay).SetEase(Ease.OutBack).Play();
            child.GetComponent<RectTransform>()
                 .DOMove(target.position, 0.8f)
                 .SetDelay(delay + 0.5f)
                 .SetEase(Ease.InBack)
                 .Play();
            child.DORotate(Vector3.zero, 0.5f).SetDelay(delay + 0.5f).SetEase(Ease.Flash).Play();
            child.DOScale(0f, 0.2f).SetDelay(delay + 1.30f).SetEase(Ease.OutBack).Play();
            delay += 0.05f;
        }

        if (counter != null)
            counter.transform.DOScale(1.1f, 0.1f).SetLoops(10, LoopType.Yoyo).SetEase(Ease.InOutSine).SetDelay(1.2f).Play();
        if (target != null)
            target.transform.DOScale(1.4f, 0.05f).SetLoops(10, LoopType.Yoyo).SetEase(Ease.InOutSine).SetDelay(1.3f).Play();
    }

    private void AnimateOutflow(int count)
    {
        // Trophies start at the counter, drift to a scattered position, then
        // scale down and fade out.
        for (int i = 0; i < count; i++)
        {
            Transform child = pileOfCurrency.transform.GetChild(i);
            RectTransform childRect = child.GetComponent<RectTransform>();
            childRect.position = target.position;
            child.localScale = Vector3.one;
            child.gameObject.SetActive(true);
            SetChildAlpha(child, 1f);
        }

        float delay = 0f;
        for (int i = 0; i < count; i++)
        {
            Transform child = pileOfCurrency.transform.GetChild(i);
            RectTransform childRect = child.GetComponent<RectTransform>();
            Vector3 scatter = target.position + (Vector3)(Random.insideUnitCircle.normalized * 220f);

            childRect.DOMove(scatter, 0.7f).SetDelay(delay).SetEase(Ease.OutQuad).Play();
            child.DOScale(1.3f, 0.2f).SetDelay(delay).SetEase(Ease.OutBack).Play();
            child.DOScale(0f, 0.4f).SetDelay(delay + 0.4f).SetEase(Ease.InBack).Play();
            FadeChild(child, 0f, 0.5f, delay + 0.3f);
            delay += 0.05f;
        }

        if (counter != null)
            counter.transform.DOScale(0.9f, 0.1f).SetLoops(10, LoopType.Yoyo).SetEase(Ease.InOutSine).SetDelay(0.2f).Play();
    }

    private static void SetChildAlpha(Transform child, float alpha)
    {
        Image[] images = child.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            Color c = images[i].color;
            c.a = alpha;
            images[i].color = c;
        }
    }

    private static void FadeChild(Transform child, float endAlpha, float duration, float delay)
    {
        Image[] images = child.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            images[i].DOFade(endAlpha, duration).SetDelay(delay).Play();
        }
    }

    private IEnumerator TickCounter(int amount, bool isGain)
    {
        yield return new WaitForSeconds(1.13f);

        int signedDelta = isGain ? amount : -amount;

        // Apply storage and clear the queued delta up front so the awarder
        // can't accidentally re-fire on the next scene load.
        TrophyUtility.AddMultiplayerTrophies(signedDelta);
        PlayerPrefs.SetInt(PREF_MP_TROPHY_GAIN, 0);
        PlayerPrefs.Save();

        int endDisplay = TrophyUtility.GetDisplayedTrophies();
        int startDisplay = endDisplay - signedDelta;

        float waitTime = amount > 40 ? 0.02f : amount > 20 ? 0.05f : 0.1f;

        if (trophyRoadManager != null) trophyRoadManager.GenerateEverything();

        int displayed = startDisplay;
        for (int i = 0; i < amount; i++)
        {
            displayed += isGain ? 1 : -1;
            if (trophyCountText != null) trophyCountText.text = displayed.ToString();
            if (audioSource != null) audioSource.Play();
            yield return new WaitForSeconds(waitTime);
        }

        if (target != null) target.localScale = Vector3.one;
    }
}
