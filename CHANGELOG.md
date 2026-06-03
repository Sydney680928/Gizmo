# Changelog

All notable changes to GIZMO will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

### Changed

### Fixed

## [1.4.0] - 2026-06-04

### Added

- **`gizmo.info` primitive** — returns a record with GIZMO version information and the full `mogwai.info` record from the underlying engine.

  ```mogwai
  gizmo.info -> '$info'
  $info version: get ?              # "1.3.0.0"
  $info mogwai: get version: get ?  # "8.8.0.0"
  ```

  | Key | Type | Description |
  |---|---|---|
  | `version:` | String | GIZMO version |
  | `mogwai:` | Record | Full record returned by `mogwai.info` |

### Changed

- Using **MOGWAI v8.8.1** — the latest version of the MOGWAI scripting engine.

### Fixed

## [1.2.0] - 2026-05-26

### Changed

- Using **MOGWAI v8.7.0** the latest version of MOGWAI Scripting Engine.

## [1.1.0] - 2026-05-23

### Added

- **`x:` and `y:` positioning on components** — components now accept explicit `x:` and `y:` keys to override the automatic vertical stacking. When `x:` is specified, the component width switches to `auto` instead of `fill`. Both keys are optional and independent.

- **`rows:` updatable on `ui.tableview`** — the `rows:` property of a `ui.tableview` can now be set at runtime via `window.update` or `ui.sprop`, replacing all table data in one call.

  ```mogwai
  [! name: 'myTable' rows: (("1.0" "3.14") ("2.0" "6.28"))] window.update
  ```

- **Status bar updatable at runtime** — status bar items are now registered with reserved names (`statusbar`, `statusbar.1`, `statusbar.2`...) and can be updated via `window.update` using the `title:` property.

  ```mogwai
  [! name: 'statusbar' title: "Ready - 42 rows computed"] window.update
  ```
### Fixed

- **Menu `y:` property ignored** — child components now respect an explicit `y:` key in their definition. Falls back to automatic stacking if not specified.

- **Components overlapping menu bar** — when a `menu:` is present, child components now start at `Y=1` automatically instead of `Y=0`.

- **Menu shortcut key forced to uppercase** — `key:` value is now used as-is: lowercase gives `Ctrl+X`, uppercase gives `Ctrl+Shift+X`. Values longer than one character are silently ignored (no shortcut assigned).

- **Menu dropdown not showing** — the `Menu` popup was created but never added to the window's view hierarchy. Fixed by adding each popup `Menu` to the window with `Arrangement = ViewArrangement.Overlapped`. Also fixed a cross-container layout error caused by `Pos.Left(shortcut)` referencing a view in a different container — replaced with `Pos.Absolute(shortcut.Frame.X)` evaluated at click time.

## [1.0.0] - 2026-05-21

### Added

- **`edit` command in REPL** — opens the built-in full-screen TUI code editor powered by Terminal.Gui v2.

  Features:
  - Line numbers column
  - Dirty indicator (`●`) in the window title
  - `Ln / Col` status bar
  - Keyboard shortcuts: `Ctrl+N` New, `Ctrl+O` Open, `Ctrl+W` Save, `Ctrl+A` Save as, `F5` Run, `Ctrl+Q` Quit
  - Editor/run loop: `F5` closes the editor, executes the script, then reopens the editor automatically
  - Unsaved changes warning when exiting with `bye`

## [0.1.0] - 2026-05-15

### Added

- **`window.refresh` primitive** — forces a screen redraw. Useful after a series of `ui.sprop` calls from the main script flow.

  > **Limitation**: has no effect inside a blocking `for` loop (the MOGWAI pump and TG share the same thread). Use a MOGWAI timer for progressive UI updates (progress bars, animations...).

- **Color support in `dialog.show`** — the dialog definition record now accepts the same four color keys as windows and components: `forecolor:`, `backcolor:`, `focusForecolor:`, `focusBackcolor:`. Colors are inherited by child components.

  ```
  [
      title: "Nouveau contact"
      backcolor: 'color.black'
      forecolor: 'color.white'
      focusForecolor: 'color.black'
      focusBackcolor: 'color.brightcyan'
      childs:
      (
          [ui.kind: 'ui.edit' name: 'name' label: "Nom :" text: ""]
          [ui.kind: 'ui.buttons' items: ("OK" "Annuler")]
      )
  ] dialog.show -> '$r'
  ```

- **MOGWAI timers active during dialogs** — MOGWAI timers now continue to fire while a dialog is displayed. The dialog uses its own pump mechanism (`System.Threading.Timer` + `App.Invoke`) to keep the MOGWAI engine alive during modal display.

- **`about` command in REPL** — displays GIZMO version, MOGWAI version, and license information.

- **`dialog.show` guard** — raises `MW.7` (operation not supported) if called outside an active window, with a clear error message.

- **Window primitives** — `window.show`, `window.hide`, `window.update`, `window.active`, `window.current`

  - `window.show` opens a window and blocks until closed. Pushes a result record `[window: 'name' status: value]`.
  - `window.hide` closes the current window with a mandatory status value.
  - `window.update` updates a component property at runtime from an event handler or timer.
  - `window.active` pushes `true` if a window is currently displayed.
  - `window.current` pushes the name of the active window as a `name` type.

- **Window lifecycle events** — `onShow:` and `onHide:` keys in the window definition record.

  - `onShow:` executes after the window is built and components are registered, but before display. Used for initialization and data loading.
  - `onHide:` executes after the window closes, before the result record is pushed. Used for cleanup and resource release.
  - Both receive `ui.eventData` with `[window: 'name']`. `onHide:` also receives `status:`.

  ```
  [
      name: 'main'
      title: "My App"
      onShow: { [! name: 'lblStatus' text: "Ready"] window.update }
      onHide: { ui.eventData status: get debug.write }
      childs: ( ... )
  ]
  window.show -> '$r'
  ```

- **UI components** — 15 components available via `ui.kind:`:

  | Component | Description |
  |---|---|
  | `ui.label` | Static text |
  | `ui.edit` | Single-line text input |
  | `ui.password` | Masked text input |
  | `ui.multiline` | Multi-line text area |
  | `ui.integer` | Integer input |
  | `ui.float` | Float input |
  | `ui.check` | Checkbox |
  | `ui.radio` | Vertical radio button group |
  | `ui.combo` | Dropdown list |
  | `ui.button` | Clickable button |
  | `ui.listview` | Scrollable single-column list |
  | `ui.tableview` | Multi-column table with full-row selection |
  | `ui.progress` | Progress bar |
  | `ui.frame` | Bordered container with title |
  | `ui.separator` | Horizontal separator line |

- **Component events** — named event handlers replacing the generic `action:` key:

  | Event | Components |
  |---|---|
  | `onClick:` | `ui.button` |
  | `onChange:` | `ui.edit`, `ui.password`, `ui.integer`, `ui.float`, `ui.multiline`, `ui.check`, `ui.radio`, `ui.combo` |
  | `onValidate:` | `ui.edit`, `ui.password`, `ui.integer`, `ui.float` |
  | `onSelect:` | `ui.listview`, `ui.tableview` |
  | `onActivate:` | `ui.listview`, `ui.tableview` |

  Event handlers can be written with standard braces `{ }` or MOGWAI function literals `« »`.

  ```
  [ui.kind: 'ui.button' label: "OK" onClick: { false window.hide }]
  ```

- **`ui.eventData`** — automatically injected into every event handler's local context. Contains event-specific data (text, index, value, row...).

- **Dialog primitives** — `dialog.show`, `msgbox.show`, `filedialog.show`

  - `dialog.show` — modal dialog with custom components and buttons. Pushes a result record with field values.
  - `msgbox.show` — info or confirm message box (`ui.info` / `ui.confirm`).
  - `filedialog.show` — open / save / folder file selection dialog.

- **Property primitives** — `ui.gprop`, `ui.sprop`

  - `ui.gprop` — reads component properties as a record.
  - `ui.sprop` — sets component properties.

- **Color system** — four color keys accepted by all components and the window definition:

  | Key | Description |
  |---|---|
  | `forecolor:` | Text color in normal state |
  | `backcolor:` | Background color in normal state |
  | `focusForecolor:` | Text color when focused |
  | `focusBackcolor:` | Background color when focused |

  Colors use the `color.*` name prefix: `color.black`, `color.blue`, `color.red`, `color.brightcyan`, etc. (16 ANSI colors).

  Unspecified colors inherit from the parent component's color scheme.

  ```
  [
      name: 'main'
      title: "My App"
      backcolor: 'color.black'
      forecolor: 'color.white'
      childs:
      (
          [ui.kind: 'ui.label' text: "Error!" forecolor: 'color.brightred']
          [ui.kind: 'ui.button' label: "OK"
              forecolor: 'color.brightcyan' backcolor: 'color.black'
              focusForecolor: 'color.black' focusBackcolor: 'color.brightcyan'
              onClick: { false window.hide }]
      )
  ]
  window.show drop
  ```

- **Themes** — two ready-to-use theme files in `themes/`:

  - `themes/dark.mog` — dark theme (black background, white text, cyan accents)
  - `themes/classic.mog` — classic theme inspired by AMSTRAD CPC (blue background, yellow text)

  ```
  "themes/dark.mog" include
  [name: 'main' backcolor: @$theme.back forecolor: @$theme.fore ...]
  window.show drop
  ```

- **`run` primitive** (GIZMO-specific) — loads and executes a `.mog` script file.

  ```
  "modules/settings.mog" run
  ```

- **Interactive REPL** — run `gizmo` without arguments for interactive mode. Commands: `studio`, `help`, `about`, `bye`.

- **MOGWAI Studio / VS Code integration** — `studio` command connects to MOGWAI Studio or the VS Code MOGWAI extension for live debugging.

- **Self-contained distribution** — published as standalone executables for 5 platforms: `win-x64`, `osx-x64`, `osx-arm64`, `linux-x64`, `linux-arm64`. No .NET runtime required.

---

[Unreleased]: https://github.com/Sydney680928/gizmo/compare/v1.4.0...HEAD
[1.4.0]: https://github.com/Sydney680928/gizmo/compare/v1.2.0...v1.4.0
[1.2.0]: https://github.com/Sydney680928/gizmo/compare/v1.1.0...v1.2.0
[1.1.0]: https://github.com/Sydney680928/gizmo/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/Sydney680928/gizmo/compare/v0.1.0...v1.0.0
[0.1.0]: https://github.com/Sydney680928/gizmo/releases/tag/v0.1.0
