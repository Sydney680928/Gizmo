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
using Terminal.Gui.Views;
using Gizmo;
using Gizmo.Helpers;

namespace Gizmo.Primitives;

internal static class MsgBoxPrimitives
{
    /// <summary>
    /// Pops a msgbox definition record { title text ui.kind 'info'|'confirm' },
    /// shows the TG MessageBox, and pushes a result record { ui.status }.
    /// </summary>
    public static async Task<EvalResult> Show(MogwaiEngine engine, UiContext context)
    {
        var sig = engine.StackSign(1);
        if (sig.Count == 0)
            return EvalResult.Failure(engine, Error.TooFewArgumentsError, "msgbox.show");
        if (sig[0] != typeof(MOGRecord))
            return EvalResult.Failure(engine, Error.BadArgumentTypeError, "msgbox.show");

        var def   = engine.StackPopRecord();
        var kind  = RecordHelper.GetString(def, "ui.kind", "info");
        var title = RecordHelper.GetString(def, "title", "");
        var text  = RecordHelper.GetString(def, "text", "");

        var app = context.App!;

        int pressedStatus = 1;

        if (kind == "confirm")
        {
            int? r = null;
            context.RunWithPump(engine, () => { r = MessageBox.Query(app, title, text, "Yes", "No"); });
            pressedStatus = (r ?? 1) == 0 ? 1 : 2;
        }
        else
        {
            context.RunWithPump(engine, () => MessageBox.Query(app, title, text, "OK"));
            pressedStatus = 1;
        }

        var result = new MOGRecord(engine);
        result.Items["ui.status"] = new MOGNumber(engine, pressedStatus);
        engine.StackPush(result);

        return EvalResult.NoError;
    }
}
