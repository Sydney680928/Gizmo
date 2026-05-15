# GIZMO — Themes

Themes are MOGWAI script files that define a set of global color variables used throughout your application. Including a theme at the top of your script gives all your windows and components a consistent look with minimal effort.

---

## Using a theme

GIZMO uses the standard MOGWAI folder structure. On Windows, the runtime automatically creates the following structure in the current user's Documents folder:

```
Documents/
└── MOGWAI/
    ├── Programs/    ← your .mog scripts (path.programs)
    ├── Usings/      ← plugins and extensions (path.usings)
    └── Files/       ← data files (path.files)
```

The recommended way to include a theme is to place the `themes/` folder inside `Programs/` and use `path.make` to build the path:

```mogwai
# Recommended — absolute path via path.programs
(! path.programs "themes" "dark.mog") path.make mogwai.include
```

> **Important**: the current working directory is the folder of the **GIZMO executable**, not the folder of the `.mog` script. Always use `path.programs` with `path.make` to build reliable absolute paths.

Then use the theme variables in your window definition:

```mogwai
(! path.programs "themes" "dark.mog") path.make mogwai.include

[!
    name: 'main'
    title: "My App"
    backcolor: @$theme.back
    forecolor: @$theme.fore
    focusForecolor: @$theme.focusFore
    focusBackcolor: @$theme.focusBack
    childs:
    (
        [!
            ui.kind: 'ui.button'
            label: "OK"
            forecolor: @$theme.btnFore
            backcolor: @$theme.btnBack
            focusForecolor: @$theme.btnFocusFore
            focusBackcolor: @$theme.btnFocusBack
            onClick: { false window.hide }
        ]
    )
]
window.show drop
```

> Use `mogwai.include` (not `run`) to load a theme — `mogwai.include` injects the theme variables into the current script context.

---

## Available variables

All themes define the following global variables:

| Variable | Description |
|---|---|
| `$theme.back` | Window background color |
| `$theme.fore` | Window foreground (text) color |
| `$theme.focusFore` | Foreground color of focused components |
| `$theme.focusBack` | Background color of focused components |
| `$theme.btnFore` | Button foreground color |
| `$theme.btnBack` | Button background color |
| `$theme.btnFocusFore` | Button foreground color when focused |
| `$theme.btnFocusBack` | Button background color when focused |
| `$theme.accent` | Accent color — highlights, titles, labels |
| `$theme.danger` | Danger color — destructive actions, errors |
| `$theme.success` | Success color — confirmations, positive feedback |
| `$theme.warning` | Warning color — caution messages |

---

## `dark.mog` — Dark theme

A modern dark theme with high contrast and cyan accents.

| Role | Color |
|---|---|
| Background | `color.black` |
| Foreground | `color.white` |
| Focus | `color.brightcyan` on `color.black` |
| Buttons | `color.brightcyan` on `color.black` |
| Accent | `color.brightcyan` |
| Danger | `color.brightred` |
| Success | `color.brightgreen` |
| Warning | `color.brightyellow` |

```mogwai
"themes/dark.mog" include
```

---

## `classic.mog` — Classic theme

A retro theme inspired by AMSTRAD CPC home computers — blue background, yellow text.

| Role | Color |
|---|---|
| Background | `color.blue` |
| Foreground | `color.yellow` |
| Focus | `color.black` on `color.cyan` |
| Buttons | `color.black` on `color.cyan` |
| Accent | `color.white` |
| Danger | `color.brightred` |
| Success | `color.brightgreen` |
| Warning | `color.brightyellow` |

```mogwai
"themes/classic.mog" include
```

---

## Creating your own theme

Copy `dark.mog` or `classic.mog`, adjust the color values, save as `themes/mytheme.mog` in your Programs folder, and include it with:

```mogwai
(! path.programs "themes" "mytheme.mog") path.make mogwai.include
```
