# Downer

A full-featured, platform-independent **markdown editor** for Windows, macOS, and Linux, built with C# / .NET and [Avalonia UI](https://avaloniaui.net/).

## Features

- **Editor** — AvaloniaEdit surface with TextMate markdown syntax highlighting, line numbers, word wrap, font zoom, current-line highlight
- **Live preview** — rendered markdown side-by-side with debounced updates and proportional scroll sync; Editor / Split / Preview layouts (`Ctrl/Cmd+1..3`)
- **Formatting** — toggle bold / italic / strikethrough / inline code; H1–H6 headings; bullet, numbered, and task lists; blockquotes — all selection-aware toggles that also *unwrap* existing formatting
- **Smart lists** — `Enter` continues lists (incrementing numbers, fresh checkboxes, preserved indent); `Enter` on an empty item exits the list
- **Insertions** — links and images (URL-aware placeholders), tables, fenced code blocks, horizontal rules with proper blank-line padding
- **Find & replace** — `Ctrl/Cmd+F` / `Ctrl/Cmd+H`
- **Files** — open/save with dirty tracking and unsaved-changes guard, recent files menu, drag & drop, open from command line
- **Export** — standalone HTML with embedded GitHub-flavored CSS (light + dark)
- **Themes** — light / dark / follow-the-OS, applied to both the app and the editor's TextMate theme
- **Persistence** — theme, view mode, wrap, line numbers, font size, and recents survive restarts
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
  Core/        Pure, fully-tested logic: MarkdownFormatter, AutoListContinuation,
               HtmlExporter, DocumentStats, RecentFiles
  Services/    SettingsService (JSON persistence in the per-user app-data dir)
  Views/       MainWindow (partial classes per concern: FileOps, Editing,
               Preview, ViewOptions, Settings, Welcome)
  Dialogs/     Code-built modal dialogs
tests/Downer.Tests/   xUnit suite covering everything in Core and Services
```

The formatting engine is deliberately pure — every operation is a
`(text, selection) -> (text, selection)` function with no UI dependencies, which is
what makes the test suite possible. The UI applies results as minimal single-replace
edits so undo history stays clean.

## Stack

| Concern | Library |
| --- | --- |
| UI framework | Avalonia 11.3 (Fluent theme) |
| Text editor | Avalonia.AvaloniaEdit + AvaloniaEdit.TextMate |
| Preview rendering | Markdown.Avalonia |
| HTML export | Markdig (advanced extensions + YAML front matter) |

## Roadmap

See [ROADMAP.md](ROADMAP.md). Post-1.0 ideas: autosave, session restore, tabs, spell check, PDF export.
