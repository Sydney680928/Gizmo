# GIZMO — Developer Documentation

GIZMO is a standalone console application that lets you build **Terminal User Interface (TUI) applications** using [MOGWAI](https://github.com/Sydney680928/mogwai), a stack-based RPN scripting language inspired by HP RPL calculators.

---

## Table of contents

| Document | Description |
|---|---|
| [Getting started](getting-started.md) | Installation, first script, tooling |
| [Windows](windows.md) | Window lifecycle, navigation, primitives |
| [Components](components.md) | All `ui.kind` components with properties and events |
| [Events](events.md) | Event system, `ui.eventData`, global variable rule |
| [Dialogs](dialogs.md) | `dialog.show`, `msgbox.show`, `filedialog.show` |
| [Scripting](scripting.md) | `run`, timers, `process.exec`, patterns |

---

## Quick reference

### Window primitives

| Primitive | Description |
|---|---|
| `window.show` | Show a window (blocking). Pushes `[window: 'name' status: value]` when closed. |
| `window.hide` | Close the current window with a status value. |
| `window.update` | Update a component property at runtime. |
| `window.active` | Pushes `true` if a window is currently displayed. |
| `window.current` | Pushes the name of the active window (or `''` if none). |

### Dialog primitives

| Primitive | Description |
|---|---|
| `dialog.show` | Show a modal dialog. Pushes a result record with field values. |
| `msgbox.show` | Show an info or confirm message box. |
| `filedialog.show` | Show an open / save / folder file dialog. |

### Property primitives

| Primitive | Description |
|---|---|
| `ui.gprop` | Get properties of a named component as a record. |
| `ui.sprop` | Set properties of a named component. |

### Script primitive

| Primitive | Description |
|---|---|
| `run` | Load and execute a `.mog` script file. |

---

## License

Apache 2.0 — see [LICENSE](../LICENSE)
