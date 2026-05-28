using UnityEngine;

/// <summary>
/// Single source of truth for trophy storage and display.
///
/// Single-player trophies are stored under <see cref="SinglePlayerKey"/> and can
/// never be lost. Multiplayer trophies are stored under <see cref="MultiplayerKey"/>,
/// can be won or lost, and are clamped to a non-negative value. Anywhere we display
/// a trophy total to the player we use <see cref="GetDisplayedTrophies"/> so SP and
/// MP earnings appear as one combined number.
/// </summary>
public static class TrophyUtility
{
    public const string SinglePlayerKey = "Trophies";
    public const string MultiplayerKey = "MP_Trophies";

    public static int GetSinglePlayerTrophies()
    {
        return PlayerPrefs.GetInt(SinglePlayerKey, 0);
    }

    public static int GetMultiplayerTrophies()
    {
        return Mathf.Max(0, PlayerPrefs.GetInt(MultiplayerKey, 0));
    }

    public static int GetDisplayedTrophies()
    {
        return GetSinglePlayerTrophies() + GetMultiplayerTrophies();
    }

    /// <summary>
    /// Adds <paramref name="delta"/> (can be negative) to the multiplayer trophy
    /// total. The result is clamped to zero — players can never have negative
    /// MP trophies.
    /// </summary>
    public static void AddMultiplayerTrophies(int delta)
    {
        int newTotal = Mathf.Max(0, GetMultiplayerTrophies() + delta);
        PlayerPrefs.SetInt(MultiplayerKey, newTotal);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Returns the trophies actually lost when a player loses an MP match with a
    /// configured loss amount of <paramref name="requestedLoss"/>. Clamped against
    /// the current MP balance so the visible animation matches what the player
    /// actually loses (e.g. user has 3 MP trophies, configured loss is 5 → 3).
    /// </summary>
    public static int GetEffectiveLossAmount(int requestedLoss)
    {
        return Mathf.Min(Mathf.Max(0, requestedLoss), GetMultiplayerTrophies());
    }
}
