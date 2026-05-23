# Windows

A **window** is the top-level container of a GIZMO application. Only one window can be active at a time.

---

## Window lifecycle

```
window.show called
       │
       ▼
  Window built
  (components registered)
       │
       ▼
  onShow: executed       ← initialize components, load data
       │
       ▼
  Window displayed
  User interacts
  (onClick, onChange, timers...)
       │
       ▼
  window.hide called     ← user or code closes the window
       │
       ▼
  onHide: executed       ← cleanup, free resources
       │
       ▼
  Result record pushed   ← [window: 'name' status: value]
  window.show returns
```

---

## Window definition record

A window is defined as a MOGWAI record with the following keys:

| Key | Type | Description |
|---|---|---|
| `name:` | Name | Window identifier — used by `window.current` and the result record |
| `title:` | String | Title displayed in the window header |
| `childs:` | List | List of component definition records |
| `menu:` | List | Optional menu bar definition |
| `statusbar:` | List | Optional status bar definition |
| `forecolor:` | Name | Window text color (e.g. `'color.yellow'`) |
| `backcolor:` | Name | Window background color (e.g. `'color.blue'`) |
| `focusForecolor:` | Name | Text color for focused components inside the window |
| `focusBackcolor:` | Name | Background color for focused components inside the window |
| `onShow:` | Code/Function | Executed just before the window is displayed |
| `onHide:` | Code/Function | Executed just after the window is closed |

```mogwai
[
    name: 'main'
    title: "My Application"
    backcolor: 'color.blue'
    forecolor: 'color.yellow'

    onShow:
    {
        # Initialize components before display
        [! name: 'lblStatus' text: "Ready"] window.update
    }

    onHide:
    {
        # Cleanup after close
        ui.eventData status: get debug.write
    }

    childs:
    (
        [ui.kind: 'ui.label' name: 'lblStatus' text: ""]
        [ui.kind: 'ui.button' label: "Quit" onClick: { false window.hide }]
    )
]
window.show -> '$r'
```

---

## Primitives

***

### `window.show`

Shows a window and blocks MOGWAI execution until the window is closed.

```
[window definition record] window.show
```

Pushes a result record onto the stack when the window closes:

| Key | Type | Description |
|---|---|---|
| `window:` | Name | Name of the window that was displayed |
| `status:` | Any | Value passed to `window.hide`, or `null` if closed via the X button |

```mogwai
[name: 'main' title: "My App" childs: (...)]
window.show -> '$r'

$r window: get ?   # 'main'
$r status: get ?   # value passed to window.hide, or null
```

> `window.show` raises `MW.7` (operation not supported) if called while a window is already active. Always use `window.hide` to close the current window before opening another.

***

### `window.hide`

Closes the current window and passes a status value to the caller of `window.show`.

```
value window.hide
```

The `value` argument is mandatory. Use `null` or `false` if no meaningful status is needed.

```mogwai
# Close with a boolean status
true  window.hide

# Close with a name
'settings' window.hide

# Close with no meaningful status
false window.hide
null  window.hide
```

> `window.hide` raises `MW.20` (too few arguments) if called without a value on the stack.

***

### `window.update`

Updates a property of a named component at runtime. Must be called from within an event handler or a timer callback (runs on the TG thread).

```
[! name: 'componentName' property: value] window.update
```

```mogwai
# Update a label text
[! name: 'lblStatus' text: "Processing..."] window.update

# Update a progress bar
[! name: 'progress1' value: 75] window.update

# Update a list
[! name: 'myList' items: ("Alice" "Bob" "Charlie")] window.update
```

See [Components](components.md) for the list of updatable properties per component type.

***

### `window.active`

Pushes `true` if a window is currently displayed, `false` otherwise.

```
window.active
```

```mogwai
window.active ?   # true or false
```

***

### `window.current`

Pushes the name of the currently displayed window as a `name` type, or an empty name if no window is active.

```
window.current
```

```mogwai
window.current ?   # 'main' (or empty)

# Typical use in a timer
timer 'clock' every 1000 do
{
    if (window.current 'main' ==) then
    {
        now ->date -> '$h'
        "{! $h hour: get}:{! $h minute: get}:{! $h second: get}" eval -> '$t'
        [! name: 'lblClock' text: @$t] window.update
    }
}
```

---

## Window navigation

Only one window can be active at a time. The recommended pattern is a **main navigation loop** in the script:

```mogwai
mogwai.reset

true -> '$running'

while ($running) do
{
    [name: 'menu' title: "Main Menu" childs: (
        [ui.kind: 'ui.button' label: "Data Entry" onClick: { 'entry'   window.hide }]
        [ui.kind: 'ui.button' label: "Reports"    onClick: { 'reports' window.hide }]
        [ui.kind: 'ui.button' label: "Quit"        onClick: { false     window.hide }]
    )]
    window.show -> '$r'

    switch
    {
        ($r status: get 'entry'   ==) then { "entry.mog"   run }
        ($r status: get 'reports' ==) then { "reports.mog" run }
        (true)                         then { false -> '$running' }
    }
}
```

> Opening a new window from inside an `onClick:` handler (while a window is already active) is not supported and will raise `MW.7`. Always use `window.hide` first.

---

## Menu bar

A menu bar is defined with the `menu:` key — a list of card records, each containing a `title:` and a list of `items:`:

```mogwai
[
    name: 'main'
    title: "My App"

    menu:
    (
        [
            title: "File"
            items:
            (
                [label: "Open"  key: "o" onClick: { 'open'  window.hide }]
                [label: "Save"  key: "s" onClick: { 'save'  window.hide }]
                [ui.kind: 'separator']
                [label: "Quit"  key: "q" onClick: { false   window.hide }]
            )
        ]
        [
            title: "Help"
            items:
            (
                [label: "About" onClick: { 'about' window.hide }]
            )
        ]
    )

    childs: ( ... )
]
window.show drop
```

> **`key:` shortcut rules:**
> - Lowercase letter (e.g. `"o"`) → `Ctrl+O`
> - Uppercase letter (e.g. `"O"`) → `Ctrl+Shift+O`
> - More than one character → shortcut ignored, item still appears in the menu

> **Layout note:** when a `menu:` is present, child components automatically start at `Y=1` to avoid overlapping the menu bar. Use `y:` to position a component further down if needed.

---

## Status bar

A status bar is defined with the `statusbar:` key — a list of strings:

```mogwai
[
    name: 'main'
    title: "My App"
    statusbar: ("F1 Help" "F10 Quit" "GIZMO v0.1.0")
    childs: ( ... )
]
window.show drop
```

Status bar items are automatically registered with reserved names and can be updated at runtime via `window.update`:

| Item | Reserved name |
|---|---|
| First item | `statusbar` |
| Second item | `statusbar.1` |
| Third item | `statusbar.2` |
| ... | ... |

The updatable property is `title:`:

```mogwai
# Update the first status bar item
[! name: 'statusbar' title: "Ready"] window.update

# Update the second item
[! name: 'statusbar.1' title: "Ln 42  Col 5"] window.update
```

---

*Previous: [Getting started](getting-started.md) · Next: [Components →](components.md)*
