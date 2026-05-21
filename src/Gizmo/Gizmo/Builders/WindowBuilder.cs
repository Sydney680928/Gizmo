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
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Gizmo.Helpers;

namespace Gizmo.Builders;

/// <summary>
/// Builds the main Terminal.Gui Window from a MOGWAI window definition record.
/// MenuBar and StatusBar use the TG v2 Bar + Shortcut pattern.
/// </summary>
internal static class WindowBuilder
{
    public static Window Build(MOGRecord def, MogwaiEngine engine, UiContext context)
    {
        var title = RecordHelper.GetString(def, "title", "MOGWAI");

        var window = new Window
        {
            Title  = title,
            X      = Pos.Absolute(0),
            Y      = Pos.Absolute(0),
            Width  = Dim.Fill(),
            Height = Dim.Fill()
        };

        context.ActiveWindow = window;
        ComponentFactory.ApplyColorScheme(window, def);
        var windowScheme = window.GetScheme();

        // ── Menu bar (Bar with Shortcuts) ─────────────────────────────────────
        if (RecordHelper.HasKey(def, "menu"))
        {
            var (menuBar, popupMenus) = BuildMenuBar(def, engine, context);
            menuBar.Y = Pos.Absolute(0);
            window.Add(menuBar);

            // Popup menus must be added to the window separately so they
            // can overlap the content area when visible.
            foreach (var popup in popupMenus)
                window.Add(popup);
        }

        // ── Status bar (Bar with Shortcuts, anchored to bottom) ───────────────
        if (RecordHelper.HasKey(def, "statusbar"))
        {
            var statusBar = BuildStatusBar(def);
            statusBar.Y = Pos.AnchorEnd();
            window.Add(statusBar);
        }

        // ── Child components ──────────────────────────────────────────────────
        // Start children at Y=1 when a menu bar is present to avoid overlap.
        var childYOffset = RecordHelper.HasKey(def, "menu") ? 1 : 0;
        ComponentFactory.AddChildren(window, RecordHelper.GetRecordList(def, "childs"), engine, context, windowScheme, childYOffset);

        return window;
    }

    // ── Menu bar ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a Bar containing one Shortcut per menu card.
    /// Each card's items are added as MenuItem sub-views under a Menu popup.
    /// </summary>
    private static (Bar bar, List<Menu> popupMenus) BuildMenuBar(MOGRecord def, MogwaiEngine engine, UiContext context)
    {
        var bar        = new Bar { Width = Dim.Fill() };
        var cardDefs   = RecordHelper.GetRecordList(def, "menu");
        var popupMenus = new List<Menu>();

        foreach (var card in cardDefs)
        {
            var cardTitle = RecordHelper.GetString(card, "title");
            var itemDefs  = RecordHelper.GetRecordList(card, "items");

            var menu = new Menu
            {
                Visible     = false,
                Arrangement = ViewArrangement.Overlapped,
                Y           = Pos.Absolute(1),  // just below the bar
            };

            foreach (var itemDef in itemDefs)
            {
                if (RecordHelper.GetString(itemDef, "ui.kind") == "separator")
                {
                    menu.Add(new Line());
                    continue;
                }

                var label  = RecordHelper.GetString(itemDef, "label");
                var keyStr = RecordHelper.GetString(itemDef, "key");
                var action = RecordHelper.GetEvent(itemDef, "onClick");

                var shortcut = keyStr.Length == 1
                    ? ((Key)keyStr[0]).WithCtrl
                    : Key.Empty;

                var menuItem = new MenuItem { Title = label, Key = shortcut };

                if (action is not null)
                {
                    var capturedAction = action;
                    var capturedDef    = itemDef;
                    menuItem.Activated += async (_, _) =>
                    {
                        menu.Visible = false;
                        var eventData = ComponentFactory.BuildEventData(engine, capturedDef);
                        await ComponentFactory.ExecuteAction(capturedAction, engine, eventData, context);
                    };
                }

                menu.Add(menuItem);
            }

            var capturedMenu = menu;
            var cardShortcut = new Shortcut { Title = cardTitle };
            cardShortcut.Action += () =>
            {
                // Pos.Left() cannot cross container boundaries (Shortcut is in Bar,
                // Menu is in Window) — use absolute frame position instead.
                capturedMenu.X       = Pos.Absolute(cardShortcut.Frame.X);
                capturedMenu.Visible = !capturedMenu.Visible;
                if (capturedMenu.Visible) capturedMenu.SetFocus();
            };

            bar.Add(cardShortcut);
            popupMenus.Add(menu);
        }

        return (bar, popupMenus);
    }

    // ── Status bar ────────────────────────────────────────────────────────────

    private static Bar BuildStatusBar(MOGRecord def)
    {
        var bar   = new Bar { Width = Dim.Fill() };
        var texts = RecordHelper.GetStringList(def, "statusbar");

        foreach (var t in texts)
            bar.Add(new Shortcut { Title = t });

        return bar;
    }
}
