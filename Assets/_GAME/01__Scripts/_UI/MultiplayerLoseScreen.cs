using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// End-of-match screen shown to the losing player after a multiplayer match.
///
/// Shares its layout with <see cref="MultiplayerWinScreen"/> — same panels —
/// but the title says "You Lost", no XP is awarded, and the trophy delta is
/// negative. The screen only renders a "-N" tick; the actual fly-out and
/// counter tick happen on the main menu via
/// <see cref="MultiplayerTrophyAwarder"/>, paralleling the SP flow. If the
/// player has 0 MP trophies, the trophy panel is hidden and nothing is queued.
/// </summary>
public class MultiplayerLoseScreen : MonoBehaviour
{
    [Header("Header")]
    public TextMeshProUGUI titleText;
    public string titleString = "You Lost";

    [Header("Reward Panels")]
    public GameObject panelTrophies;
    public TextMeshProUGUI TrophyText;
    public AudioSource audioSourceTrophies;

    [Header("Buttons")]
    public Button playAgainButton;
    public Button mainMenuButton;

    [Header("Player Emote")]
    public Transform playerGameObject;

    private GameObject playerEmoteObject;
    private Vector3 originalPlayerScale;

    private void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.FreezeObstacles();
        if (AudioManager.Instance != null)
            AudioManager.Instance.BGMVolume = 0.1f;

        SetupPlayerEmote();
        SetupTitle();
        SetupButtons();

        int mpDelta = PlayerPrefs.GetInt(LevelGoal.PREF_LAST_RUN_MP_TROPHY_DELTA, 0);
        int requestedLoss = Mathf.Max(0, -mpDelta);
        int effectiveLoss = TrophyUtility.GetEffectiveLossAmount(requestedLoss);

        // Spec: when the player has 0 MP trophies we hide the panel entirely
        // — no animation, no number change.
        if (panelTrophies != null) panelTrophies.SetActive(effectiveLoss > 0);

        PlayerPrefs.SetInt(LevelGoal.PREF_LAST_RUN_MP_TROPHY_DELTA, 0);
        PlayerPrefs.Save();

        if (playerGameObject != null) originalPlayerScale = playerGameObject.localScale;
        StartCoroutine(MoveAndScalePlayerEmoteObject());

        if (effectiveLoss > 0)
            StartCoroutine(AnimateLoss(effectiveLoss));
    }

    private void SetupPlayerEmote()
    {
        Player player = GameManager.Instance.LocalPlayer;
        if (player == null) return;
        playerEmoteObject = player.gameObject;
        Animator animator = playerEmoteObject.GetComponent<Animator>();
        if (animator != null) animator.Play("Defeat_1");
        if (player.EndScreenCamera != null)
        {
            MultiplayerWinScreen.HideOpponentFromEndScreenCamera(player);
            player.EndScreenCamera.SetActive(true);
        }
    }

    private void SetupTitle()
    {
        if (titleText != null) titleText.text = titleString;
    }

    private void SetupButtons()
    {
        if (playAgainButton != null)
        {
            playAgainButton.onClick.AddListener(GameManager.Instance.DisconnectAndReturnToMatchmaking);
            playAgainButton.gameObject.SetActive(true);
        }
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(GameManager.Instance.DisconnectAndReturnToMainMenu);
            mainMenuButton.gameObject.SetActive(true);
        }
    }

    private IEnumerator AnimateLoss(int effectiveLoss)
    {
        if (TrophyText != null) TrophyText.text = "-0";

        // Queue the (clamped) negative delta for the main menu awarder to
        // animate and apply — matches the SP flow where the win screen queues
        // "TrophyGain" and TrophyAwarder handles storage + animation later.
        int queued = PlayerPrefs.GetInt(MultiplayerTrophyAwarder.PREF_MP_TROPHY_GAIN, 0);
        PlayerPrefs.SetInt(MultiplayerTrophyAwarder.PREF_MP_TROPHY_GAIN, queued - effectiveLoss);

        StartCoroutine(TickText(TrophyText, effectiveLoss, audioSourceTrophies, prefix: "-"));
        yield break;
    }

    private IEnumerator TickText(TextMeshProUGUI text, int amount, AudioSource sound, string prefix = "")
    {
        if (text == null || amount <= 0) yield break;
        const float blings = 5f;
        float increment = amount / blings;
        float interval = sound != null && sound.clip != null ? (sound.clip.length / blings) / 2f : 0.5f;
        float remainder = amount % blings;
        int displayed = 0;
        for (int i = 0; i < blings; i++)
        {
            yield return new WaitForSeconds(interval + 0.1f);
            displayed += Mathf.RoundToInt(increment);
            if (displayed > amount) displayed = amount;
            if (i == blings - 1) displayed += (int)remainder;
            text.text = prefix + displayed;
            if (sound != null) sound.Play();
            text.transform.DOPunchScale(new Vector3(0.1f, 0.1f, 0.1f), interval).Play();
        }
    }

    private IEnumerator MoveAndScalePlayerEmoteObject()
    {
        yield return new WaitForSeconds(1.5f);
        if (playerGameObject == null) yield break;
        float targetX = Screen.width * 0.32f;
        Vector3 pos = playerGameObject.position;
        playerGameObject.DOMove(new Vector3(targetX, pos.y, pos.z), 1f).SetEase(Ease.OutQuad).Play();
        playerGameObject.DOScale(originalPlayerScale * 0.75f, 1f).SetEase(Ease.OutQuad).Play();
    }
}
