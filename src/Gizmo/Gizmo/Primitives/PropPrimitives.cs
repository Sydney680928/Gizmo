using Gizmo.Builders;
using Gizmo.Helpers;
using MOGWAI.Engine;
using MOGWAI.Objects;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Gizmo.Primitives;

internal static class PropPrimitives
{
    // ── ui.gprop ──────────────────────────────────────────────────────────────

    public static EvalResult Get(MogwaiEngine engine, UiContext context)
    {
        var sig = engine.StackSign(1);
        if (sig.Count == 0)
            return EvalResult.Failure(engine, Error.TooFewArgumentsError, "ui.gprop");
        if (sig[0] != typeof(MOGRecord))
            return EvalResult.Failure(engine, Error.BadArgumentTypeError, "ui.gprop");

        var rec = engine.StackPopRecord();
        var name = RecordHelper.GetString(rec, "name");
        var prop = RecordHelper.GetString(rec, "prop");

        if (string.IsNullOrEmpty(name))
            return EvalResult.Failure(engine, Error.BadArgumentValueError, "ui.gprop");

        var view = context.GetComponent(name);
        if (view is null)
            return EvalResult.Failure(engine, Error.BadArgumentValueError, "ui.gprop");

        if (string.IsNullOrEmpty(prop))
            engine.StackPush(ExtractAllProperties(view, engine));
        else
            engine.StackPush(ExtractProperty(view, prop, engine) ?? (MOGObject)new MOGString(engine, ""));

        return EvalResult.NoError;
    }

    // ── ui.sprop ──────────────────────────────────────────────────────────────

    public static EvalResult Set(MogwaiEngine engine, UiContext context)
    {
        var sig = engine.StackSign(1);
        if (sig.Count == 0)
            return EvalResult.Failure(engine, Error.TooFewArgumentsError, "ui.sprop");
        if (sig[0] != typeof(MOGRecord))
            return EvalResult.Failure(engine, Error.BadArgumentTypeError, "ui.sprop");

        var rec = engine.StackPopRecord();
        var name = RecordHelper.GetString(rec, "name");

        if (string.IsNullOrEmpty(name))
            return EvalResult.Failure(engine, Error.BadArgumentValueError, "ui.sprop");

        var view = context.GetComponent(name);
        if (view is null)
            return EvalResult.Failure(engine, Error.BadArgumentValueError, "ui.sprop");

        ComponentFactory.ApplyProperties(view, rec);
        return EvalResult.NoError;
    }

    // ── Property extraction ───────────────────────────────────────────────────

    private static MOGRecord ExtractAllProperties(View view, MogwaiEngine engine)
    {
        var rec = new MOGRecord(engine);

        switch (view)
        {
            // DropDownList FIRST — hérite de TextField
            case DropDownList ddl:
                var ddlText = ddl.Value?.ToString() ?? "";
                var ddlOpts = ddl.Data as List<string> ?? [];
                rec.Items["index"] = new MOGNumber(engine, ddlOpts.IndexOf(ddlText) + 1);
                rec.Items["value"] = new MOGString(engine, ddlText); break;
            case TextField tf:
                rec.Items["text"] = new MOGString(engine, tf.Text?.ToString() ?? ""); break;
            case TextView tv:
                rec.Items["text"] = new MOGString(engine, tv.Text?.ToString() ?? ""); break;
            case Label lbl:
                rec.Items["text"] = new MOGString(engine, lbl.Text?.ToString() ?? ""); break;
            case CheckBox cb:
                rec.Items["checked"] = new MOGBoolean(engine, cb.Value == CheckState.Checked); break;
            case OptionSelector os:
                rec.Items["index"] = new MOGNumber(engine, (os.Value ?? 0) + 1); break;
            case ListView lv:
                rec.Items["index"] = new MOGNumber(engine, (lv.SelectedItem ?? 0) + 1); break;
            case ProgressBar pb:
                rec.Items["value"] = new MOGNumber(engine, pb.Fraction * 100.0); break;
        }

        return rec;
    }

    private static MOGObject? ExtractProperty(View view, string prop, MogwaiEngine engine)
    {
        // DropDownList FIRST — hérite de TextField
        if (view is DropDownList ddl)
        {
            var ddlText = ddl.Value?.ToString() ?? "";
            var ddlOpts = ddl.Data as List<string> ?? [];
            return prop switch
            {
                "index" => new MOGNumber(engine, ddlOpts.IndexOf(ddlText) + 1),
                "value" => new MOGString(engine, ddlText),
                _ => null
            };
        }

        return (prop, view) switch
        {
            ("text", TextField tf) => new MOGString(engine, tf.Text?.ToString() ?? ""),
            ("text", TextView tv) => new MOGString(engine, tv.Text?.ToString() ?? ""),
            ("text", Label lbl) => new MOGString(engine, lbl.Text?.ToString() ?? ""),
            ("checked", CheckBox cb) => new MOGBoolean(engine, cb.Value == CheckState.Checked),
            ("index", OptionSelector os) => new MOGNumber(engine, (os.Value ?? 0) + 1),
            ("index", ListView lv) => new MOGNumber(engine, (lv.SelectedItem ?? 0) + 1),
            ("value", ProgressBar pb) => new MOGNumber(engine, pb.Fraction * 100.0),
            _ => null
        };
    }
}
