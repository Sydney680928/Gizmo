// Copyright 2026 Stéphane Sibué
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using MOGWAI.Engine;
using MOGWAI.Objects;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Gizmo.Helpers;

namespace Gizmo.Builders;

/// <summary>
/// Builds and runs modal dialogs from MOGWAI dialog definition records.
/// Uses context.App.Run(dialog) which returns the pressed button index (int?)
/// or null if cancelled (Esc).
/// </summary>
internal static class DialogBuilder
{
    public static MOGRecord ShowModal(MOGRecord def, MogwaiEngine engine, UiContext context)
    {
        var title     = RecordHelper.GetString(def, "title", "Dialog");
        var childDefs = RecordHelper.GetRecordList(def, "childs");

        // Separate buttons row from regular components
        var buttonsDef = childDefs.FirstOrDefault(d =>
            RecordHelper.GetString(d, "ui.kind") == "ui.buttons");
        var compDefs = childDefs
            .Where(d => RecordHelper.GetString(d, "ui.kind") != "ui.buttons")
            .ToList();

        var buttonLabels = buttonsDef is not null
            ? RecordHelper.GetStringList(buttonsDef, "items")
            : (List<string>)["OK"];

        var localContext = new UiContext();

        var dialog = new Dialog
        {
            Title  = title,
            Width  = Dim.Percent(60),
            Height = Dim.Auto()
        };

        ComponentFactory.ApplyColorScheme(dialog, def);
        var dialogScheme = dialog.GetScheme();

        // ── Add components via AddChildren (handles labels + colors) ──────────
        ComponentFactory.AddChildren(dialog, compDefs, engine, localContext, dialogScheme);

        // ── Buttons — AddButton lets app.Run return the button index ──────────
        foreach (var label in buttonLabels)
            dialog.AddButton(new Button { Text = label });

        // ── Run modal with pump via System.Threading.Timer ────────────────────
        // Using App.AddTimeout would crash when called from within a TG callback
        // (e.g. onClick). Instead, use a .NET timer + App.Invoke to marshal
        // MOGWAI execution onto the TG UI thread — keeps timers alive during dialog.
        var emptyCode  = new MOGCode(engine);
        var pumpActive = true;

        using var pumpTimer = new System.Threading.Timer(_ =>
        {
            if (!pumpActive) return;
            context.App?.Invoke(() =>
            {
                if (!pumpActive) return;
                var result = emptyCode.Execute().GetAwaiter().GetResult();
                if (result.IsError)
                {
                    context.PumpError = result;
                    pumpActive = false;
                    context.App?.RequestStop();
                }
                else if (engine.ExitRequested)
                {
                    pumpActive = false;
                    context.App?.RequestStop();
                }
            });
        }, null, dueTime: 0, period: 50);

        int? buttonResult = context.App.Run(dialog) as int?;
        pumpActive = false;
        dialog.Dispose();

        // 1-based status: button 0 → status 1, null/Esc → last button index
        int pressedStatus = buttonResult.HasValue
            ? buttonResult.Value + 1
            : buttonLabels.Count;

        return BuildResult(engine, compDefs, localContext, pressedStatus);
    }

    // ── Result record ─────────────────────────────────────────────────────────

    private static MOGRecord BuildResult(MogwaiEngine engine, List<MOGRecord> compDefs,
        UiContext localContext, int pressedStatus)
    {
        var result = new MOGRecord(engine);
        result.Items["ui.status"] = new MOGNumber(engine, pressedStatus);

        foreach (var def in compDefs)
        {
            var kind = RecordHelper.GetString(def, "ui.kind");
            var name = RecordHelper.GetString(def, "name");

            if (kind is "ui.label" or "ui.separator" || string.IsNullOrEmpty(name)) continue;

            var view = localContext.GetComponent(name);
            if (view is null) continue;

            // Strip "ui." prefix before matching in ExtractValue
            var shortKind = kind.StartsWith("ui.") ? kind[3..] : kind;
            var mogValue = ExtractValue(shortKind, view, engine);
            if (mogValue is not null)
                result.Items[name] = mogValue;
        }

        return result;
    }

    private static MOGObject? ExtractValue(string kind, View view, MogwaiEngine engine)
    {
        return kind switch
        {
            // DropDownList FIRST — hérite de TextField
            "combo" when view is DropDownList ddl
                => new MOGNumber(engine,
                    ((ddl.Data as List<string>) ?? []).IndexOf(ddl.Value?.ToString() ?? "") + 1),

            "edit" or "password" when view is TextField tf
                => new MOGString(engine, tf.Text?.ToString() ?? ""),

            "multiline" when view is TextView tv
                => new MOGString(engine, tv.Text?.ToString() ?? ""),

            "integer" when view is TextField tf
                => int.TryParse(tf.Text?.ToString(), out var i)
                    ? new MOGNumber(engine, i)
                    : new MOGNumber(engine, 0),

            "float" when view is TextField tf
                => double.TryParse(tf.Text?.ToString(),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var d)
                    ? new MOGNumber(engine, d)
                    : new MOGNumber(engine, 0.0),

            "check" when view is CheckBox cb
                => new MOGBoolean(engine, cb.Value == CheckState.Checked),

            "radio" when view is OptionSelector os
                => new MOGNumber(engine, (os.Value ?? 0) + 1),

            _ => null
        };
    }
}
