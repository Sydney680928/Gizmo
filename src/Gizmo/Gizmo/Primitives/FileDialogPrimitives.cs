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
using Gizmo.Helpers;

namespace Gizmo.Primitives;

internal static class FileDialogPrimitives
{
    /// <summary>
    /// Pops a filedialog definition record { title filter mode 'open'|'save'|'folder' },
    /// shows the TG FileDialog, and pushes a result record { ui.status text }.
    /// </summary>
    public static async Task<EvalResult> Show(MogwaiEngine engine, UiContext context)
    {
        var sig = engine.StackSign(1);
        if (sig.Count == 0)
            return EvalResult.Failure(engine, Error.TooFewArgumentsError, "filedialog.show");
        if (sig[0] != typeof(MOGRecord))
            return EvalResult.Failure(engine, Error.BadArgumentTypeError, "filedialog.show");

        var def    = engine.StackPopRecord();
        var mode   = RecordHelper.GetString(def, "mode",   "open");
        var title  = RecordHelper.GetString(def, "title",  "");
        var filter = RecordHelper.GetString(def, "filter", "*");

        string selectedPath = "";
        int    status       = 2;

        if (mode == "save")
        {
            var saveDialog = new SaveDialog
            {
                Title        = title,
                AllowedTypes = [new AllowedType("Files", filter)]
            };
            context.RunWithPump(engine, saveDialog);

            if (!saveDialog.Canceled && saveDialog.Path is not null)
            {
                selectedPath = saveDialog.Path.ToString()!;
                status       = 1;
            }
            saveDialog.Dispose();
        }
        else
        {
            var openDialog = new OpenDialog
            {
                Title        = title,
                AllowedTypes = [new AllowedType("Files", filter)],
                OpenMode     = mode == "folder" ? OpenMode.Directory : OpenMode.File
            };
            context.RunWithPump(engine, openDialog);

            if (!openDialog.Canceled && openDialog.Path is not null)
            {
                selectedPath = openDialog.Path.ToString()!;
                status       = 1;
            }
            openDialog.Dispose();
        }

        var result = new MOGRecord(engine);
        result.Items["ui.status"] = new MOGNumber(engine, status);
        result.Items["text"]      = new MOGString(engine, selectedPath);
        engine.StackPush(result);

        return EvalResult.NoError;
    }
}
