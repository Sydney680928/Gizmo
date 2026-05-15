# Getting started

## What is GIZMO?

GIZMO is a standalone executable that runs MOGWAI scripts and renders their user interface as a **TUI (Terminal User Interface)** powered by Terminal.Gui v2.

You write a `.mog` script that describes windows, components, and events — GIZMO handles the rendering and interaction loop.

```
┌─ hello.mog ──────────────────────────┐
│  [name: 'main' title: "Hello" ...]   │
│  window.show                          │
└───────────────────────────────────────┘
           │
           ▼
    gizmo hello.mog
           │
           ▼
┌─ Terminal ────────────────────────────┐
│  Hello                                │
│  ┌──────────────────────────────────┐ │
│  │ Your name: [____________]        │ │
│  │             [ Say hello ]        │ │
│  └──────────────────────────────────┘ │
└───────────────────────────────────────┘
```

---

## Installation

Download the binary for your platform from the [Releases](https://github.com/Sydney680928/Gizmo/releases) page:

| Platform | File |
|---|---|
| Windows x64 | `gizmo-win-x64.exe` |
| macOS x64 (Intel) | `gizmo-osx-x64` |
| macOS arm64 (Apple Silicon) | `gizmo-osx-arm64` |
| Linux x64 | `gizmo-linux-x64` |
| Linux arm64 | `gizmo-linux-arm64` |

All binaries are **self-contained** — no .NET runtime installation required.

On macOS and Linux, make the binary executable:
```bash
chmod +x gizmo-osx-arm64
```

---

## Running a script

```bash
gizmo yourapp.mog
```

GIZMO loads the script, executes it, and renders any windows defined in it.

---

## Interactive REPL

Run GIZMO without arguments to enter interactive mode:

```bash
gizmo
```

You can type MOGWAI expressions directly, one line at a time.

| Command | Description |
|---|---|
| `"hello.mog" run` | Load and execute a GIZMO script file |
| `studio` | Connect to VS Code (via MOGWAI extension) or MOGWAI Studio |
| `help` | Show usage instructions |
| `bye` | Exit GIZMO |

---

## Tooling — VS Code

The recommended way to write GIZMO scripts is **VS Code** with the **MOGWAI language extension**:

- Syntax highlighting for `.mog` files
- Code completion
- Inline error reporting
- **Full debug support** — step through your script, inspect the stack and variables

Once the extension is installed, connect it to a running GIZMO instance with the `studio` command.

---

## Folder structure

When GIZMO starts, the MOGWAI runtime automatically creates the following folder structure in the current user's Documents folder:

```
Documents/
└── MOGWAI/
    ├── Programs/    ← your .mog scripts
    ├── Usings/      ← plugins and extensions
    └── Files/       ← data files read and written by your scripts
```

The paths to these folders are accessible from any MOGWAI script:

| Primitive | Description |
|---|---|
| `path.programs` | Path to the Programs folder |
| `path.files` | Path to the Files folder |
| `path.usings` | Path to the Usings folder |

To build a full path from multiple segments, use `path.make` (equivalent to `Path.Combine` in C#):

```mogwai
# Build the path to themes/dark.mog inside Programs
(! path.programs "themes" "dark.mog") path.make
# → "C:\Users\<user>\Documents\MOGWAI\Programs\themes\dark.mog"

# Include the file
(! path.programs "themes" "dark.mog") path.make mogwai.include
```

> Place your scripts in `Programs/`, your data files in `Files/`. Sub-folders are allowed — for example `Programs/themes/` for themes, `Programs/modules/` for shared code.

---

## Your first script

Create a file `hello.mog`:

```mogwai
mogwai.reset

[
    name: 'main'
    title: "My first GIZMO app"

    childs:
    (
        [ui.kind: 'ui.edit' name: 'txtName' label: "Your name:" text: ""]

        [
            ui.kind: 'ui.button'
            label: "Say hello"
            onClick:
            {
                [name: 'txtName'] ui.gprop -> '$r'
                "Hello, {! $r text: get} !" eval -> '$msg'
                [! ui.kind: 'ui.info' title: "Hello" text: @$msg] msgbox.show
                drop
            }
        ]

        [ui.kind: 'ui.button' label: "Quit" onClick: { false window.hide }]
    )
]

window.show drop
```

Run it:
```bash
gizmo hello.mog
```

---

## The MOGWAI language

GIZMO uses **MOGWAI** as its scripting engine. MOGWAI is a stack-based RPN language — values are pushed onto a stack, and primitives operate on them.

```mogwai
2 3 +      # pushes 2, pushes 3, adds → 5 on stack
5 ?        # prints top of stack → 5
"hello" ?  # prints "hello"
```

Key syntax rules:

| Syntax | Meaning |
|---|---|
| `42 -> 'A'` | Store 42 in variable A |
| `@A` | Push value of variable A (static sigil) |
| `$A` | Global variable (accessible in event handlers) |
| `{ ... }` | Code block (can be used as event handler) |
| `« ... »` | Function literal (equivalent to `{ }` for events) |
| `[key: value ...]` | Record (key-value structure) |
| `[! key: value ...]` | Auto-evaluated record (values are evaluated when record is created) |
| `"text {! code }"` | String interpolation (`!` is mandatory) |
| `# comment` | Comment |

Full MOGWAI language documentation: [mogwai.eu.com](https://mogwai.eu.com)

---

## Important: variable scope in event handlers

Event handlers (`onClick:`, `onChange:`, etc.) are **functions** with their own local scope. To share a variable between the main script and an event handler, it must be **global** (prefixed with `$`).

```mogwai
# ✗ Does NOT work — 'r' is local, not visible inside onClick:
[filename: "data.exe"] process.exec -> 'r'
[ui.kind: 'ui.button' onClick: { r output: get ? }]

# ✓ Correct — '$r' is global, accessible everywhere
[filename: "data.exe"] process.exec -> '$r'
[ui.kind: 'ui.button' onClick: { $r output: get ? }]
```

> **Rule**: any variable read inside an event handler must start with `$`.
>
> Exception: `ui.eventData` is injected automatically into the local context of every event handler — no `$` needed for it.

---

*Next: [Windows →](windows.md)*
