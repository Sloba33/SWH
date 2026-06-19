using System.Collections.Generic;
using Eflatun.SceneReference;
using UnityEngine;

/// <summary>
/// A complete bot performance for one (fixed) multiplayer level: where it
/// starts, which way it faces, and the ordered list of actions to perform.
///
/// These assets are produced two ways, both consumed by the same
/// <see cref="BotController"/>:
///   1. Hand-authored in the inspector (the "programmed bot" approach).
///   2. Captured by the recorder while a human plays (the "replay" approach) —
///      including harvesting real players to use as opponents for others.
///
/// Positions are stored in the local space of the playback level root (the
/// opponent level's parent — see BotController.levelRoot), not world space. This
/// lets replays be authored relative to the level and keeps them valid when the
/// whole level is moved or rotated. With no level root assigned, positions are
/// interpreted as world space.
/// </summary>
[CreateAssetMenu(fileName = "BotReplay", menuName = "SWH/Bot Replay")]
public class BotReplay : ScriptableObject
{
    [Tooltip("The level scene this replay plays on. Loaded by the bot-match fallback before spawning the bot.")]
    public SceneReference scene;

    [Tooltip("Optional free-form id for the level, for grouping/filtering replays.")]
    public string levelId;

    [Tooltip("Free-form label, e.g. difficulty tier or the player who was recorded.")]
    public string label;

    [Tooltip("Start position, local to the level root (added on top of the root's transform at playback).")]
    public Vector3 startPosition;

    [Tooltip("Initial facing yaw in degrees, relative to the level root's yaw.")]
    public float startYaw;

    [Header("Stats at record time")]
    [Tooltip("Move speed the bot had when this was recorded. Re-applied on playback so the bot reaches the level's " +
             "timed/falling obstacles in sync — otherwise a faster/slower character would drift out of step.")]
    public float moveSpeed = 2f;
    [Tooltip("Strength the bot had when recorded (drives push/pull speed = strength / obstacle weight, capped at move speed). " +
             "Re-applied on playback for the same timing-consistency reason.")]
    public float strength = 10f;

    public List<BotAction> actions = new List<BotAction>();
}
