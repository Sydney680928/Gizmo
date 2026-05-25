# GIZMO — Examples

This folder contains ready-to-run GIZMO scripts demonstrating the main features of the framework.

All examples use the built-in dark theme (black background, white text, cyan accents).

---

## Running an example

```bash
gizmo examples/clock.mog
gizmo examples/calculator.mog
gizmo examples/todo.mog
gizmo examples/showcase.mog
gizmo examples/formula.mog
```

---

## `clock.mog` — Digital clock

A real-time clock updated every second using a MOGWAI timer.

**Features demonstrated:**
- MOGWAI timer (`timer ... every 1000 do`)
- `window.update` from a timer callback
- Number formatting with `->format`
- `ui.label`, `ui.frame`, `ui.button`

```
┌─ Time ───────────────────────┐
│ 14:32:07                     │
└──────────────────────────────┘
┌─ Date ───────────────────────┐
│ 14/05/2026                   │
└──────────────────────────────┘
           [ Close ]
```

---

## `calculator.mog` — RPN calculator

A stack-based RPN calculator in the spirit of the HP calculators that inspired MOGWAI.

Enter a number in the **Value** field and press **ENTER ↵** (or the Enter key) to push it onto the stack. Then press an operation button to compute.

**Features demonstrated:**
- Global variable helpers (`$upd`, `$push` stored as code blocks)
- `guard { } else { }` for safe number conversion
- `ui.edit` with `onValidate:`
- `ui.label`, `ui.frame`, `ui.button` with custom colors
- Stack registers (X = top, Y = second)

```
┌─ Stack ──────────────────────┐
│ Y:  3                        │
│ X:  4                        │
└──────────────────────────────┘
Value: [___________]
───────────────────────────────
[ ENTER ↵ ]
[ +   add ]
[ -   subtract ]
[ *   multiply ]
[ /   divide ]
[ +/-  negate ]
[ CL  clear ]
───────────────────────────────
[ Close ]
```

**Example session:**
```
Type 3  → ENTER   →  X: 3,  Y: 0
Type 4  → ENTER   →  X: 4,  Y: 3
Click + →          →  X: 7,  Y: 0
Type 10 → ENTER   →  X: 10, Y: 7
Click / →          →  X: 0.7, Y: 0
```

---

## `todo.mog` — Todo list

A simple task manager with add, delete, and clear-all functionality.

Type a task in the **New task** field and press **Add** (or the Enter key) to add it to the list. Select a task and press **Delete selected** to remove it. **Clear all** asks for confirmation before deleting everything.

**Features demonstrated:**
- MOGWAI list manipulation (`liste element +`, `get`)
- `ui.listview` with `onSelect:`
- `dialog.show` for confirmation
- Global helper code blocks (`$refresh`, `$addTask`)
- `ui.edit`, `ui.button` with color variants (green add, red delete, yellow clear)
- Dynamic counter label

```
0 task(s)
┌──────────────────────────────┐
│ Buy groceries                │
│ Call dentist                 │
│ ▶ Fix the bug                │
│                              │
└──────────────────────────────┘
New task: [___________]
[ Add ]
[ Delete selected ]
[ Clear all ]
───────────────────────────────
[ Close ]
```

---

## `showcase.mog` — Component demo

A visual demo that displays all available GIZMO components in a single window. No actions are wired to the buttons — this script is useful to understand how components are declared and laid out, and to explore the dark theme rendering.

**Components shown:** `ui.label`, `ui.edit`, `ui.password`, `ui.integer`, `ui.float`, `ui.check`, `ui.radio`, `ui.combo`, `ui.tableview`, `ui.progress`, `ui.frame`, `ui.separator`, `ui.button`

---

## `formula.mog` — Formula calculator

A mathematical function plotter — enter a formula with variable `X`, define a range and a step, and compute a table of `(X, Y)` values.

**Features demonstrated:**
- `->code` + `eval` — compile a user-entered string into executable MOGWAI code
- `guard { } else { }` + `switch` for input validation
- `(.number) check` to verify the formula pushes a numeric result
- `after 0 do { }` to yield the TUI thread and allow the interface to refresh before a blocking computation
- `ui.tableview` updated at runtime via `window.update`
- `statusbar` updated at runtime

```
FORMULA CALCULATOR
──────────────────────────────────────────
Formula: [2 X * 7 + X sin +            ]
Start:   [-10                           ]
End:     [10                            ]
Step:    [1                             ]
                [ Calculate ]
┌─────────────────────────────────────────┐
│ X       │ Y                             │
│ -10     │ -13.456                       │
│ -9      │ -10.412                       │
│  ...    │  ...                          │
└─────────────────────────────────────────┘
                [  Exit  ]
──────────────────────────────────────────
Ready - Calculation time 12 ms
```

---

## Writing your own scripts

Start from any example and adapt it. The key rules:

- Include a theme at the top: `"themes/dark.mog" include`
- Variables shared across event handlers must be global: `-> '$myVar'`
- `dialog.show` must be called from within an active window
- For progressive UI updates (progress bars...), use a timer — not a `for` loop

See the [documentation](../docs/README.md) for the full reference.
