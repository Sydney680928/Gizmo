# Dialogs

GIZMO provides three modal dialog primitives. All three block execution until the user closes the dialog, and push a result record onto the stack.

---

### `dialog.show`

Shows a modal dialog with custom components and buttons.

```
[dialog definition record] dialog.show
```

> `dialog.show` must be called from within an active window (from `onClick:`, `onShow:`, or any event handler). Calling it outside a window raises `MW.7`.

The dialog definition record accepts the following keys:

| Key | Type | Description |
|---|---|---|
| `title:` | String | Dialog title |
| `childs:` | List | Component definitions — including the `ui.buttons` row |
| `forecolor:` | Name | Dialog text color |
| `backcolor:` | Name | Dialog background color |
| `focusForecolor:` | Name | Focused component text color |
| `focusBackcolor:` | Name | Focused component background color |

The `ui.buttons` component must be placed **inside `childs:`** as the last item:

```mogwai
[
    title: "Nouveau contact"
    backcolor: 'color.black'
    forecolor: 'color.white'
    focusForecolor: 'color.black'
    focusBackcolor: 'color.brightcyan'

    childs:
    (
        [ui.kind: 'ui.edit'     name: 'name'  label: "Nom :"    text: ""]
        [ui.kind: 'ui.edit'     name: 'email' label: "Email :"  text: ""]
        [ui.kind: 'ui.password' name: 'pwd'   label: "Mot de passe :" text: ""]
        [
            ui.kind: 'ui.combo'
            name: 'role'
            label: "Rôle :"
            options: ("Client" "Fournisseur" "Partenaire")
            index: 1
        ]
        [ui.kind: 'ui.check' name: 'active' label: "Actif" checked: true]
        [ui.kind: 'ui.buttons' items: ("OK" "Annuler")]
    )
] dialog.show -> '$r'
```

If `ui.buttons:` is omitted, a single "OK" button is added automatically.

Pushes a result record onto the stack:

| Key | Type | Description |
|---|---|---|
| `ui.status:` | Number | 1-based index of the pressed button (1 = first button). Last button index if Esc was pressed. |
| *(field names)* | Any | One key per named component, containing the component's value |

```mogwai
$r ui.status: get -> '$btn'   # 1 = OK, 2 = Annuler

if ($btn 1 ==) then
{
    $r name:   get ?
    $r email:  get ?
    $r active: get ?
}
```

> The result record contains only components that have a `name:` key. Labels and separators are excluded.

> MOGWAI timers remain active while a dialog is displayed.

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
