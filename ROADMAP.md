# Downer — Roadmap

A platform-independent (Windows / macOS / Linux) markdown editor built with .NET / Avalonia UI.

Each phase lands as its own commit (pushed to `origin/main`) with tests where logic is testable.

## Phase 1 — Scaffold ✅
- [x] Avalonia app skeleton (Program, App, empty MainWindow)
- [x] Solution with `src/Downer` + `tests/Downer.Tests` (xUnit)
- [x] Builds and launches on .NET 10

## Phase 2 — Editor core ✅
- [x] AvaloniaEdit editing surface with markdown syntax highlighting (TextMate)
- [x] File ops: New / Open / Save / Save As with dirty tracking
- [x] Unsaved-changes guard on close/new/open
- [x] Open file from command line

## Phase 3 — Live preview ✅
- [x] Markdown.Avalonia preview pane, debounced updates
- [x] View modes: Editor only / Split / Preview only
- [x] Proportional scroll sync (editor → preview)

## Phase 4 — Formatting engine (pure + fully tested) ✅
- [x] `MarkdownFormatter`: pure (text, selection) → (text, selection) transforms
- [x] Inline toggles: bold, italic, strikethrough, inline code
- [x] Line ops: bullet / numbered / task list, blockquote, headings H1–H6
- [x] Insertions: link, image, table, horizontal rule, fenced code block
- [x] Auto-continuation of lists/quotes on Enter (incl. exit-on-empty)
- [x] Toolbar + Format/Insert menus wired to the engine

## Phase 5 — Find/replace & editor comfort ✅
- [x] Find and Replace (AvaloniaEdit SearchPanel)
- [x] Status bar: cursor position, word/char count (tested counter)
- [x] Word wrap and line-number toggles, font zoom in/out/reset

## Phase 6 — HTML export ✅
- [x] `HtmlExporter`: Markdig advanced pipeline + embedded GitHub-ish CSS (tested)
- [x] Export As HTML… menu action

## Phase 7 — Settings, recents, themes ✅
- [x] JSON settings persistence in per-user app-data dir (tested)
- [x] Recent files menu (tested MRU logic)
- [x] Light / dark theme toggle (app theme + editor TextMate theme)

## Phase 8 — Polish ✅
- [x] Drag & drop file open
- [x] Platform-aware shortcuts (Cmd on macOS, Ctrl elsewhere)
- [x] Welcome document, About dialog
- [x] README with build/run instructions for all platforms

## Backlog / ideas (post-1.0)
- Autosave, session restore, multiple tabs
- Spell check
- Export to PDF
- Synchronized bidirectional scroll
