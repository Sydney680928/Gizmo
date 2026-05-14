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
using Gizmo.Builders;
using Gizmo;

namespace Gizmo.Primitives;

internal static class DialogPrimitives
{
    /// <summary>
    /// Pops a dialog definition record, shows a modal dialog via context.App.Run(),
    /// and pushes the result record.
    /// Must be called from the TG thread (inside an action block).
    /// </summary>
    public static async Task<EvalResult> Show(MogwaiEngine engine, UiContext context)
    {
        var sig = engine.StackSign(1);
        if (sig.Count == 0)
            return EvalResult.Failure(engine, Error.TooFewArgumentsError, "dialog.show");
        if (sig[0] != typeof(MOGRecord))
            return EvalResult.Failure(engine, Error.BadArgumentTypeError, "dialog.show");

        var dialogDef = engine.StackPopRecord();

        if (context.App is null)
            return EvalResult.Failure(engine, Error.OperationNotSupportedError,
                "dialog.show must be called from within an active window");

        var result    = DialogBuilder.ShowModal(dialogDef, engine, context);
        engine.StackPush(result);

        return EvalResult.NoError;
    }
}
