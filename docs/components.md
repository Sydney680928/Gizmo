# Components

Components are the building blocks of a GIZMO window. Each component is defined as a record with a `ui.kind:` key that determines its type.

---

## Layout

Components are stacked **vertically** inside their container. The layout is automatic:

- Components with a `label:` key receive an external label aligned to the left
- Buttons are centered horizontally
- All other components fill the available width
- Use `height:` to control the height of scrollable components

### Explicit positioning — `x:` and `y:`

Use `x:` and `y:` to override the automatic layout and position a component at an absolute column/row:

| Key | Type | Description |
|---|---|---|
| `x:` | Number | Absolute column position (0-based). When specified, the component width switches to `auto` instead of `fill`. |
| `y:` | Number | Absolute row position (0-based). When specified, the component is placed at that row instead of being stacked below the previous one. |

```mogwai
# Two buttons side by side on row 5
[ui.kind: 'ui.button' label: "OK"     x: 15 y: 5 onClick: { true  window.hide }]
[ui.kind: 'ui.button' label: "Cancel" x: 25 y: 5 onClick: { false window.hide }]
```

> When placing multiple components on the same row, specify `y:` on each of them — the automatic stacking always places a component below the previous one if `y:` is absent.

---

## Common keys

Most components accept the following keys:

| Key | Type | Description |
|---|---|---|
| `ui.kind:` | Name | Component type (required) |
| `name:` | String | Component identifier — required to use `ui.gprop`, `ui.sprop`, `window.update` |
| `label:` | String | External label displayed to the left of the component |

---

## Colors

All components accept the following optional color keys:

| Key | Type | Description |
|---|---|---|
| `forecolor:` | Name | Text color in normal state |
| `backcolor:` | Name | Background color in normal state |
| `focusForecolor:` | Name | Text color when the component has focus |
| `focusBackcolor:` | Name | Background color when the component has focus |

If `focusForecolor:` / `focusBackcolor:` are not specified, they fall back to `forecolor:` / `backcolor:`.
If `forecolor:` / `backcolor:` are not specified, the component inherits its parent's colors.

**Available colors:**

| Name | Name |
|---|---|
| `color.black` | `color.darkgray` |
| `color.blue` | `color.brightblue` |
| `color.green` | `color.brightgreen` |
| `color.cyan` | `color.brightcyan` |
| `color.red` | `color.brightred` |
| `color.magenta` | `color.brightmagenta` |
| `color.yellow` | `color.brightyellow` |
| `color.white` | `color.gray` |

```mogwai
# Label with red text — inherits parent background
[ui.kind: 'ui.label' text: "Error!" forecolor: 'color.red']

# Button with full color scheme
[
    ui.kind: 'ui.button'
    label: "Delete"
    forecolor:      'color.brightred'
    backcolor:      'color.black'
    focusForecolor: 'color.white'
    focusBackcolor: 'color.red'
    onClick: { false window.hide }
]
```

Colors can also be changed at runtime via `window.update`:
```mogwai
[! name: 'lblStatus' forecolor: 'color.red' backcolor: 'color.black'] window.update
```

---

## Components reference

***

### `ui.label`

Displays static text. Updated at runtime via `window.update`.

```mogwai
[ui.kind: 'ui.label' name: 'status' text: "Ready"]
```

| Key | Type | Default | Description |
|---|---|---|---|
| `text:` | String | `""` | Text to display |

**`ui.gprop` returns:** `[text: "..."]`

**`window.update` accepts:** `text:`

***

### `ui.edit`

Single-line text input.

```mogwai
[ui.kind: 'ui.edit' name: 'txtName' label: "Name:" text: ""]
```

| Key | Type | Default | Description |
|---|---|---|---|
| `text:` | String | `""` | Initial text content |
| `label:` | String | — | External label |
| `onChange:` | Code | — | Fires on every keystroke |
| `onValidate:` | Code | — | Fires when Enter is pressed |

**`ui.gprop` returns:** `[text: "..."]`

**`window.update` accepts:** `text:`

***

### `ui.password`

Single-line text input with masked characters.

```mogwai
[ui.kind: 'ui.password' name: 'pwd' label: "Password:" text: ""]
```

| Key | Type | Default | Description |
|---|---|---|---|
| `text:` | String | `""` | Initial value (displayed masked) |
| `label:` | String | — | External label |
| `onChange:` | Code | — | Fires on every keystroke |
| `onValidate:` | Code | — | Fires when Enter is pressed |

**`ui.gprop` returns:** `[text: "..."]`

***

### `ui.multiline`

Multi-line text area.

```mogwai
[ui.kind: 'ui.multiline' name: 'notes' text: "" height: 6]
```

| Key | Type | Default | Description |
|---|---|---|---|
| `text:` | String | `""` | Initial text content |
| `height:` | Number | `4` | Number of visible rows |
| `onChange:` | Code | — | Fires when text changes |

**`ui.gprop` returns:** `[text: "..."]`

**`window.update` accepts:** `text:`

***

### `ui.integer`

Text input restricted to integer values.

```mogwai
[ui.kind: 'ui.integer' name: 'qty' label: "Quantity:" value: 0]
```

| Key | Type | Default | Description |
|---|---|---|---|
| `value:` | Number | `0` | Initial integer value |
| `label:` | String | — | External label |
| `onChange:` | Code | — | Fires on every keystroke |
| `onValidate:` | Code | — | Fires when Enter is pressed |

**`ui.gprop` returns:** `[text: "42"]` *(as string — convert with `->int` if needed)*

***

### `ui.float`

Text input for floating-point values. Uses invariant culture (`.` as decimal separator).

```mogwai
[ui.kind: 'ui.float' name: 'price' label: "Price:" value: 0.0]
```

| Key | Type | Default | Description |
|---|---|---|---|
| `value:` | Number | `0.0` | Initial float value |
| `label:` | String | — | External label |
| `onChange:` | Code | — | Fires on every keystroke |
| `onValidate:` | Code | — | Fires when Enter is pressed |

**`ui.gprop` returns:** `[text: "3.14"]` *(as string — convert with `->number` if needed)*

***

### `ui.check`

Checkbox with a label.

```mogwai
[ui.kind: 'ui.check' name: 'chkAgree' label: "I agree" checked: false]
```

| Key | Type | Default | Description |
|---|---|---|---|
| `label:` | String | `""` | Checkbox label |
| `checked:` | Boolean | `false` | Initial state |
| `onChange:` | Code | — | Fires when the state changes |

**`ui.gprop` returns:** `[checked: true]` or `[checked: false]`

**`window.update` accepts:** `checked:`

***

### `ui.radio`

Vertical radio button group (single selection).

```mogwai
[
    ui.kind: 'ui.radio'
    name: 'size'
    label: "Size:"
    options: ("Small" "Medium" "Large")
    index: 2
    onChange: { ui.eventData index: get ? }
]
```

| Key | Type | Default | Description |
|---|---|---|---|
| `options:` | List | `()` | List of option labels |
| `index:` | Number | `1` | Initially selected option (1-based) |
| `label:` | String | — | External label |
| `onChange:` | Code | — | Fires when selection changes |

**`ui.eventData` contains:** `[index: N]` *(1-based)*

**`ui.gprop` returns:** `[index: N]` *(1-based)*

**`window.update` accepts:** `index:`

***

### `ui.combo`

Dropdown list (single selection).

```mogwai
[
    ui.kind: 'ui.combo'
    name: 'country'
    label: "Country:"
    options: ("France" "Germany" "UK")
    index: 1
    onChange: { ui.eventData value: get ? }
]
```

| Key | Type | Default | Description |
|---|---|---|---|
| `options:` | List | `()` | List of option labels |
| `index:` | Number | `1` | Initially selected option (1-based) |
| `label:` | String | — | External label |
| `onChange:` | Code | — | Fires when selection changes |

**`ui.eventData` contains:** `[index: N value: "text"]`

**`ui.gprop` returns:** `[index: N value: "text"]`

***

### `ui.button`

Clickable button.

```mogwai
[
    ui.kind: 'ui.button'
    label: "Validate"
    onClick:
    {
        # handle click
    }
]
```

| Key | Type | Default | Description |
|---|---|---|---|
| `label:` | String | `""` | Button text |
| `onClick:` | Code | — | Fires on click or Enter |

***

### `ui.listview`

Scrollable single-column list.

```mogwai
[
    ui.kind: 'ui.listview'
    name: 'contacts'
    items: ("Alice" "Bob" "Charlie")
    height: 8
    onSelect:   { ui.eventData value: get ? }
    onActivate: { ui.eventData value: get ? }
]
```

| Key | Type | Default | Description |
|---|---|---|---|
| `items:` | List | `()` | List of string items |
| `height:` | Number | `10` | Number of visible rows |
| `onSelect:` | Code | — | Fires when selection changes |
| `onActivate:` | Code | — | Fires on Enter or double-click |

**`ui.eventData` contains:** `[index: N value: "text"]` *(index is 1-based)*

**`ui.gprop` returns:** `[index: N]`

**`window.update` accepts:** `items:`, `index:`

***

### `ui.tableview`

Multi-column table with scrolling and full-row selection.

```mogwai
[
    ui.kind: 'ui.tableview'
    name: 'employees'
    columns: ("Name" "Department" "City")
    rows:
    (
        ("Alice" "Engineering" "Paris")
        ("Bob"   "Marketing"   "Lyon")
    )
    height: 8
    onSelect:   { ui.eventData row: get -> '$row'  $row 0 get ? }
    onActivate: { ui.eventData row: get -> '$row'  $row 1 get ? }
]
```

| Key | Type | Default | Description |
|---|---|---|---|
| `columns:` | List | `()` | Column header labels |
| `rows:` | List of lists | `()` | Table data — each item is a list of cell values |
| `height:` | Number | `10` | Number of visible rows |
| `onSelect:` | Code | — | Fires when selected row changes |
| `onActivate:` | Code | — | Fires on Enter or double-click |

**`ui.eventData` contains:** `[index: N row: ("cell0" "cell1" ...)]` *(index is 1-based, row is a list of strings)*

**`ui.gprop` returns:** `[index: N]`

**`window.update` accepts:** `rows:`

```mogwai
# Replace all rows at runtime
[! name: 'valuesTable' rows: (("1" "3.14") ("2" "6.28"))] window.update

# Clear all rows
[name: 'valuesTable' rows: ()] window.update
```

***

### `ui.progress`

Progress bar. Value ranges from `0` to `100`.

```mogwai
[ui.kind: 'ui.progress' name: 'bar' value: 0 min: 0 max: 100]
```

| Key | Type | Default | Description |
|---|---|---|---|
| `value:` | Number | `0` | Initial value |
| `min:` | Number | `0` | Minimum value |
| `max:` | Number | `100` | Maximum value |

**`ui.gprop` returns:** `[value: N]` *(0–100)*

**`window.update` accepts:** `value:`

***

### `ui.frame`

Bordered container with a title. Used to group related components visually.

```mogwai
[
    ui.kind: 'ui.frame'
    title: "Personal info"
    height: 6
    childs:
    (
        [ui.kind: 'ui.edit' name: 'firstName' label: "First name:" text: ""]
        [ui.kind: 'ui.edit' name: 'lastName'  label: "Last name:"  text: ""]
    )
]
```

| Key | Type | Default | Description |
|---|---|---|---|
| `title:` | String | `""` | Frame title displayed in the border |
| `height:` | Number | `10` | Frame height |
| `childs:` | List | `()` | Nested component definitions |

> Components inside a `ui.frame` are registered normally — they can be accessed by `ui.gprop`, `ui.sprop`, and `window.update` using their `name:`.

***

### `ui.separator`

Horizontal line used to visually separate groups of components.

```mogwai
[ui.kind: 'ui.separator']
```

No additional keys.

---

*Previous: [Windows](windows.md) · Next: [Events →](events.md)*
