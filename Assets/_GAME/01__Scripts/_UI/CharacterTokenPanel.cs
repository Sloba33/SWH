using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using DG.Tweening;
using TMPro;
public class CharacterTokenPanel : MonoBehaviour
{
    public Image characterImage;
    public Image fillAmount;
    public TextMeshProUGUI counter;
    public Image tokenImage;
    public Transform target;
    public Image tokenBubblePrefab;
    public float vicinityRange = 10;
    public CharacterTokenManager characterTokenManager;
    private void Start()
    {
        characterTokenManager = FindObjectOfType<CharacterTokenManager>(true);
    }
    public void SetPortrait(CharacterStats characterStats)
    {
        characterImage.sprite = characterStats.characterPortrait;
    }
    public void GenerateTokens(Transform pileOfCurrency, CharacterStats characterStats, bool full, int amount, TrophyRoadButton trophyRoadButton)
    {
        Debug.Log("+++++++ Generating Tokens +++++++");
        Debug.Log("Character stats : " + characterStats.name);
        Debug.Log("Character stats tokens : " + characterStats.tokensRequired);
        Debug.Log("Tokens : " + PlayerPrefs.GetInt("CharacterTokens", 0));
        characterImage.sprite = characterStats.characterPortrait;

        // Load the saved fill amount from PlayerPrefs
        float savedFillAmount = PlayerPrefs.GetFloat("CharacterFillAmount_" + characterImage.sprite.name, 0f);

        // Set the current fillAmount UI to the saved value
        fillAmount.fillAmount = savedFillAmount;

        float fillTarget = 0f;
        float fillDuration;
        if (full)
        {
            Debug.Log("Is full");
            fillTarget = 1f;
            fillDuration = 1f;
        }
        else
        {
            Debug.Log("Is not full");

            // Calculate the new fill target based on tokens and saved progress
            fillTarget = savedFillAmount + (float)PlayerPrefs.GetInt("CharacterTokens", 0) / characterStats.tokensRequired;
            fillDuration = 2f;
        }

        // Generate token bubbles
        RectTransform[] tokenBubbles = new RectTransform[10];
        int bubbleAmount = amount;
        for (int i = 0; i < bubbleAmount; i++)
        {
            RectTransform tokenBubble = Instantiate(tokenBubblePrefab.rectTransform, pileOfCurrency);
            tokenBubbles[i] = tokenBubble;
            tokenBubble.gameObject.SetActive(true);
            Vector3 randomOffset = new Vector3(
               Random.Range(-2, 2), // Random X
               Random.Range(-2, 2), // Random Y
               Random.Range(1, 1)  // z
           );
            float randomDelay = Random.Range(0, 0.5f);
            Vector3 randomTargetPosition = tokenBubble.position + randomOffset;
            tokenBubble.transform.DOMove(randomTargetPosition, 0.3f).SetDelay(randomDelay).Play();
            Debug.Log("playing ");
        }

        // Animate bubbles and fill the bar
        AnimateBubbles(bubbleAmount, tokenBubbles);
        FillBar(fillTarget, fillDuration, full, trophyRoadButton);
    }
    private void FillBar(float target, float fillDuration, bool full, TrophyRoadButton trophyRoadButton)
    {
        StartCoroutine(FillTokenBar(target, fillDuration, full, trophyRoadButton));
        Debug.Log("Target :" + target + " fill Duration : " + fillDuration + " full? : " + full + "trophyRoadButton " + trophyRoadButton);
    }
    public IEnumerator FillTokenBar(float target, float fillDuration, bool full, TrophyRoadButton trophyRoadButton)
    {
        yield return new WaitForSeconds(1f);
        float startValue = fillAmount.fillAmount;
        float currentDuration = 0f;
        while (currentDuration < fillDuration)
        {
            currentDuration += Time.deltaTime;
            float progress = currentDuration / fillDuration;
            fillAmount.fillAmount = Mathf.Lerp(startValue, target, progress);
            yield return null;
        }

        // Save the updated fill amount to PlayerPrefs
        PlayerPrefs.SetFloat("CharacterFillAmount_" + characterImage.sprite.name, fillAmount.fillAmount);
        PlayerPrefs.Save();

        if (full)
        {
            StartCoroutine(trophyRoadButton.SpawnPanel());
        }
        yield return new WaitForSeconds(1.5f);
        Destroy(this.gameObject);
    }
    private void AnimateBubbles(int bubbleAmount, RectTransform[] tokenBubbles)
    {
        var delay = 0.51f;
        for (int i = 0; i < bubbleAmount; i++)
        {
            tokenBubbles[i]
                .DOScale(1.5f, 0.3f)
                .SetDelay(delay)
                .SetEase(Ease.OutBack)
                .Play();

            tokenBubbles[i]
               .GetComponent<RectTransform>()
               .DOMove(target.transform.position, 0.8f)
               .SetDelay(delay + 0.5f)
               .SetEase(Ease.InBack)
               .Play();

            tokenBubbles[i]
               .DORotate(Vector3.zero, 0.5f)
               .SetDelay(delay + 0.5f)
               .SetEase(Ease.Flash)
               .Play();

            tokenBubbles[i]
              .DOScale(0f, 0.2f)
              .SetDelay(delay + 1.30f)
              .SetEase(Ease.OutBack)
              .Play();

            delay += 0.05f;

            counter
                .transform.parent.GetChild(0)
                .transform.DOScale(1.1f, 0.1f)
                .SetLoops(10, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetDelay(1.5f)
                .Play();
            target
                .transform.DOScale(1.4f, 0.05f)
                .SetLoops(10, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetDelay(1.5f)
                .Play();

        }
    }
}
