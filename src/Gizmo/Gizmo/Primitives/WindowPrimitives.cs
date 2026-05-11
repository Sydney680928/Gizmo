using MOGWAI.Engine;
using MOGWAI.Objects;
using Terminal.Gui.App;
using Gizmo.Builders;
using Gizmo.Helpers;
using Gizmo;

namespace Gizmo.Primitives;

internal static class WindowPrimitives
{
    // ── window.run ────────────────────────────────────────────────────────────

    /// <summary>
    /// Pops the window definition record, starts Terminal.Gui on a dedicated thread,
    /// and blocks MOGWAI execution until the application exits.
    /// The engine is idle during the await, so action blocks can safely call
    /// RunAsync/Execute from TG event handlers.
    /// </summary>
    public static async Task<EvalResult> Run(MogwaiEngine engine, UiContext context)
    {
        var sig = engine.StackSign(1);
        if (sig.Count == 0)
            return EvalResult.Failure(engine, Error.TooFewArgumentsError, "window.run");
        if (sig[0] != typeof(MOGRecord))
            return EvalResult.Failure(engine, Error.BadArgumentTypeError, "window.run");

        var windowDef = engine.StackPopRecord();

        context.Reset();
        context.ActiveWindowName = RecordHelper.GetString(windowDef, "name");
        var tcs = new TaskCompletionSource<EvalResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        context.AppTcs = tcs;

        var tgThread = new Thread(() =>
        {
            try
            {
                using IApplication app = Application.Create().Init();
                context.App = app;

                var window = WindowBuilder.Build(windowDef, engine, context);
                context.RunWithPump(engine, window);
                context.ActiveWindowName = "";
                app.Dispose();

                if (context.PumpError is not null)
                    tcs.TrySetResult(context.PumpError);
                else
                    tcs.TrySetResult(EvalResult.NoError);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        })
        {
            Name         = "TG-Main",
            IsBackground = true
        };

        tgThread.Start();
        return await tcs.Task;
    }

    // ── window.active ─────────────────────────────────────────────────────────

    /// <summary>Pushes true if a window is currently displayed, false otherwise.</summary>
    public static EvalResult Active(MogwaiEngine engine, UiContext context)
    {
        engine.StackPushBoolean(!string.IsNullOrEmpty(context.ActiveWindowName));
        return EvalResult.NoError;
    }

    // ── window.current ────────────────────────────────────────────────────────

    /// <summary>Pushes the name of the currently displayed window, or "" if none.</summary>
    public static EvalResult Current(MogwaiEngine engine, UiContext context)
    {
        engine.StackPushName(context.ActiveWindowName);
        return EvalResult.NoError;
    }

    /// <summary>
    /// Pops a record { name "x" property value … } and applies the properties
    /// to the named component.
    /// window.update is always called from an action block (TG thread),
    /// so no marshaling is needed.
    /// </summary>
    public static EvalResult Update(MogwaiEngine engine, UiContext context)
    {
        var sig = engine.StackSign(1);
        if (sig.Count == 0)
            return EvalResult.Failure(engine, Error.TooFewArgumentsError, "window.update");
        if (sig[0] != typeof(MOGRecord))
            return EvalResult.Failure(engine, Error.BadArgumentTypeError, "window.update");

        var rec  = engine.StackPopRecord();
        var name = RecordHelper.GetString(rec, "name");

        if (string.IsNullOrEmpty(name))
            return EvalResult.Failure(engine, Error.BadArgumentValueError, "window.update");

        var view = context.GetComponent(name);
        if (view is null)
            return EvalResult.Failure(engine, Error.BadArgumentValueError, "window.update");

        ComponentFactory.ApplyProperties(view, rec);
        return EvalResult.NoError;
    }
}
