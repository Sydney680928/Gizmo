# Scripting

---

### `run`

Loads and executes a MOGWAI script file (`.mog`). The script runs in the same engine context as the caller.

```
"path/to/script.mog" run
```

```mogwai
# Run a sub-script
"modules/settings.mog" run

# Dynamic path
"modules/" $moduleName + ".mog" + run
```

> `run` raises `MW.1` (parse error) if the file cannot be parsed, and `MW.72` (file operation error) if the file cannot be read.

---

## Property primitives

***

### `ui.gprop`

Gets properties of a named component as a record.

```
[name: 'componentName'] ui.gprop
[name: 'componentName' prop: 'propertyName'] ui.gprop
```

Without `prop:`, returns a record with all available properties of the component.
With `prop:`, returns the single property value.

```mogwai
# Get all properties
[name: 'txtName'] ui.gprop -> '$r'
$r text: get ?

# Get a single property
[name: 'txtName' prop: 'text'] ui.gprop ?

# Common patterns
[name: 'chk1']    ui.gprop -> '$r'  $r checked: get -> '$isChecked'
[name: 'combo1']  ui.gprop -> '$r'  $r index: get   -> '$idx'
[name: 'lst1']    ui.gprop -> '$r'  $r index: get   -> '$selected'
[name: 'bar1']    ui.gprop -> '$r'  $r value: get   -> '$progress'
```

**Properties returned per component type:**

| Component | Properties |
|---|---|
| `ui.label`, `ui.edit`, `ui.password`, `ui.multiline` | `text:` |
| `ui.integer`, `ui.float` | `text:` *(string — convert with `->int` or `->number`)* |
| `ui.check` | `checked:` |
| `ui.radio` | `index:` *(1-based)* |
| `ui.combo` | `index:` *(1-based)*, `value:` |
| `ui.listview` | `index:` *(1-based)* |
| `ui.tableview` | `index:` *(1-based)* |
| `ui.progress` | `value:` *(0–100)* |

***

### `ui.sprop`

Sets a property of a named component. Equivalent to `window.update` but intended for use from code rather than from event data.

```
[name: 'componentName' property: value] ui.sprop
```

```mogwai
[name: 'lblStatus' text: "Processing..."] ui.sprop
[name: 'progress1' value: 50]             ui.sprop
```

---

## Timers and dynamic UI

Timers let you update the UI periodically without user interaction. MOGWAI timers run inside the GIZMO engine pump — they can safely call `window.update`.

```mogwai
mogwai.reset

timer 'refresh' every 2000 do
{
    if (window.current 'dashboard' ==) then
    {
        [filename: "status.exe"] process.exec -> '$result'
        $result output: get -> '$status'
        [! name: 'lblStatus' text: @$status] window.update
    }
}

[
    name: 'dashboard'
    title: "Dashboard"
    childs:
    (
        [ui.kind: 'ui.label' name: 'lblStatus' text: "Waiting..."]
        [ui.kind: 'ui.button' label: "Quit" onClick: { false window.hide }]
    )
]

'refresh' timer.start
window.show drop
```

> Always guard timer callbacks with `window.current` to avoid calling `window.update` after the window has closed.

---

## Extending GIZMO with external processes

GIZMO uses the MOGWAI `process.exec` primitive to communicate with external executables. This is the recommended way to extend GIZMO with custom logic written in any language.

***

### `process.exec`

Launches an external process, sends data to its stdin (optional), waits for completion, and captures stdout and stderr.

```
[filename: "..." arguments: "..." input: "..."] process.exec
```

| Key | Type | Required | Description |
|---|---|---|---|
| `filename:` | String | Yes | Path to the executable |
| `arguments:` | String | No | Command-line arguments |
| `workingDirectory:` | String | No | Working directory |
| `input:` | String | No | Data sent to stdin |

Pushes a result record:

| Key | Type | Description |
|---|---|---|
| `status:` | Number | Exit code (0 = success) |
| `output:` | String | Content of stdout (trailing newline trimmed) |
| `error:` | String | Content of stderr (trailing newline trimmed) |

```mogwai
# Simple command
[filename: "dotnet" arguments: "--version"] process.exec -> '$r'
$r output: get ?   # "10.0.203"

# With stdin input
[filename: "myservice.exe" input: "42"] process.exec -> '$r'

if ($r status: get 0 ==) then
{
    $r output: get ?
}
else
{
    $r error: get ?
}

# JSON exchange
[! filename: "myservice.exe" input: {! [value: 42] ->json}] process.exec -> '$r'
$r output: get json-> -> '$data'
$data result: get ?
```

> `process.exec` is a MOGWAI primitive, not specific to GIZMO. It is available in any MOGWAI host.

---

## Navigation patterns

### Main loop

```mogwai
mogwai.reset

true -> '$running'

while ($running) do
{
    [name: 'menu' title: "Main Menu" childs: (
        [ui.kind: 'ui.button' label: "Entry"   onClick: { 'entry'   window.hide }]
        [ui.kind: 'ui.button' label: "Reports" onClick: { 'reports' window.hide }]
        [ui.kind: 'ui.button' label: "Quit"    onClick: { false     window.hide }]
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

### Data passing between windows

Use global variables (`$`) to share data across windows:

```mogwai
# Window 1 — fill a form
[
    name: 'form'
    title: "New user"
    onHide:
    {
        if (ui.eventData status: get 'save' ==) then
        {
            [name: 'txtName'] ui.gprop -> '$formData'
        }
    }
    childs:
    (
        [ui.kind: 'ui.edit'   name: 'txtName' label: "Name:" text: ""]
        [ui.kind: 'ui.button' label: "Save"   onClick: { 'save'  window.hide }]
        [ui.kind: 'ui.button' label: "Cancel" onClick: { 'cancel' window.hide }]
    )
]
window.show -> '$r'

# Window 2 — display the result
if ($r status: get 'save' ==) then
{
    [!
        name: 'confirm'
        title: "Confirm"
        onShow:
        {
            [! name: 'lblName' text: {! $formData text: get}] window.update
        }
        childs:
        (
            [ui.kind: 'ui.label' name: 'lblName' text: ""]
            [ui.kind: 'ui.button' label: "OK" onClick: { true window.hide }]
        )
    ]
    window.show drop
}
```

---

*Previous: [Dialogs](dialogs.md)*
