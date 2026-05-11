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
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Gizmo;

internal sealed class UiContext
{
    private readonly Dictionary<string, View> _components = new(StringComparer.Ordinal);

    public IApplication? App { get; set; }
    public TaskCompletionSource<EvalResult>? AppTcs { get; set; }
    public Window? ActiveWindow { get; set; }

    /// <summary>Name of the currently displayed window (empty if none).</summary>
    public string ActiveWindowName { get; set; } = "";

    /// <summary>Name of the window that just closed — captured before Reset for window.run result.</summary>
    public string ClosedWindowName { get; set; } = "";

    /// <summary>Value passed to window.close — pushed as status in window.run result record.</summary>
    public MOGObject? CloseStatus { get; set; }

    /// <summary>Stores an error generated during the pump loop, to be propagated by window.run.</summary>
    public EvalResult? PumpError { get; set; }

    // ── Registry ──────────────────────────────────────────────────────────────

    public void RegisterComponent(string name, View view)
    {
        if (!string.IsNullOrEmpty(name))
            _components[name] = view;
    }

    public View? GetComponent(string name)
        => _components.TryGetValue(name, out var v) ? v : null;

    public T? GetComponent<T>(string name) where T : View
        => GetComponent(name) as T;

    public void Reset()
    {
        _components.Clear();
        ActiveWindow      = null;
        ActiveWindowName  = "";
        ClosedWindowName  = "";
        CloseStatus       = null;
        AppTcs            = null;
        App               = null;
        PumpError         = null;
    }

    // ── Pump helpers ──────────────────────────────────────────────────────────

    /// <summary>
    /// Runs a blocking App.Run(view) while keeping the MOGWAI pump alive via AddTimeout.
    /// Returns the run result (e.g. button index for dialogs).
    /// </summary>
    public object? RunWithPump(MogwaiEngine engine, IRunnable runnable)
    {
        var active    = true;
        var emptyCode = new MOGCode(engine);
        App!.AddTimeout(TimeSpan.FromMilliseconds(50), () =>
        {
            var result = emptyCode.Execute().GetAwaiter().GetResult();           
            
            if (result.IsError)
            {
                PumpError = result;
                active    = false;
                App!.RequestStop();
                return false;
            }
            else if (engine.ExitRequested)
            {
                active = false;
                App!.RequestStop();
                return false;
            }

            return active;
        });
        var runResult = App!.Run(runnable);
        active = false;
        return runResult;
    }

    public void RunWithPump(MogwaiEngine engine, Action blockingAction)
    {
        var active    = true;
        var emptyCode = new MOGCode(engine);
        App!.AddTimeout(TimeSpan.FromMilliseconds(50), () =>
        {
            var result = emptyCode.Execute().GetAwaiter().GetResult();
            
            if (result.IsError)
            {
                PumpError = result;
                active    = false;
                App!.RequestStop();
                return false;
            }
            else if (engine.ExitRequested)
            {
                active = false;
                App!.RequestStop();
                return false;
            }

            return active;
        });
        blockingAction();
        active = false;
    }
}
