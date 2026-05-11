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
using System.Collections.ObjectModel;
using System.Data;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Gizmo.Helpers;

namespace Gizmo.Builders;

/// <summary>
/// Creates Terminal.Gui views from MOGWAI component definition records.
/// Registers named components in UiContext and wires up action blocks.
/// </summary>
internal static class ComponentFactory
{
    private const int LabelWidth = 18;

    // ── Dispatch ──────────────────────────────────────────────────────────────

    public static View? Create(MOGRecord def, MogwaiEngine engine, UiContext context)
    {
        var kind = RecordHelper.GetString(def, "ui.kind");

        var view = kind switch
        {
            "ui.label" => CreateLabel(def),
            "ui.edit" => CreateEdit(def, engine, context),
            "ui.password" => CreatePassword(def, engine, context),
            "ui.multiline" => CreateMultiline(def, engine, context),
            "ui.integer" => CreateInteger(def, engine, context),
            "ui.float" => CreateFloat(def, engine, context),
            "ui.check" => CreateCheck(def, engine, context),
            "ui.radio" => CreateRadio(def, engine, context),
            "ui.combo" => CreateCombo(def, engine, context),
            "ui.button" => CreateButton(def, engine, context),
            "ui.listview" => CreateListView(def, engine, context),
            "ui.tableview" => CreateTableView(def, engine, context),
            "ui.progress" => CreateProgress(def),
            "ui.frame" => CreateFrame(def, engine, context),
            "ui.separator" => (View)new Line { Orientation = Orientation.Horizontal, Width = Dim.Fill() },
            _ => null
        };

        if (view is not null && kind != "ui.tableview")
        {
            var name = RecordHelper.GetString(def, "name");
            if (!string.IsNullOrEmpty(name))
                context.RegisterComponent(name, view);
        }

        return view;
    }

    /// <summary>
    /// Stacks all child views vertically inside container.
    /// When a component has a label:, the Label and the input view are added
    /// directly to the parent (no wrapper View) so TG v2 focus chain is preserved.
    /// </summary>
    public static void AddChildren(View container, IEnumerable<MOGRecord> childDefs,
        MogwaiEngine engine, UiContext context)
    {
        View? previous = null;

        foreach (var def in childDefs)
        {
            var view = Create(def, engine, context);
            if (view is null) continue;

            var kind = RecordHelper.GetString(def, "ui.kind");
            var y = previous is null ? Pos.Absolute(0) : Pos.Bottom(previous) + 1;
            var labelText = NeedsExternalLabel(kind)
                            ? RecordHelper.GetString(def, "label")
                            : "";

            if (!string.IsNullOrEmpty(labelText))
            {
                var label = new Label
                {
                    Text = labelText,
                    X = Pos.Absolute(0),
                    Y = y,
                    Width = Dim.Absolute(LabelWidth),
                    CanFocus = false
                };
                view.X = Pos.Right(label);
                view.Y = y;
                view.Width = Dim.Fill();

                container.Add(label, view);
                previous = view;
            }
            else if (kind == "ui.button")
            {
                view.X = Pos.Center();
                view.Y = y;
                view.Width = Dim.Auto();

                container.Add(view);
                previous = view;
            }
            else
            {
                view.X = Pos.Absolute(0);
                view.Y = y;

                if (kind is not "ui.check" and not "ui.label")
                    view.Width = Dim.Fill();

                container.Add(view);
                previous = view;
            }
        }
    }

    private static bool NeedsExternalLabel(string kind) => kind switch
    {
        "ui.edit" or "ui.password" or "ui.multiline" or "ui.integer" or "ui.float"
            or "ui.radio" or "ui.combo" => true,
        _ => false
    };

    // ── label ─────────────────────────────────────────────────────────────────

    private static Label CreateLabel(MOGRecord def)
        => new() { Text = RecordHelper.GetString(def, "text"), Width = Dim.Fill() };

    // ── edit ──────────────────────────────────────────────────────────────────

    private static View CreateEdit(MOGRecord def, MogwaiEngine engine, UiContext context)
    {
        var field = new TextField { Text = RecordHelper.GetString(def, "text"), Width = Dim.Fill() };

        if (RecordHelper.GetEvent(def, "onChange") is MOGFunction onChange)
            field.TextChanged += async (_, _) =>
                await ExecuteAction(onChange, engine, BuildEventData(engine, def,
                    text: field.Text?.ToString()), context);

        if (RecordHelper.GetEvent(def, "onValidate") is MOGFunction onValidate)
            field.Accepting += async (_, e) =>
            {
                await ExecuteAction(onValidate, engine, BuildEventData(engine, def,
                    text: field.Text?.ToString()), context);
                e.Handled = true;
            };

        return field;
    }

    // ── password ──────────────────────────────────────────────────────────────

    private static View CreatePassword(MOGRecord def, MogwaiEngine engine, UiContext context)
    {
        return new TextField
        {
            Text = RecordHelper.GetString(def, "text"),
            Secret = true,
            Width = Dim.Fill()
        };
    }

    // ── multiline ─────────────────────────────────────────────────────────────

    private static View CreateMultiline(MOGRecord def, MogwaiEngine engine, UiContext context)
    {
        return new TextView
        {
            Text = RecordHelper.GetString(def, "text"),
            Width = Dim.Fill(),
            Height = Dim.Absolute(RecordHelper.GetInt(def, "height", 4))
        };
    }

    // ── integer ───────────────────────────────────────────────────────────────

    private static View CreateInteger(MOGRecord def, MogwaiEngine engine, UiContext context)
    {
        return new TextField
        {
            Text = RecordHelper.GetInt(def, "value", 0).ToString(),
            Width = Dim.Absolute(12)
        };
    }

    // ── float ─────────────────────────────────────────────────────────────────

    private static View CreateFloat(MOGRecord def, MogwaiEngine engine, UiContext context)
    {
        return new TextField
        {
            Text = RecordHelper.GetNumber(def, "value", 0.0)
                        .ToString("G", System.Globalization.CultureInfo.InvariantCulture),
            Width = Dim.Absolute(16)
        };
    }

    // ── check ─────────────────────────────────────────────────────────────────

    private static View CreateCheck(MOGRecord def, MogwaiEngine engine, UiContext context)
    {
        var cb = new CheckBox
        {
            Text = RecordHelper.GetString(def, "label"),
            Value = RecordHelper.GetBool(def, "checked") ? CheckState.Checked : CheckState.UnChecked
        };

        if (RecordHelper.GetEvent(def, "onChange") is MOGFunction onChange)
            cb.ValueChanged += async (_, _) =>
                await ExecuteAction(onChange, engine, BuildEventData(engine, def), context);

        return cb;
    }

    // ── radio — OptionSelector (vertical) ────────────────────────────────────

    private static View CreateRadio(MOGRecord def, MogwaiEngine engine, UiContext context)
    {
        var options = RecordHelper.GetStringList(def, "options");
        var index = RecordHelper.GetInt(def, "index", 1) - 1; // 1-based → 0-based

        var radio = new OptionSelector
        {
            Labels = [.. options],
            Value = Math.Max(0, index),
            Orientation = Orientation.Vertical,
            Width = Dim.Fill()
        };

        if (RecordHelper.GetEvent(def, "onChange") is MOGFunction onChange)
            radio.ValueChanged += async (_, _) =>
                await ExecuteAction(onChange, engine, BuildEventData(engine, def,
                    index: (radio.Value ?? 0) + 1), context);

        return radio;
    }

    // ── combo — OptionSelector (horizontal) ──────────────────────────────────

    private static View CreateCombo(MOGRecord def, MogwaiEngine engine, UiContext context)
    {
        var options = RecordHelper.GetStringList(def, "options");
        var index = RecordHelper.GetInt(def, "index", 1) - 1;

        var combo = new OptionSelector
        {
            Labels = [.. options],
            Value = Math.Max(0, index),
            Orientation = Orientation.Horizontal,
            Width = Dim.Fill()
        };

        if (RecordHelper.GetEvent(def, "onChange") is MOGFunction onChange)
            combo.ValueChanged += async (_, _) =>
                await ExecuteAction(onChange, engine, BuildEventData(engine, def,
                    index: (combo.Value ?? 0) + 1), context);

        return combo;
    }

    // ── button ────────────────────────────────────────────────────────────────

    private static View CreateButton(MOGRecord def, MogwaiEngine engine, UiContext context)
    {
        var btn = new Button { Text = RecordHelper.GetString(def, "label", "Button") };

        if (RecordHelper.GetEvent(def, "onClick") is MOGFunction onClick)
            btn.Accepting += async (_, e) =>
            {
                await ExecuteAction(onClick, engine, BuildEventData(engine, def), context);
                e.Handled = true;
            };

        return btn;
    }

    // ── listview ──────────────────────────────────────────────────────────────

    private static View CreateListView(MOGRecord def, MogwaiEngine engine, UiContext context)
    {
        var items = RecordHelper.GetStringList(def, "items");
        var lv = new ListView
        {
            Source = new ListWrapper<string>(new ObservableCollection<string>(items)),
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        if (RecordHelper.GetEvent(def, "onSelect") is MOGFunction onSelect)
            lv.ValueChanged += async (_, _) =>
            {
                var idx = lv.Value;
                if (idx != null)
                {
                    var value = idx >= 0 && idx < items.Count ? items[idx.Value] : "";
                    await ExecuteAction(onSelect, engine, BuildEventData(engine, def,
                        index: idx.Value + 1,
                        value: value), context);
                }
            };

        if (RecordHelper.GetEvent(def, "onActivate") is MOGFunction onActivate)
            lv.Accepting += async (_, e) =>
            {
                var idx = lv.SelectedItem ?? 0;
                var value = idx >= 0 && idx < items.Count ? items[idx] : "";
                await ExecuteAction(onActivate, engine, BuildEventData(engine, def,
                    index: idx + 1,
                    value: value), context);
                e.Handled = true;
            };

        return lv;
    }

    // ── tableview ─────────────────────────────────────────────────────────────

    private static View CreateTableView(MOGRecord def, MogwaiEngine engine, UiContext context)
    {
        var columns = RecordHelper.GetStringList(def, "columns");
        var rows = RecordHelper.GetRowList(def, "rows");

        var dt = new DataTable();
        foreach (var col in columns)
            dt.Columns.Add(col);

        foreach (var row in rows)
        {
            var dr = dt.NewRow();
            for (int i = 0; i < Math.Min(row.Count, columns.Count); i++)
                dr[i] = row[i];
            dt.Rows.Add(dr);
        }

        var tv = new TableView
        {
            Table = new DataTableSource(dt),
            FullRowSelect = true,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        tv.BorderStyle = LineStyle.Single;

        // Register tv directly so ui.gprop/ui.sprop can access it
        var name = RecordHelper.GetString(def, "name");
        if (!string.IsNullOrEmpty(name))
            context.RegisterComponent(name, tv);

        if (RecordHelper.GetEvent(def, "onSelect") is MOGFunction onSelect)
            tv.ValueChanged += async (_, _) =>
            {
                var rowIdx = tv.GetAllSelectedCells().FirstOrDefault().Y;
                var rowValues = rowIdx >= 0 && rowIdx < dt.Rows.Count
                    ? dt.Rows[rowIdx].ItemArray.Select(c => c?.ToString() ?? "").ToList()
                    : (List<string>)[];
                await ExecuteAction(onSelect, engine, BuildEventData(engine, def,
                    index: rowIdx + 1,
                    row: rowValues), context);
            };

        if (RecordHelper.GetEvent(def, "onActivate") is MOGFunction onActivate)
            tv.Accepting += async (_, e) =>
            {
                var rowIdx = tv.GetAllSelectedCells().FirstOrDefault().Y;
                var rowValues = rowIdx >= 0 && rowIdx < dt.Rows.Count
                    ? dt.Rows[rowIdx].ItemArray.Select(c => c?.ToString() ?? "").ToList()
                    : (List<string>)[];
                await ExecuteAction(onActivate, engine, BuildEventData(engine, def,
                    index: rowIdx + 1,
                    row: rowValues), context);
                e.Handled = true;
            };

        return tv;
    }

    // ── progress ──────────────────────────────────────────────────────────────

    private static View CreateProgress(MOGRecord def)
    {
        var value = RecordHelper.GetNumber(def, "value", 0);
        var min = RecordHelper.GetNumber(def, "min", 0);
        var max = RecordHelper.GetNumber(def, "max", 100);
        var range = max > min ? max - min : 1;

        return new ProgressBar
        {
            Fraction = (float)((value - min) / range),
            Width = Dim.Fill()
        };
    }

    // ── frame ─────────────────────────────────────────────────────────────────

    private static View CreateFrame(MOGRecord def, MogwaiEngine engine, UiContext context)
    {
        var frame = new FrameView
        {
            Title = RecordHelper.GetString(def, "title"),
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        AddChildren(frame, RecordHelper.GetRecordList(def, "childs"), engine, context);
        return frame;
    }

    // ── Action execution ──────────────────────────────────────────────────────

    /// <summary>
    /// Clones the action MOGFunction, prepends the ui.eventData injection, and executes.
    /// Injection: [eventData record]  'ui.eventData'  STO  [original code…]
    /// </summary>
    internal static async Task<EvalResult> ExecuteAction(MOGFunction func, MogwaiEngine engine,
        MOGRecord eventData, UiContext context)
    {
        var clone = (MOGFunction)func.Clone();
        var sto = engine.GetPrimitive("STO", true)
                    ?? throw new InvalidOperationException("STO primitive not found in engine.");
        clone.Items.Insert(0, sto);
        clone.Items.Insert(0, new MOGName(engine, "ui.eventData"));
        clone.Items.Insert(0, eventData);

        var result = await clone.Execute();
        if (result.IsError)
        {
            context.PumpError = result;
            context.App?.RequestStop();
        }
        return result;
    }

    // ── ui.eventData builder ──────────────────────────────────────────────────

    internal static MOGRecord BuildEventData(MogwaiEngine engine, MOGRecord def,
        string? text = null,
        int index = 0,
        string? value = null,
        List<string>? row = null)
    {
        var rec = new MOGRecord(engine);
        rec.Items["ui.kind"] = new MOGString(engine, RecordHelper.GetString(def, "ui.kind"));
        rec.Items["name"] = new MOGString(engine, RecordHelper.GetString(def, "name"));

        if (text is not null) rec.Items["text"] = new MOGString(engine, text);
        if (index > 0) rec.Items["index"] = new MOGNumber(engine, index);
        if (value is not null) rec.Items["value"] = new MOGString(engine, value);

        if (row is { Count: > 0 })
        {
            var mogRow = new MOGList(engine);
            foreach (var cell in row)
                mogRow.Items.Add(new MOGString(engine, cell));
            rec.Items["row"] = mogRow;
        }

        return rec;
    }

    // ── Property application ──────────────────────────────────────────────────

    /// <summary>
    /// Applies all key/value pairs from rec (except "name") to view.
    /// Must be called on the TG thread.
    /// </summary>
    internal static void ApplyProperties(View view, MOGRecord rec)
    {
        foreach (var (key, val) in rec.Items)
        {
            if (key == "name") continue;
            ApplyProperty(view, key, val);
        }
        view.SetNeedsDraw();
    }

    private static void ApplyProperty(View view, string key, MOGObject val)
    {
        switch (key)
        {
            case "text" when view is TextField tf:
                tf.Text = MogStr(val); break;
            case "text" when view is TextView tv:
                tv.Text = MogStr(val); break;
            case "text" when view is Label lbl:
                lbl.Text = MogStr(val); break;

            case "checked" when view is CheckBox cb && val is MOGBoolean b:
                cb.Value = b.Value ? CheckState.Checked : CheckState.UnChecked; break;

            case "index" when view is ListView lv && val is MOGNumber ni:
                lv.SelectedItem = (int)ni.Value - 1; break;
            case "index" when view is OptionSelector os && val is MOGNumber nos:
                os.Value = (int)nos.Value - 1; break;

            case "value" when view is ProgressBar pb && val is MOGNumber np:
                pb.Fraction = (float)(np.Value / 100.0); break;

            case "items" when view is ListView lv2 && val is MOGList list:
                var newItems = list.Items
                    .Select(i => i is MOGString s ? s.Value : i.ToString() ?? "")
                    .ToList();
                lv2.Source = new ListWrapper<string>(new ObservableCollection<string>(newItems));
                break;
        }
    }

    private static string MogStr(MOGObject val) => val switch
    {
        MOGString s => s.Value,
        MOGName n => n.Value,
        _ => val.ToString() ?? ""
    };
}
