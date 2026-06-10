using System;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Simple UGUI driver that exposes parameters of
/// <see cref="CoherenceMatchmaker.FindMatchAsync"/> through input fields and
/// dropdowns, and loads the selected map scene when matchmaking succeeds.
/// </summary>
public class MatchmakingTestUI : MonoBehaviour
{
    [Header("Skill Inputs")]
    [SerializeField] private TMP_InputField skillField;
    [SerializeField] private TMP_InputField minOpponentSkillField;
    [SerializeField] private TMP_InputField maxOpponentSkillField;

    [Header("Selection")]
    [SerializeField] private TMP_Dropdown mapDropdown;
    [SerializeField] private TMP_Dropdown regionDropdown;

    [Header("Action")]
    [SerializeField] private Button playButton;
    [SerializeField] private TMP_Text statusText;

    [Header("Defaults")]
    [SerializeField] private int defaultSkill = 100;
    [SerializeField] private int defaultMinOpponentSkill = 80;
    [SerializeField] private int defaultMaxOpponentSkill = 120;

    private CancellationTokenSource _cts;

    private void Awake()
    {
        if (skillField != null)
        {
            skillField.contentType = TMP_InputField.ContentType.IntegerNumber;
            skillField.text = defaultSkill.ToString();
        }
        if (minOpponentSkillField != null)
        {
            minOpponentSkillField.contentType = TMP_InputField.ContentType.IntegerNumber;
            minOpponentSkillField.text = defaultMinOpponentSkill.ToString();
        }
        if (maxOpponentSkillField != null)
        {
            maxOpponentSkillField.contentType = TMP_InputField.ContentType.IntegerNumber;
            maxOpponentSkillField.text = defaultMaxOpponentSkill.ToString();
        }

        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(OnPlayClicked);
        }

        SetStatus("Ready.");
        
    }

    private void OnDestroy()
    {
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
    }

    private void OnPlayClicked()
    {
        if (_cts != null)
        {
            // Already searching.
            return;
        }

        if (!int.TryParse(skillField.text, out int skill))
        {
            skill = defaultSkill;
        }
        if (!int.TryParse(minOpponentSkillField.text, out int minSkill))
        {
            minSkill = defaultMinOpponentSkill;
        }
        if (!int.TryParse(maxOpponentSkillField.text, out int maxSkill))
        {
            maxSkill = defaultMaxOpponentSkill;
        }

        string mapName = mapDropdown != null && mapDropdown.options.Count > 0
            ? mapDropdown.options[mapDropdown.value].text
            : "Zarko_Multiplayer_Test_Collectibles+Falling";

        string region = regionDropdown != null && regionDropdown.options.Count > 0
            ? regionDropdown.options[regionDropdown.value].text
            : "eu";

        if (playButton != null)
        {
            playButton.interactable = false;
        }

        _cts = new CancellationTokenSource();
        _ = RunMatchmakingAsync(mapName, skill, minSkill, maxSkill, region, _cts.Token);
    }

    private async Task RunMatchmakingAsync(
        string mapName,
        int skill,
        int minSkill,
        int maxSkill,
        string region,
        CancellationToken cancellationToken)
    {
        try
        {
            SetStatus($"Searching match on '{mapName}' ({region})...");
            var matchResult = await CoherenceMatchmaker.FindMatchAsync(
                mapName,
                skill,
                minSkill,
                maxSkill,
                region,
                onProgress: SetStatus,
                cancellationToken: cancellationToken);

            SetStatus($"Match found (host={matchResult.IsHost}). Loading '{mapName}'...");
            Debug.Log($"[MatchmakingTestUI] Match ready. Host={matchResult.IsHost} Lobby={matchResult.Lobby?.LobbyData.Id} Room={matchResult.Room.UniqueId}");

            // Load the selected map scene. The scene must be added to Build Settings.
            SceneManager.LoadScene(mapName);
        }
        catch (OperationCanceledException)
        {
            SetStatus("Matchmaking cancelled.");
        }
        catch (ArgumentException e)
        {
            Debug.LogException(e);
            SetStatus($"Invalid input: {e.Message}");
        }
        catch (MatchmakingException e)
        {
            Debug.LogException(e);
            var detail = e.InnerException is { } inner ? $"{e.Message} ({inner.Message})" : e.Message;
            SetStatus($"Matchmaking failed: {detail}");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            SetStatus($"Unexpected error: {e.Message}");
        }
        finally
        {
            if (playButton != null)
            {
                playButton.interactable = true;
            }
            if (_cts != null)
            {
                _cts.Dispose();
                _cts = null;
            }
        }
    }

    private void SetStatus(string text)
    {
        if (statusText != null)
        {
            statusText.text = text;
            Debug.Log("[MatchmakingTestUI] " + text);
        }
    }
    
    public void OnBackButtonClicked()
    {
        if (_cts != null)
        {
            _cts.Cancel();
        }
        SetStatus("Cancelled matchmaking. Returning to main menu...");
        SceneManager.LoadScene(1);
    }
}
