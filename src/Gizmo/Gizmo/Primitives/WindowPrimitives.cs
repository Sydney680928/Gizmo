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
using Terminal.Gui.App;
using Gizmo.Builders;
using Gizmo.Helpers;
using Gizmo;

namespace Gizmo.Primitives;

internal static class WindowPrimitives
{
    // ── window.show ────────────────────────────────────────────────────────────

    /// <summary>
    /// Pops the window definition record, starts Terminal.Gui on a dedicated thread,
    /// and blocks MOGWAI execution until the application exits.
    /// The engine is idle during the await, so action blocks can safely call
    /// RunAsync/Execute from TG event handlers.
    /// </summary>
    public static async Task<EvalResult> Show(MogwaiEngine engine, UiContext context)
    {
        var sig = engine.StackSign(1);
        if (sig.Count == 0)
            return EvalResult.Failure(engine, Error.TooFewArgumentsError, "window.show");
        if (sig[0] != typeof(MOGRecord))
            return EvalResult.Failure(engine, Error.BadArgumentTypeError, "window.show");
        if (!string.IsNullOrEmpty(context.ActiveWindowName))
            return EvalResult.Failure(engine, Error.OperationNotSupportedError, "window.show");

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

                // ── onShow: — after build, before display ─────────────────────────
                if (RecordHelper.GetEvent(windowDef, "onShow") is MOGFunction onShow)
                {
                    var ed = new MOGRecord(engine);
                    ed.SetName("window", context.ActiveWindowName);

                    ComponentFactory.ExecuteAction(onShow, engine, ed, context)
                        .GetAwaiter().GetResult();

                    if (context.PumpError is not null)
                    {
                        app.Dispose();
                        tcs.TrySetResult(context.PumpError);
                        return;
                    }
                }

                context.RunWithPump(engine, window);

                // ── onHide: — after close, before result ──────────────────────────
                if (context.PumpError is null)
                {
                    if (RecordHelper.GetEvent(windowDef, "onHide") is MOGFunction onHide)
                    {
                        var ed = new MOGRecord(engine);
                        ed.SetName("window", context.ActiveWindowName);
                        if (context.CloseStatus is not null)
                            ed.SetItem("status", context.CloseStatus);
                        else
                            ed.SetNull("status");

                        ComponentFactory.ExecuteAction(onHide, engine, ed, context)
                            .GetAwaiter().GetResult();
                    }
                }

                context.ClosedWindowName = context.ActiveWindowName;
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
        var evalResult = await tcs.Task;

        // Push result record onto MOGWAI stack (on MOGWAI thread)
        var closeRecord = new MOGRecord(engine);
        closeRecord.SetName("window", context.ClosedWindowName);
        if (context.CloseStatus is not null)
            closeRecord.SetItem("status", context.CloseStatus);
        else
            closeRecord.SetNull("status");
        engine.StackPush(closeRecord);

        return evalResult;
    }

    // ── window.hide ─────────────────────────────────────────────────────────

    /// <summary>
    /// Pops one mandatory value (any type) from the stack, stores it as CloseStatus,
    /// and stops the TG application. window.show will push a result record
    /// [name: 'xxx' status: value] when it returns.
    /// </summary>
    public static EvalResult Hide(MogwaiEngine engine, UiContext context)
    {
        var sig = engine.StackSign(1);
        if (sig.Count == 0)
            return EvalResult.Failure(engine, Error.TooFewArgumentsError, "window.hide");

        context.CloseStatus = engine.StackPop();
        context.App?.RequestStop();
        return EvalResult.NoError;
    }

    // ── window.active ─────────────────────────────────────────────────────────

    /// <summary>Pushes true if a window is currently displayed, false otherwise.</summary>
    public static async Task<EvalResult> Refresh(MogwaiEngine engine, UiContext context)
    {
        context.ActiveWindow?.SetNeedsDraw();
        await Task.Delay(1); // yield to TG event loop so it can redraw
        return EvalResult.NoError;
    }

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
