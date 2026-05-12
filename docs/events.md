# Events

Events allow components and windows to react to user interactions and lifecycle changes.

---

## How events work

When a user interacts with a component (clicks a button, types in a field, selects a list item...), GIZMO executes the corresponding event handler.

Before execution, GIZMO **injects a `ui.eventData` record** into the handler's local context. This record contains information about the event (the new value, the selected index, etc.).

```mogwai
[
    ui.kind: 'ui.edit'
    name: 'search'
    onChange:
    {
        # ui.eventData is automatically available here
        ui.eventData text: get -> '$query'
        # ...
    }
]
```

---

## Event syntax

Event handlers can be written using either syntax:

```mogwai
# Standard braces { } — recommended, works on all keyboards
onClick: { "Clicked!" ? }

# MOGWAI function literal « » — equivalent, requires Alt+174 / Alt+175
onClick: « "Clicked!" ? »
```

Both are equivalent. The `{ }` syntax is preferred for its universal keyboard accessibility.

---

## Global variables — critical rule

Event handlers are **functions** with their own local scope. Any variable shared between the main script and an event handler must be **global** (prefixed with `$`).

```mogwai
# ✗ Does NOT work — 'data' is local, invisible inside onClick:
[filename: "service.exe"] process.exec -> 'data'
[ui.kind: 'ui.button' onClick: { data output: get ? }]

# ✓ Correct — '$data' is global, accessible everywhere
[filename: "service.exe"] process.exec -> '$data'
[ui.kind: 'ui.button' onClick: { $data output: get ? }]
```

> **Rule**: any variable read inside an event handler must start with `$`.
>
> **Exception**: `ui.eventData` is injected automatically into the local context of every event handler — no `$` needed.

---

## Component events

***

### `onClick:`

Fires when a button is clicked or when Enter is pressed while the button has focus.

**Components:** `ui.button`

```mogwai
[
    ui.kind: 'ui.button'
    label: "Validate"
    onClick:
    {
        [name: 'txtName'] ui.gprop -> '$r'
        $r text: get -> '$name'
        "Hello, {! $name} !" eval ?
    }
]
```

**`ui.eventData`:** empty record `[]`

***

### `onChange:`

Fires when the value of a component changes.

**Components:** `ui.edit`, `ui.password`, `ui.integer`, `ui.float`, `ui.multiline`, `ui.check`, `ui.radio`, `ui.combo`

```mogwai
# Text field — fires on every keystroke
[
    ui.kind: 'ui.edit'
    name: 'search'
    onChange: { ui.eventData text: get debug.write }
]

# Checkbox — fires when checked/unchecked
[
    ui.kind: 'ui.check'
    label: "Enable notifications"
    onChange: { ui.eventData checked: get debug.write }
]

# Radio group — fires when selection changes
[
    ui.kind: 'ui.radio'
    options: ("Small" "Medium" "Large")
    onChange: { ui.eventData index: get debug.write }
]

# Dropdown — fires when selection changes
[
    ui.kind: 'ui.combo'
    options: ("Option A" "Option B" "Option C")
    onChange:
    {
        ui.eventData index: get -> '$idx'
        ui.eventData value: get -> '$val'
        "{! $idx} : {! $val}" eval ?
    }
]
```

**`ui.eventData` per component:**

| Component | Keys |
|---|---|
| `ui.edit`, `ui.password`, `ui.integer`, `ui.float`, `ui.multiline` | `text:` |
| `ui.check` | `checked:` |
| `ui.radio` | `index:` *(1-based)* |
| `ui.combo` | `index:` *(1-based)*, `value:` |

***

### `onValidate:`

Fires when the user presses Enter to confirm the value of a text field.

**Components:** `ui.edit`, `ui.password`, `ui.integer`, `ui.float`

```mogwai
[
    ui.kind: 'ui.edit'
    name: 'txtSearch'
    label: "Search:"
    onValidate:
    {
        ui.eventData text: get -> '$query'
        # perform search with $query
    }
]
```

**`ui.eventData`:** `[text: "..."]`

> Use `onValidate:` when you want to react only when the user confirms their input (Enter), rather than on every keystroke (`onChange:`).

***

### `onSelect:`

Fires when the selected item changes in a list or table.

**Components:** `ui.listview`, `ui.tableview`

```mogwai
# List
[
    ui.kind: 'ui.listview'
    name: 'items'
    items: ("Alice" "Bob" "Charlie")
    onSelect:
    {
        ui.eventData value: get -> '$name'
        ui.eventData index: get -> '$idx'
        "{! $idx}: {! $name}" eval -> '$msg'
        [! name: 'lblStatus' text: @$msg] window.update
    }
]

# Table
[
    ui.kind: 'ui.tableview'
    name: 'people'
    columns: ("Name" "City")
    rows: (("Alice" "Paris") ("Bob" "Lyon"))
    onSelect:
    {
        ui.eventData row: get -> '$row'
        $row 0 get -> '$name'
        $row 1 get -> '$city'
    }
]
```

**`ui.eventData` for `ui.listview`:** `[index: N value: "text"]` *(index is 1-based)*

**`ui.eventData` for `ui.tableview`:** `[index: N row: ("cell0" "cell1" ...)]` *(index is 1-based)*

***

### `onActivate:`

Fires when the user activates an item (Enter key or double-click).

**Components:** `ui.listview`, `ui.tableview`

```mogwai
[
    ui.kind: 'ui.listview'
    name: 'files'
    items: ("file1.txt" "file2.txt")
    onActivate:
    {
        ui.eventData value: get -> '$file'
        # open $file
    }
]
```

**`ui.eventData`:** same structure as `onSelect:`

> Use `onActivate:` for "open" or "confirm" actions, and `onSelect:` for live preview or status updates.

---

## Window lifecycle events

***

### `onShow:`

Fires after the window is built and its components are registered, but **before** the window is displayed to the user.

Use `onShow:` to initialize component values, load data, or pre-fill fields.

```mogwai
[
    name: 'main'
    title: "Dashboard"
    onShow:
    {
        # Load data before the user sees the window
        [filename: "data.exe" arguments: "--list"] process.exec -> '$data'
        $data output: get json-> -> '$items'
        [! name: 'lstItems' items: @$items] window.update

        [! name: 'lblStatus' text: "Data loaded"] window.update
    }
    childs:
    (
        [ui.kind: 'ui.listview' name: 'lstItems' items: ()]
        [ui.kind: 'ui.label'   name: 'lblStatus' text: "Loading..."]
        [ui.kind: 'ui.button'  label: "Close" onClick: { false window.hide }]
    )
]
window.show drop
```

**`ui.eventData`:** `[window: 'windowname']`

> If `onShow:` raises an error, the window is not displayed and the error propagates normally.

***

### `onHide:`

Fires after the window is closed, before the result record is pushed onto the stack.

Use `onHide:` to save state, release resources, stop timers, or free OOP instances.

```mogwai
[
    name: 'main'
    title: "Editor"
    onHide:
    {
        ui.eventData status: get -> '$status'

        # Save if closed with 'save' status
        if ($status 'save' ==) then
        {
            [name: 'txtContent'] ui.gprop -> '$r'
            $r text: get -> '$content'
            # save $content...
        }

        # Always free resources
        $myInstance free
    }
    childs: ( ... )
]
window.show drop
```

**`ui.eventData`:** `[window: 'windowname' status: value]`
*(`status:` is the value passed to `window.hide`, or `null` if the window was closed via the X button)*

> `onHide:` is only executed if the window closed without a pump error. It is skipped if an unhandled error occurred during the window session.

---

## Timers and UI updates

MOGWAI timers can update the UI at any time. The timer callback runs inside the MOGWAI engine pump (every 50ms), so it can safely call `window.update`.

```mogwai
mogwai.reset

timer 'clock' every 1000 do
{
    if (window.current 'main' ==) then
    {
        now ->date -> '$h'
        "{! $h hour: get}:{! $h minute: get}:{! $h second: get}" eval -> '$time'
        [! name: 'lblClock' text: @$time] window.update
    }
}

[
    name: 'main'
    title: "Clock"
    childs:
    (
        [ui.kind: 'ui.label' name: 'lblClock' text: "00:00:00"]
        [ui.kind: 'ui.button' label: "Quit" onClick: { false window.hide }]
    )
]

'clock' timer.start
window.show drop
```

> Use `window.current` inside a timer to check which window is active before calling `window.update` — the timer may still fire after a window has closed.

---

*Previous: [Components](components.md) · Next: [Dialogs →](dialogs.md)*
