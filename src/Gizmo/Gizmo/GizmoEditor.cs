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
using System.Text;
using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Gizmo
{
    /// <summary>
    /// Full-screen TUI code editor for GIZMO (Terminal.Gui v2).
    /// No MenuBar — all actions via keyboard shortcuts to avoid AZERTY/AltGr conflicts
    /// on Windows Terminal.
    /// Must be opened from the main thread (TG requirement).
    /// </summary>
    internal class GizmoEditor
    {
        private readonly MogwaiEngine _engine;

        // ─── Persistent state across edit sessions ────────────────────────────

        private string _sessionCode = string.Empty;
        private string _savedText   = string.Empty;
        private string _filename    = string.Empty;

        // ─── Public properties ────────────────────────────────────────────────

        public bool    HasUnsavedChanges => _sessionCode != _savedText;
        public string? PendingRunCode    { get; private set; }

        // ─── Constructor ──────────────────────────────────────────────────────

        public GizmoEditor(MogwaiEngine engine) => _engine = engine;

        // ─── Helpers ──────────────────────────────────────────────────────────

        private static string Normalize(string s) => s.Replace("\r\n", "\n");

        private static string GetText(TextView tv) =>
            Normalize(tv.Text?.ToString() ?? string.Empty);

        private static void RefreshLineNumbers(TextView lineNumView, TextView textView)
        {
            var text  = GetText(textView);
            var count = text.Split('\n').Length;
            var sb    = new StringBuilder(count * 7);

            for (int i = 1; i <= count; i++)
                sb.AppendLine($"{i,4} │");

            var newText = sb.ToString();
            if (lineNumView.Text?.ToString() != newText)
                lineNumView.Text = newText;
        }

        private string BuildTitle(string currentCode)
        {
            var name  = _filename == string.Empty ? "[untitled]" : Path.GetFileName(_filename);
            var dirty = currentCode != _savedText ? " ●" : string.Empty;
            return $"GIZMO Editor — {name}{dirty}";
        }

        // ─── Entry point ──────────────────────────────────────────────────────

        public void Open()
        {
            PendingRunCode = null;

            using IApplication app = Application.Create();
            app.Init();

            // ── Command bar (second-to-last line) ────────────────────────────
            var cmdBar = new Label
            {
                Text   = "  Ctrl+N New   Ctrl+O Open   Ctrl+W Save   Ctrl+A Save as   F5 Run   Ctrl+Q Quit",
                X      = 0,
                Y      = Pos.AnchorEnd(2),
                Width  = Dim.Fill(),
                Height = 1,
            };
            cmdBar.SetScheme(new Scheme
            {
                Normal = new Terminal.Gui.Drawing.Attribute(Color.Black, Color.Gray),
                Focus  = new Terminal.Gui.Drawing.Attribute(Color.Black, Color.Gray),
            });

            // ── Hint bar (last line) : Ln/Col + filename ─────────────────────
            var hintBar = new Label
            {
                Text   = "",
                X      = 0,
                Y      = Pos.AnchorEnd(1),
                Width  = Dim.Fill(),
                Height = 1,
            };
            hintBar.SetScheme(new Scheme
            {
                Normal = new Terminal.Gui.Drawing.Attribute(Color.Black, Color.Gray),
                Focus  = new Terminal.Gui.Drawing.Attribute(Color.Black, Color.Gray),
            });

            // ── Line number gutter ───────────────────────────────────────────
            const int gutterWidth = 6;

            var lineNumView = new TextView
            {
                X        = 0,
                Y        = 0,
                Width    = gutterWidth,
                Height   = Dim.Fill(2),
                ReadOnly = true,
                WordWrap = false,
                CanFocus = false,
            };
            lineNumView.SetScheme(new Scheme
            {
                Normal = new Terminal.Gui.Drawing.Attribute(Color.Gray, Color.Black),
                Focus  = new Terminal.Gui.Drawing.Attribute(Color.Gray, Color.Black),
            });

            // ── Main TextView ────────────────────────────────────────────────
            var textView = new TextView
            {
                X        = gutterWidth,
                Y        = 0,
                Width    = Dim.Fill(),
                Height   = Dim.Fill(2),
                WordWrap = false,
                Text     = _sessionCode,
            };
            textView.SetScheme(new Scheme
            {
                Normal = new Terminal.Gui.Drawing.Attribute(Color.White, Color.Black),
                Focus  = new Terminal.Gui.Drawing.Attribute(Color.White, Color.Black),
            });

            // ── Top-level window ─────────────────────────────────────────────
            var window = new Window
            {
                Title  = BuildTitle(_sessionCode),
                X      = 0,
                Y      = 0,
                Width  = Dim.Fill(),
                Height = Dim.Fill(),
            };
            window.SetScheme(new Scheme
            {
                Normal = new Terminal.Gui.Drawing.Attribute(Color.White, Color.Black),
                Focus  = new Terminal.Gui.Drawing.Attribute(Color.White, Color.Black),
            });

            window.Add(cmdBar, hintBar, lineNumView, textView);

            // ── Keyboard shortcuts via KeyDown event ─────────────────────────
            // Use event subscription instead of override — TextView doesn't
            // expose OnKeyDown as overridable in TG v2.
            textView.KeyDown += (s, e) =>
            {
                if (e == Key.N.WithCtrl)
                {
                    if (ConfirmSave(app, textView)) DoNew(textView, lineNumView, window);
                    e.Handled = true;
                }
                else if (e == Key.O.WithCtrl)
                {
                    if (ConfirmSave(app, textView)) DoOpen(app, textView, lineNumView, window);
                    e.Handled = true;
                }
                else if (e == Key.W.WithCtrl)
                {
                    DoSave(app, textView);
                    window.Title = BuildTitle(GetText(textView));
                    e.Handled = true;
                }
                else if (e == Key.A.WithCtrl)
                {
                    DoSaveAs(app, textView);
                    window.Title = BuildTitle(GetText(textView));
                    e.Handled = true;
                }
                else if (e == Key.Q.WithCtrl)
                {
                    if (ConfirmSave(app, textView)) window.RequestStop();
                    e.Handled = true;
                }
                else if (e == Key.F5)
                {
                    DoRun(textView, window);
                    e.Handled = true;
                }
            };

            // ── Init line numbers ────────────────────────────────────────────
            RefreshLineNumbers(lineNumView, textView);

            // ── 50ms timer : scroll sync + line numbers + title + hint ───────
            app.AddTimeout(TimeSpan.FromMilliseconds(50), () =>
            {
                if (lineNumView.Viewport.Y != textView.Viewport.Y)
                {
                    lineNumView.Viewport = lineNumView.Viewport with { Y = textView.Viewport.Y };
                    lineNumView.SetNeedsDraw();
                }

                RefreshLineNumbers(lineNumView, textView);

                var newTitle = BuildTitle(GetText(textView));
                if (window.Title != newTitle)
                    window.Title = newTitle;

                var hint = $"  Ln {textView.CurrentRow + 1}  Col {textView.CurrentColumn + 1}" +
                           (_filename == string.Empty ? "   [untitled]" : $"   {_filename}");
                if (hintBar.Text != hint)
                    hintBar.Text = hint;

                return true;
            });

            // ── Run ──────────────────────────────────────────────────────────
            app.Run(window);

            _sessionCode = GetText(textView);
        }

        // ─── Actions ─────────────────────────────────────────────────────────

        private void DoNew(TextView textView, TextView lineNumView, Window window)
        {
            textView.Text = string.Empty;
            _savedText    = string.Empty;
            _sessionCode  = string.Empty;
            _filename     = string.Empty;
            RefreshLineNumbers(lineNumView, textView);
            window.Title  = BuildTitle(string.Empty);
        }

        private void DoRun(TextView textView, Window window)
        {
            _sessionCode   = GetText(textView);
            PendingRunCode = _sessionCode;
            window.RequestStop();
        }

        private void DoOpen(IApplication app, TextView textView, TextView lineNumView, Window window)
        {
            List<IAllowedType> aTypes = [new AllowedType("MOGWAI Scripts", ".mog"), new AllowedTypeAny()];

            var dlg = new OpenDialog
            {
                Title                   = "Open",
                AllowedTypes            = aTypes,
                AllowsMultipleSelection = false,
                Path                    = _engine.ProgramsDirectory,
            };

            app.Run(dlg);

            if (!dlg.Canceled && dlg.FilePaths.Count > 0)
            {
                try
                {
                    var content   = Normalize(File.ReadAllText(dlg.FilePaths[0]));
                    _filename     = dlg.FilePaths[0];
                    _savedText    = content;
                    _sessionCode  = content;
                    textView.Text = content;
                    RefreshLineNumbers(lineNumView, textView);
                    window.Title  = BuildTitle(content);
                }
                catch
                {
                    MessageBox.ErrorQuery(app, "Open", "Unable to open the file!", "OK");
                }
            }

            dlg.Dispose();
        }

        private bool DoSave(IApplication app, TextView textView)
        {
            if (_filename == string.Empty)
                return DoSaveAs(app, textView);

            var content = GetText(textView);
            try
            {
                File.WriteAllText(_filename, content);
                _savedText   = content;
                _sessionCode = content;
                return true;
            }
            catch
            {
                MessageBox.ErrorQuery(app, "Save", "Unable to save the file!", "OK");
            }

            return false;
        }

        private bool DoSaveAs(IApplication app, TextView textView)
        {
            List<IAllowedType> aTypes = [new AllowedType("MOGWAI Scripts", ".mog"), new AllowedTypeAny()];

            var dlg = new SaveDialog
            {
                Title        = "Save as...",
                AllowedTypes = aTypes,
                Path         = _filename == string.Empty ? _engine.ProgramsDirectory : _filename,
            };

            app.Run(dlg);

            bool canceled = dlg.Canceled;
            var  path     = dlg.Path;
            dlg.Dispose();

            if (canceled || string.IsNullOrEmpty(path))
                return false;

            var content = GetText(textView);
            try
            {
                if (File.Exists(path))
                {
                    if (MessageBox.Query(app, "Save", "File already exists. Overwrite?", "Yes", "No") is not 0)
                        return false;
                }

                File.WriteAllText(path, content);
                _filename    = path;
                _savedText   = content;
                _sessionCode = content;
                return true;
            }
            catch
            {
                MessageBox.ErrorQuery(app, "Save", "Unable to save the file!", "OK");
            }

            return false;
        }

        private bool ConfirmSave(IApplication app, TextView textView)
        {
            var current = GetText(textView);
            if (current == _savedText)
                return true;

            int? r = MessageBox.Query(app, "Save", "Modifications are not saved. Save?", "Yes", "No", "Cancel");

            return r switch
            {
                0 => DoSave(app, textView),
                1 => true,
                _ => false,
            };
        }
    }
}
