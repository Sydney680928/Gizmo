# Changelog

All notable changes to GIZMO will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

### Changed

### Fixed

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

[Unreleased]: https://github.com/Sydney680928/gizmo/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/Sydney680928/gizmo/releases/tag/v0.1.0
