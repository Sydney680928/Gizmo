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

using Gizmo.Primitives;
using MOGWAI.Engine;
using MOGWAI.Interfaces;
using MOGWAI.Objects;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Text;

namespace Gizmo;

public sealed class UiDelegate : IDelegate
{
    private readonly MogwaiEngine _engine;
    private readonly UiContext _context = new();
    private object _consoleAccessLocker = new();


    public MogwaiEngine Engine => _engine;

    public UiDelegate()
    {
        _engine = new MogwaiEngine("GIZMO_MOGWAI", keepAlive: true, useDefaultFolders: true);
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
        "window.refresh",
        "dialog.show",
        "msgbox.show",
        "filedialog.show",
        "ui.gprop",
        "ui.sprop",
        "run",
        "process.exec",
        "gizmo.info",
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
            "window.refresh" => await WindowPrimitives.Refresh(engine, _context),
            "dialog.show" => await DialogPrimitives.Show(engine, _context),
            "msgbox.show" => await MsgBoxPrimitives.Show(engine, _context),
            "filedialog.show" => await FileDialogPrimitives.Show(engine, _context),
            "ui.gprop" => PropPrimitives.Get(engine, _context),
            "ui.sprop" => PropPrimitives.Set(engine, _context),
            "run" => await RunPrimitive(engine, word),
            "process.exec" => await ProcessExec(engine, word),
            "gizmo.info" => await GizmoInfo(engine, word),  
            _ => EvalResult.NoExternalFunction
        };
    }

    private async Task<EvalResult> ProcessExec(MogwaiEngine engine, string word)
    {
        // [
        //   filename:         "myservice.exe"   (required)
        //   arguments:        "--flag value"    (optional)
        //   workingDirectory: "C:\..."          (optional)
        //   input:            "stdin data"      (optional)
        // ] process.exec

        var s = engine.StackSign(1);

        if (s.Count == 0)
            return EvalResult.Failure(Engine, Error.TooFewArgumentsError, word);

        var record = engine.StackPopRecord();
        
        var filename = record.GetItem("filename") as MOGString;

        if (filename == null)
            return EvalResult.Failure(Engine, Error.BadArgumentValueError, "filename key is mandatory");

        var args = record.GetItem("arguments") as MOGString;
        var wd = record.GetItem("workingDirectory") as MOGString;
        var input = record.GetItem("input") as MOGString;

        var process = new Process();

        process.StartInfo.FileName = filename.Value;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.RedirectStandardInput = input is not null;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
        process.StartInfo.StandardErrorEncoding = Encoding.UTF8;

        if (args is not null)
            process.StartInfo.Arguments = args.Value;

        if (wd is not null)
            process.StartInfo.WorkingDirectory = wd.Value;

        try
        {
            process.Start();

            // Write stdin before reading stdout/stderr to avoid deadlock
            if (input is not null)
            {
                await process.StandardInput.WriteAsync(input.Value);
                process.StandardInput.Close();
            }

            // Read stdout and stderr in parallel to avoid buffer deadlock
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            var output = await outputTask;
            var error = await errorTask;

            var result = new MOGRecord(Engine);
            result.SetNumber("status", process.ExitCode);
            result.SetString("output", output.TrimEnd('\r', '\n'));
            result.SetString("error", error.TrimEnd('\r', '\n'));

            Engine.StackPush(result);
        }
        catch
        {
            return EvalResult.Failure(Engine, Error.InternalError, "Unable to execute process");
        }

        return EvalResult.NoError;
    }

    private async Task<EvalResult> GizmoInfo(MogwaiEngine engine, string word)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var attr = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>();
        string strVersion = attr?.Version ?? string.Empty;

        var infos = new MOGRecord(Engine);
        infos.SetString("version", strVersion);

        var code = new MOGCode(Engine, "mogwai.info", 0, null);
        var r = await code.Execute();

        if (r.IsSuccess)
        {
            var s = Engine.StackSign(1);    

            if (s.Count > 0 && s[0] == typeof(MOGRecord))
            {
                var record = Engine.StackPopRecord();
                infos.SetItem("mogwai", record);    
            }
        }
        
        Engine.StackPush(infos);
        return EvalResult.NoError;
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
    
    public Task<EvalResult> ConsoleLocate(MogwaiEngine engine, int x, int y)
    {
        lock (_consoleAccessLocker)
            Console.SetCursorPosition(x, y);

        return Task.FromResult(EvalResult.NoError);
    }

    public Task<(EvalResult result, int x, int y)> ConsoleGetCursorPosition(MogwaiEngine engine)
    {
        var r = Console.GetCursorPosition();
        return Task.FromResult((EvalResult.NoError, r.Left, r.Top));
    }

    public Task<EvalResult> ConsoleSetForegroundColor(MogwaiEngine engine, string color)
    {
        lock (_consoleAccessLocker)
            switch (color.ToLower())
            {
                case "black": Console.ForegroundColor = ConsoleColor.Black; break;
                case "blue": Console.ForegroundColor = ConsoleColor.Blue; break;
                case "cyan": Console.ForegroundColor = ConsoleColor.Cyan; break;
                case "gray": Console.ForegroundColor = ConsoleColor.Gray; break;
                case "green": Console.ForegroundColor = ConsoleColor.Green; break;
                case "magenta": Console.ForegroundColor = ConsoleColor.Magenta; break;
                case "red": Console.ForegroundColor = ConsoleColor.Red; break;
                case "white": Console.ForegroundColor = ConsoleColor.White; break;
                case "yellow": Console.ForegroundColor = ConsoleColor.Yellow; break;
                default: break;
            }

        return Task.FromResult(EvalResult.NoError);
    }

    public Task<EvalResult> ConsoleSetBackgroundColor(MogwaiEngine engine, string color)
    {
        lock (_consoleAccessLocker)
            switch (color.ToLower())
            {
                case "black": Console.BackgroundColor = ConsoleColor.Black; break;
                case "blue": Console.BackgroundColor = ConsoleColor.Blue; break;
                case "cyan": Console.BackgroundColor = ConsoleColor.Cyan; break;
                case "gray": Console.BackgroundColor = ConsoleColor.Gray; break;
                case "green": Console.BackgroundColor = ConsoleColor.Green; break;
                case "magenta": Console.BackgroundColor = ConsoleColor.Magenta; break;
                case "red": Console.BackgroundColor = ConsoleColor.Red; break;
                case "white": Console.BackgroundColor = ConsoleColor.White; break;
                case "yellow": Console.BackgroundColor = ConsoleColor.Yellow; break;
                default: break;
            }

        return Task.FromResult(EvalResult.NoError);
    }

    public Task<(EvalResult result, int key)> ConsoleGetInputKey(MogwaiEngine engine)
    {
        int key = -1;

        lock (_consoleAccessLocker)
        {
            if (Console.KeyAvailable)
            {
                var keyInfo = Console.ReadKey(true);
                key = (int)keyInfo.Key;
            }
        }

        return Task.FromResult((EvalResult.NoError, key));
    }

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

    public string[] Skills(MogwaiEngine engine) => ["GIZMO", "TERMINAL"];

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
