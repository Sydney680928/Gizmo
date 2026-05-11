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

using System.Diagnostics;
using System.Net;
using MOGWAI.Engine;
using MOGWAI.Interfaces;
using MOGWAI.Objects;
using Gizmo.Primitives;

namespace Gizmo;

public sealed class UiDelegate : IDelegate
{
    private readonly MogwaiEngine _engine;
    private readonly UiContext _context = new();
    private object _consoleAccessLocker = new();


    public MogwaiEngine Engine => _engine;

    public UiDelegate()
    {
        _engine = new MogwaiEngine("MOGWAI_UI", keepAlive: true, useDefaultFolders: true);
        _engine.Delegate = this;
    }

    public async Task<EvalResult> RunScript(string script)
        => await _engine.RunAsync(script, debugMode: false);

    // ── Host functions ────────────────────────────────────────────────────────

    public string[] HostFunctions(MogwaiEngine engine) =>
    [
        "window.show",
        "window.update",
        "window.active",
        "window.current",
        "window.hide",
        "dialog.show",
        "msgbox.show",
        "filedialog.show",
        "ui.gprop",
        "ui.sprop",
        "run"
    ];

    public async Task<EvalResult> ExecuteHostFunction(MogwaiEngine engine, string word)
    {
        return word switch
        {
            "window.show" => await WindowPrimitives.Show(engine, _context),
            "window.update" => WindowPrimitives.Update(engine, _context),
            "window.active" => WindowPrimitives.Active(engine, _context),
            "window.current" => WindowPrimitives.Current(engine, _context),
            "window.hide" => WindowPrimitives.Hide(engine, _context),
            "dialog.show" => await DialogPrimitives.Show(engine, _context),
            "msgbox.show" => await MsgBoxPrimitives.Show(engine, _context),
            "filedialog.show" => await FileDialogPrimitives.Show(engine, _context),
            "ui.gprop" => PropPrimitives.Get(engine, _context),
            "ui.sprop" => PropPrimitives.Set(engine, _context),
            "run" => await RunPrimitive(engine, word),
            _ => EvalResult.NoExternalFunction
        };
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public async Task ProgramStart(MogwaiEngine engine, string code)    => await Task.CompletedTask;
    
    public Task ProgramEnd(MogwaiEngine engine, EvalResult result)
    {
        // Stop TG if still running
        _context.App?.RequestStop();
        
        return Task.CompletedTask;
    }

    public async Task<EvalResult> EngineDidPause(MogwaiEngine engine)    => EvalResult.NoError;
    public async Task<EvalResult> EngineDidResume(MogwaiEngine engine)   => EvalResult.NoError;

    // ── Console I/O — ignoré en mode TUI ─────────────────────────────────────

    public Task<EvalResult> ConsoleClearScreen(MogwaiEngine engine)
    {
        lock (_consoleAccessLocker)
            Console.Clear();

        return Task.FromResult(EvalResult.NoError);
    }

    public Task<EvalResult> ConsolePrintLn(MogwaiEngine engine, string message)
    {
        lock (_consoleAccessLocker)
            Console.WriteLine(message);

        return Task.FromResult(EvalResult.NoError);
    }

    public Task<EvalResult> ConsolePrint(MogwaiEngine engine, string message)
    {
        lock (_consoleAccessLocker)
            Console.Write(message);

        return Task.FromResult(EvalResult.NoError);
    }

    public Task<(EvalResult result, string? value)> Prompt(MogwaiEngine engine, string message)
    {
        lock (_consoleAccessLocker)
        {
            Console.Write(message);
            var r = Console.ReadLine();
            return Task.FromResult((EvalResult.NoError, r));
        }
    }

    public async Task<EvalResult> ConsoleShow(MogwaiEngine engine)                            => EvalResult.NoError;
    public async Task<EvalResult> ConsoleHide(MogwaiEngine engine)                            => EvalResult.NoError;
    public async Task<EvalResult> ConsoleLocate(MogwaiEngine engine, int x, int y)            => EvalResult.NoError;
    public async Task<(EvalResult result, int x, int y)> ConsoleGetCursorPosition(MogwaiEngine engine)
        => (EvalResult.NoError, 0, 0);
    public async Task<EvalResult> ConsoleSetForegroundColor(MogwaiEngine engine, string color) => EvalResult.NoError;
    public async Task<EvalResult> ConsoleSetBackgroundColor(MogwaiEngine engine, string color) => EvalResult.NoError;
    public async Task<(EvalResult result, int key)> ConsoleGetInputKey(MogwaiEngine engine)
        => (EvalResult.NoError, 0);

    // ── Messages / debug ─────────────────────────────────────────────────────

    public async Task<EvalResult> MessageReceivedFromRuntime(MogwaiEngine engine, string message, MOGObject parameter)
        => EvalResult.NoError;
    public async Task<EvalResult> DebugMessage(MogwaiEngine engine, string message)
    {
        Debug.WriteLine($"[MOGWAI] {message}");
        return EvalResult.NoError;
    }
    
    
    public async Task<EvalResult> DebugClear(MogwaiEngine engine)                   => EvalResult.NoError;

    // ── MOGWAI STUDIO ─────────────────────────────────────────────────────────

    public async Task<EvalResult> StudioDidConnect(MogwaiEngine engine)                                  => EvalResult.NoError;
    public async Task<EvalResult> StudioDidDisconnect(MogwaiEngine engine)                               => EvalResult.NoError;
    public async Task<EvalResult> SocketServerDidStart(MogwaiEngine engine, IPAddress address, int port) => EvalResult.NoError;
    public async Task<EvalResult> SocketServerDidStop(MogwaiEngine engine)                               => EvalResult.NoError;

    // ── run primitive ─────────────────────────────────────────────────────────────

    private async Task<EvalResult> RunPrimitive(MogwaiEngine engine, string word)
    {
        var s = engine.StackSign(1);

        if (s.Count == 0)
            return EvalResult.Failure(engine, Error.TooFewArgumentsError, word);

        if (s[0] == typeof(MOGString))
        {
            var codeFile = engine.StackPop() as MOGString;

            try
            {
                var bytes = File.ReadAllBytes(codeFile!.Value);
                var result = engine.GetCodeFormBytes(bytes);

                if (result.code != null)
                {
                    return await _engine.RunAsync(result.code, false);
                }
                else
                {
                    return EvalResult.Failure(engine, Error.ParseError, word);
                }
            }
            catch
            {
                return EvalResult.Failure(engine, Error.FileOperationError, word);
            }
        }

        return EvalResult.Failure(engine, Error.BadArgumentTypeError, word);
    }
}
