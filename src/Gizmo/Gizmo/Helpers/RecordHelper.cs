using MOGWAI.Objects;

namespace Gizmo.Helpers;

/// <summary>
/// Typed accessors for MOGRecord items.
/// MOGWAI record keys are plain strings with no trailing colon.
/// </summary>
internal static class RecordHelper
{
    // ── Scalars ───────────────────────────────────────────────────────────────

    public static string GetString(MOGRecord rec, string key, string defaultValue = "")
    {
        if (!rec.Items.TryGetValue(key, out var val)) return defaultValue;
        return val switch
        {
            MOGString s => s.Value,
            MOGName   n => n.Value,
            _           => defaultValue
        };
    }

    public static double GetNumber(MOGRecord rec, string key, double defaultValue = 0.0)
    {
        if (!rec.Items.TryGetValue(key, out var val)) return defaultValue;
        return val is MOGNumber n ? n.Value : defaultValue;
    }

    public static int GetInt(MOGRecord rec, string key, int defaultValue = 0)
        => (int)GetNumber(rec, key, defaultValue);

    public static bool GetBool(MOGRecord rec, string key, bool defaultValue = false)
    {
        if (!rec.Items.TryGetValue(key, out var val)) return defaultValue;
        return val is MOGBoolean b ? b.Value : defaultValue;
    }

    // ── Collections ───────────────────────────────────────────────────────────

    public static List<string> GetStringList(MOGRecord rec, string key)
    {
        if (!rec.Items.TryGetValue(key, out var val) || val is not MOGList list)
            return [];

        return list.Items
            .Select(i => i switch
            {
                MOGString s => s.Value,
                MOGName   n => n.Value,
                _           => i.ToString() ?? ""
            })
            .ToList();
    }

    public static List<MOGRecord> GetRecordList(MOGRecord rec, string key)
    {
        if (!rec.Items.TryGetValue(key, out var val) || val is not MOGList list)
            return [];

        return list.Items.OfType<MOGRecord>().ToList();
    }

    public static List<List<string>> GetRowList(MOGRecord rec, string key)
    {
        if (!rec.Items.TryGetValue(key, out var val) || val is not MOGList outer)
            return [];

        return outer.Items
            .OfType<MOGList>()
            .Select(row => row.Items
                .Select(cell => cell switch
                {
                    MOGString s  => s.Value,
                    MOGName   n  => n.Value,
                    MOGNumber nm => nm.Value.ToString("G"),
                    _            => cell.ToString() ?? ""
                })
                .ToList())
            .ToList();
    }

    // ── Events ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the MOGFunction for a named event (onClick, onChange, onValidate, onSelect, onActivate),
    /// or null if absent.
    /// </summary>
    public static MOGFunction? GetEvent(MOGRecord rec, string eventName)
    {
        if (!rec.Items.TryGetValue(eventName, out var val)) return null;
        return val as MOGFunction;
    }

    // ── Presence ──────────────────────────────────────────────────────────────

    public static bool HasKey(MOGRecord rec, string key)
        => rec.Items.ContainsKey(key);
}
