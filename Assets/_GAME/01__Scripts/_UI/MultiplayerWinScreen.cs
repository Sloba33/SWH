using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// End-of-match screen shown to the winning player after a multiplayer match.
///
/// Mirrors the single-player <see cref="WinScreen"/> layout but skips
/// SP-only mechanics: no star animator, no level progress fill, no scene
/// progression, no quest panels. XP is awarded the same way as SP wins
/// (read from <c>LastRunXPReward</c> and animated via <see cref="Awarder"/>).
/// Trophies are queued under <see cref="MultiplayerTrophyAwarder.PREF_MP_TROPHY_GAIN"/>
/// and animated on the main menu by <see cref="MultiplayerTrophyAwarder"/>, the
/// same way SP trophy gains flow through <see cref="TrophyAwarder"/>.
/// </summary>
public class MultiplayerWinScreen : MonoBehaviour
{
    [Header("Header")]
    public TextMeshProUGUI titleText;
    public string titleString = "You Won";

    [Header("Reward Panels")]
    public GameObject panelXP;
    public GameObject panelTrophies;
    public TextMeshProUGUI XPText;
    public TextMeshProUGUI TrophyText;
    public AudioSource audioSourceXP;
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

        int xpReward = PlayerPrefs.GetInt("LastRunXPReward", 0);
        int mpTrophyDelta = PlayerPrefs.GetInt(LevelGoal.PREF_LAST_RUN_MP_TROPHY_DELTA, 0);

        if (panelXP != null) panelXP.SetActive(xpReward > 0);
        if (panelTrophies != null) panelTrophies.SetActive(mpTrophyDelta != 0);

        // Consume the delta so it can't accidentally fire again on the next screen.
        PlayerPrefs.SetInt(LevelGoal.PREF_LAST_RUN_MP_TROPHY_DELTA, 0);
        PlayerPrefs.Save();

        if (playerGameObject != null) originalPlayerScale = playerGameObject.localScale;
        StartCoroutine(MoveAndScalePlayerEmoteObject());

        StartCoroutine(AnimateRewards(xpReward, mpTrophyDelta));
    }

    private void SetupPlayerEmote()
    {
        Player player = GameManager.Instance.LocalPlayer;
        if (player == null) return;
        playerEmoteObject = player.gameObject;
        Animator animator = playerEmoteObject.GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetBool("Idle", false);
            animator.Play("Victory_2");
        }
        if (player.EndScreenCamera != null) player.EndScreenCamera.SetActive(true);
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

    private IEnumerator AnimateRewards(int xpReward, int mpTrophyDelta)
    {
        if (XPText != null) XPText.text = "0";
        if (TrophyText != null) TrophyText.text = mpTrophyDelta > 0 ? "+0" : "0";

        // The Awarder component reads "XPGain" in its Start() and runs its own
        // animation, so we relay through PlayerPrefs to stay consistent with
        // the SP flow.
        if (xpReward > 0)
            PlayerPrefs.SetInt("XPGain", PlayerPrefs.GetInt("XPGain") + xpReward);

        if (xpReward > 0) StartCoroutine(TickText(XPText, xpReward, audioSourceXP));

        if (mpTrophyDelta != 0)
        {
            // Queue the delta for the main menu's MultiplayerTrophyAwarder to
            // animate and apply — mirrors how SP wins queue "TrophyGain" for
            // TrophyAwarder. Signed and accumulating so chained "Play Again"
            // runs combine correctly.
            int queued = PlayerPrefs.GetInt(MultiplayerTrophyAwarder.PREF_MP_TROPHY_GAIN, 0);
            PlayerPrefs.SetInt(MultiplayerTrophyAwarder.PREF_MP_TROPHY_GAIN, queued + mpTrophyDelta);
            StartCoroutine(TickText(TrophyText, Mathf.Abs(mpTrophyDelta), audioSourceTrophies, prefix: "+"));
        }

        PlayerPrefs.Save();
        yield break;
    }

    /// <summary>
    /// Drives the small "+N" label next to the XP / trophy icon — mirrors the
    /// SP win screen's blinging counter.
    /// </summary>
    private IEnumerator TickText(TextMeshProUGUI text, int amount, AudioSource sound, string prefix = "")
    {
        if (text == null || amount <= 0) yield break;
        const float blings = 5f;
        float increment = amount / blings;
        float interval = sound != null && sound.clip != null ? (sound.clip.length / blings) / 5f : 0.5f;
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
