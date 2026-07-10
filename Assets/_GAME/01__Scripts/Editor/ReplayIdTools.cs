using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor tooling for the replay identity system.
///
///  • Assign Replay IDs — adds a <see cref="ReplayId"/> to every trackable entity
///    under each <see cref="ReplayScope"/> in the open scene and assigns IDs in
///    deterministic hierarchy order (sibling-index chain). Existing IDs are
///    preserved so additive level edits don't invalidate old replays; only
///    missing/duplicate ones get new IDs.
///  • Mirror Replay IDs From Source (context menu on ReplayScope) — copies IDs
///    from the scope's mirrorSource onto entities at the same relative hierarchy
///    path. Because the two halves of an MP level are duplicates, this is what
///    makes "record on one half, play back on the other" resolve correctly.
///  • Validate Replay IDs — reports unassigned IDs, duplicates, and trackable
///    entities with no ReplayId.
/// </summary>
public static class ReplayIdTools
{
    // Component types that the replay system tracks. Extend as new trackable
    // entity kinds appear (kept here, in one place, on purpose).
    private static readonly System.Type[] TrackableTypes =
    {
        typeof(Obstacle),
        typeof(CollectibleItem), // abstract base: covers powerups, bombs, etc.
        typeof(Rocket),
    };

    // ------------------------------------------------------------------ Assign

    [MenuItem("Tools/SWH/Replay/Assign Replay IDs In Scene")]
    private static void AssignInScene()
    {
        ReplayScope[] scopes = Object.FindObjectsByType<ReplayScope>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (scopes.Length == 0)
        {
            Debug.LogWarning("[ReplayIdTools] No ReplayScope found in the scene. Add one to each level root (playerLevel / opponentLevel) first.");
            return;
        }
        foreach (ReplayScope scope in scopes) AssignInScope(scope);
    }

    private static void AssignInScope(ReplayScope scope)
    {
        List<GameObject> trackables = FindTrackables(scope);

        // Ensure every trackable has a ReplayId component.
        var ids = new List<ReplayId>(trackables.Count);
        foreach (GameObject go in trackables)
        {
            ReplayId rid = go.GetComponent<ReplayId>();
            if (rid == null)
            {
                rid = Undo.AddComponent<ReplayId>(go);
            }
            ids.Add(rid);
        }

        // Deterministic order: sibling-index chain from the scope root. Identical
        // half-layouts (duplicated hierarchies) therefore enumerate identically.
        ids.Sort((a, b) => CompareSiblingChains(
            SiblingChain(a.transform, scope.transform),
            SiblingChain(b.transform, scope.transform)));

        // Preserve valid existing IDs; collect what's taken.
        var taken = new HashSet<int>();
        var needsId = new List<ReplayId>();
        foreach (ReplayId rid in ids)
        {
            if (rid.id > 0 && rid.id < ReplayScope.RuntimeIdStart && taken.Add(rid.id)) continue;
            needsId.Add(rid); // unassigned, duplicate, or in the runtime range
        }

        int next = taken.Count > 0 ? taken.Max() + 1 : 1;
        foreach (ReplayId rid in needsId)
        {
            Undo.RecordObject(rid, "Assign Replay ID");
            rid.id = next++;
            EditorUtility.SetDirty(rid);
        }

        Debug.Log($"[ReplayIdTools] Scope '{scope.name}': {ids.Count} trackables, " +
                  $"{needsId.Count} newly assigned, {ids.Count - needsId.Count} preserved.", scope);
    }

    // ------------------------------------------------------------------ Mirror

    [MenuItem("CONTEXT/ReplayScope/Mirror Replay IDs From Source")]
    private static void MirrorFromSource(MenuCommand command)
    {
        var target = (ReplayScope)command.context;
        if (target.mirrorSource == null)
        {
            Debug.LogError("[ReplayIdTools] Set 'Mirror Source' on this ReplayScope first (the other level half).", target);
            return;
        }
        ReplayScope source = target.mirrorSource;

        // Path (sibling-index chain) → id, from the source half.
        var sourceIdsByPath = new Dictionary<string, int>();
        foreach (GameObject go in FindTrackables(source))
        {
            ReplayId rid = go.GetComponent<ReplayId>();
            if (rid == null || rid.id == 0)
            {
                Debug.LogWarning($"[ReplayIdTools] Source entity '{go.name}' has no assigned ReplayId — run Assign on the source scope first.", go);
                continue;
            }
            sourceIdsByPath[PathKey(go.transform, source.transform)] = rid.id;
        }

        int matched = 0, unmatched = 0;
        foreach (GameObject go in FindTrackables(target))
        {
            string key = PathKey(go.transform, target.transform);
            if (!sourceIdsByPath.TryGetValue(key, out int id))
            {
                unmatched++;
                Debug.LogWarning($"[ReplayIdTools] No source match for '{go.name}' (path {key}) — halves differ here.", go);
                continue;
            }
            ReplayId rid = go.GetComponent<ReplayId>() ?? Undo.AddComponent<ReplayId>(go);
            if (rid.id != id)
            {
                Undo.RecordObject(rid, "Mirror Replay ID");
                rid.id = id;
                EditorUtility.SetDirty(rid);
            }
            matched++;
        }

        Debug.Log($"[ReplayIdTools] Mirrored '{source.name}' → '{target.name}': {matched} matched, {unmatched} unmatched.", target);
    }

    // ---------------------------------------------------------------- Validate

    [MenuItem("Tools/SWH/Replay/Validate Replay IDs In Scene")]
    private static void ValidateInScene()
    {
        ReplayScope[] scopes = Object.FindObjectsByType<ReplayScope>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (scopes.Length == 0)
        {
            Debug.LogWarning("[ReplayIdTools] No ReplayScope found in the scene.");
            return;
        }

        foreach (ReplayScope scope in scopes)
        {
            var report = new StringBuilder($"[ReplayIdTools] Validation of scope '{scope.name}':\n");
            bool clean = true;

            var seen = new Dictionary<int, ReplayId>();
            foreach (GameObject go in FindTrackables(scope))
            {
                ReplayId rid = go.GetComponent<ReplayId>();
                if (rid == null) { report.AppendLine($"  MISSING ReplayId: '{go.name}'"); clean = false; continue; }
                if (rid.id == 0) { report.AppendLine($"  UNASSIGNED id: '{go.name}'"); clean = false; continue; }
                if (rid.id >= ReplayScope.RuntimeIdStart) { report.AppendLine($"  RUNTIME-RANGE id {rid.id} serialized on '{go.name}' (should be authored)"); clean = false; continue; }
                if (seen.TryGetValue(rid.id, out ReplayId dup)) { report.AppendLine($"  DUPLICATE id {rid.id}: '{dup.name}' and '{go.name}'"); clean = false; continue; }
                seen.Add(rid.id, rid);
            }

            report.Append(clean ? $"  OK — {seen.Count} entities." : "  Run Assign Replay IDs to fix.");
            if (clean) Debug.Log(report.ToString(), scope);
            else Debug.LogWarning(report.ToString(), scope);
        }
    }

    // -------------------------------------------------------------------- List

    [MenuItem("Tools/SWH/Replay/List Replay IDs In Scene")]
    private static void ListInScene()
    {
        ReplayScope[] scopes = Object.FindObjectsByType<ReplayScope>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (scopes.Length == 0)
        {
            Debug.LogWarning("[ReplayIdTools] No ReplayScope found in the scene.");
            return;
        }

        foreach (ReplayScope scope in scopes)
        {
            var ids = new List<ReplayId>();
            foreach (GameObject go in FindTrackables(scope))
            {
                ReplayId rid = go.GetComponent<ReplayId>();
                if (rid != null) ids.Add(rid);
            }
            ids.Sort((a, b) => a.id.CompareTo(b.id));

            Debug.Log($"[ReplayIdTools] ─── Scope '{scope.name}': {ids.Count} entities ───", scope);
            foreach (ReplayId rid in ids)
            {
                // One entry per id, with the ReplayId as context so clicking the
                // console line pings/selects that object in the hierarchy.
                Debug.Log($"[ReplayIdTools] [{scope.name}] id {rid.id}: '{rid.name}'", rid);
            }
        }
    }

    // ----------------------------------------------------------------- Helpers

    /// <summary>
    /// GameObjects under the scope that carry any trackable component (one entry
    /// per GameObject even if it has several). Includes inactive objects.
    /// </summary>
    private static List<GameObject> FindTrackables(ReplayScope scope)
    {
        var result = new List<GameObject>();
        var seen = new HashSet<GameObject>();
        foreach (System.Type type in TrackableTypes)
        {
            foreach (Component c in scope.GetComponentsInChildren(type, true))
            {
                if (seen.Add(c.gameObject)) result.Add(c.gameObject);
            }
        }
        return result;
    }

    /// <summary>Sibling-index chain from the scope root down to t (structural position, name-independent).</summary>
    private static List<int> SiblingChain(Transform t, Transform root)
    {
        var chain = new List<int>();
        while (t != null && t != root)
        {
            chain.Add(t.GetSiblingIndex());
            t = t.parent;
        }
        chain.Reverse();
        return chain;
    }

    private static int CompareSiblingChains(List<int> a, List<int> b)
    {
        int n = Mathf.Min(a.Count, b.Count);
        for (int i = 0; i < n; i++)
        {
            int cmp = a[i].CompareTo(b[i]);
            if (cmp != 0) return cmp;
        }
        return a.Count.CompareTo(b.Count);
    }

    private static string PathKey(Transform t, Transform root) =>
        string.Join(".", SiblingChain(t, root));
}
