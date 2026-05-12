# Dialogs

GIZMO provides three modal dialog primitives. All three block execution until the user closes the dialog, and push a result record onto the stack.

---

### `dialog.show`

Shows a modal dialog with custom components and buttons.

```
[dialog definition record] dialog.show
```

The dialog definition record accepts the following keys:

| Key | Type | Description |
|---|---|---|
| `title:` | String | Dialog title |
| `childs:` | List | Component definition records (same as window `childs:`) |
| `ui.buttons:` | Record | Button bar definition — `[ui.kind: 'ui.buttons' items: ("OK" "Cancel")]` |

If `ui.buttons:` is omitted, a single "OK" button is added automatically.

Pushes a result record onto the stack:

| Key | Type | Description |
|---|---|---|
| `ui.status:` | Number | 1-based index of the pressed button (1 = first button). Last button index if Esc was pressed. |
| *(field names)* | Any | One key per named component, containing the component's value |

```mogwai
[
    title: "User info"
    childs:
    (
        [ui.kind: 'ui.edit' name: 'firstName' label: "First name:" text: ""]
        [ui.kind: 'ui.edit' name: 'lastName'  label: "Last name:"  text: ""]
        [ui.kind: 'ui.check' name: 'active'   label: "Active"      checked: true]
    )
    [ui.kind: 'ui.buttons' items: ("Save" "Cancel")]
] dialog.show -> '$r'

$r ui.status: get -> '$btn'   # 1 = Save, 2 = Cancel

if ($btn 1 ==) then
{
    $r firstName: get ?
    $r lastName:  get ?
    $r active:    get ?
}
```

> The result record contains only components that have a `name:` key. Labels and separators are excluded.

***

### `msgbox.show`

Shows a simple message box.

```
[msgbox definition record] msgbox.show
```

| Key | Type | Default | Description |
|---|---|---|---|
| `ui.kind:` | Name | `'ui.info'` | `'ui.info'` for an OK box, `'ui.confirm'` for a Yes/No box |
| `title:` | String | `""` | Dialog title |
| `text:` | String | `""` | Message text |

Pushes a result record: `[ui.status: N]`

| `ui.status:` value | Meaning |
|---|---|
| `1` | OK (info) or Yes (confirm) |
| `2` | No (confirm) |

```mogwai
# Info box
[! ui.kind: 'ui.info' title: "Success" text: "File saved."] msgbox.show
drop   # discard result

# Confirm box
[! ui.kind: 'ui.confirm' title: "Confirm" text: "Delete this record?"] msgbox.show -> '$r'

if ($r ui.status: get 1 ==) then
{
    # user clicked Yes
}
```

> Use `[! ...]` (auto-evaluated record) when the `text:` contains a variable expression like `{! $msg}`.

***

### `filedialog.show`

Shows a file open / save / folder selection dialog.

```
[filedialog definition record] filedialog.show
```

| Key | Type | Default | Description |
|---|---|---|---|
| `mode:` | Name | `'open'` | `'open'` for file open, `'save'` for file save, `'folder'` for directory selection |
| `title:` | String | `""` | Dialog title |
| `filter:` | String | `"*"` | File filter (e.g. `"*.mog"`, `"*.txt"`) |

Pushes a result record: `[ui.status: N text: "path"]`

| Key | Type | Description |
|---|---|---|
| `ui.status:` | Number | `1` if a path was selected, `2` if cancelled |
| `text:` | String | Full path of the selected file or folder, or `""` if cancelled |

```mogwai
# Open file
[! mode: 'open' title: "Open script" filter: "*.mog"] filedialog.show -> '$r'

if ($r ui.status: get 1 ==) then
{
    $r text: get -> '$path'
    "{! $path} selected" eval ?
}

# Save file
[! mode: 'save' title: "Save as" filter: "*.txt"] filedialog.show -> '$r'

if ($r ui.status: get 1 ==) then
{
    $r text: get -> '$savePath'
}

# Select folder
[! mode: 'folder' title: "Select output directory"] filedialog.show -> '$r'
```

---

*Previous: [Events](events.md) · Next: [Scripting →](scripting.md)*
