# Downer

A full-featured, platform-independent **markdown editor** for Windows, macOS, and Linux, built with C# / .NET and [Avalonia UI](https://avaloniaui.net/).

## Features

- **WYSIWYG-first editing** — the default *Formatted* view styles markdown in place: real heading sizes, true bold/italic/strikethrough, mono code runs with tinted backgrounds, underlined links, and dimmed syntax markers. `Ctrl/Cmd+E` flips to the raw *Markdown Source* view (monospace, TextMate highlighting, line numbers) and back
- **Editor** — AvaloniaEdit surface with word wrap, font zoom, and a source mode with TextMate markdown syntax highlighting and line numbers
- **Live preview** — rendered markdown side-by-side with debounced updates and proportional scroll sync; Editor / Split / Preview layouts (`Ctrl/Cmd+1..3`)
- **Formatting** — toggle bold / italic / strikethrough / inline code; H1–H6 headings; bullet, numbered, and task lists; blockquotes — all selection-aware toggles that also *unwrap* existing formatting
- **Smart lists** — `Enter` continues lists (incrementing numbers, fresh checkboxes, preserved indent); `Enter` on an empty item exits the list
- **Insertions** — links and images (URL-aware placeholders), tables, fenced code blocks, horizontal rules with proper blank-line padding
- **Find & replace** — `Ctrl/Cmd+F` / `Ctrl/Cmd+H`
- **Tabs** — multiple documents in one window (`Ctrl/Cmd+N` new, `Ctrl/Cmd+W` close, `Ctrl+Tab` cycle), with per-tab undo history and caret memory
- **Spell check** — Hunspell with an embedded en_US dictionary; dotted red underlines on prose only (code, URLs, and identifiers are left alone)
- **Files** — open/save with dirty tracking and unsaved-changes guard, recent files menu, drag & drop, open from command line, optional autosave for titled documents
- **Session restore** — reopens all tabs from the previous session (toggleable)
- **Export** — standalone HTML with embedded GitHub-flavored CSS (light + dark), and dependency-free PDF export
- **Themes** — light / dark / follow-the-OS, applied to both the app and the editor's TextMate theme
- **Persistence** — theme, view mode, wrap, line numbers, font size, spell check, autosave, and recents survive restarts
- **Status bar** — caret position plus live word / character / line counts

Keyboard shortcuts use `Cmd` on macOS and `Ctrl` elsewhere, resolved from the platform's hotkey configuration at runtime.

## Building and running

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download) on any of Windows, macOS, or Linux.

```bash
dotnet run --project src/Downer            # run the app
dotnet run --project src/Downer notes.md   # open a file
dotnet test                                # run the test suite
dotnet publish src/Downer -c Release       # self-contained-ish release build
```

## Project layout

```
src/Downer/
  Core/        Pure, fully-tested logic: MarkdownFormatter, MarkdownSpanParser,
               AutoListContinuation, HtmlExporter, DocumentStats, RecentFiles
  Services/    SettingsService (JSON persistence in the per-user app-data dir)
  Views/       MainWindow (partial classes per concern: FileOps, Editing,
               Preview, ViewOptions, Settings, EditorMode, Welcome) and
               RichMarkdownTransformer (the in-place WYSIWYG renderer)
  Dialogs/     Code-built modal dialogs
tests/Downer.Tests/     xUnit suite covering everything in Core and Services
tests/Downer.UiTests/   Avalonia.Headless UI tests: boots the real window,
                        sends real keyboard input, captures rendered frames
tools/capture-window.ps1  Screenshots of the live app window for design review
```

The formatting engine is deliberately pure — every operation is a
`(text, selection) -> (text, selection)` function with no UI dependencies, which is
what makes the test suite possible. The UI applies results as minimal single-replace
edits so undo history stays clean. The WYSIWYG view is the same principle: a pure
span parser feeds an AvaloniaEdit line transformer, so the document is always plain
markdown — only the rendering changes.

## Stack

| Concern | Library |
| --- | --- |
| UI framework | Avalonia 11.3 (Fluent theme) |
| Text editor | Avalonia.AvaloniaEdit + AvaloniaEdit.TextMate |
| Preview rendering | Markdown.Avalonia |
| HTML export | Markdig (advanced extensions + YAML front matter) |

## Roadmap

See [ROADMAP.md](ROADMAP.md). Post-1.0 ideas: autosave, session restore, tabs, spell check, PDF export.
