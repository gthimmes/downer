# Downer — Roadmap

A platform-independent (Windows / macOS / Linux) markdown editor built with .NET / Avalonia UI.

Each phase lands as its own commit (pushed to `origin/main`) with tests where logic is testable.

## Phase 1 — Scaffold ⬜
- [ ] Avalonia app skeleton (Program, App, empty MainWindow)
- [ ] Solution with `src/Downer` + `tests/Downer.Tests` (xUnit)
- [ ] Builds and launches on .NET 10

## Phase 2 — Editor core ⬜
- [ ] AvaloniaEdit editing surface with markdown syntax highlighting (TextMate)
- [ ] File ops: New / Open / Save / Save As with dirty tracking
- [ ] Unsaved-changes guard on close/new/open
- [ ] Open file from command line

## Phase 3 — Live preview ⬜
- [ ] Markdown.Avalonia preview pane, debounced updates
- [ ] View modes: Editor only / Split / Preview only
- [ ] Proportional scroll sync (editor → preview)

## Phase 4 — Formatting engine (pure + fully tested) ⬜
- [ ] `MarkdownFormatter`: pure (text, selection) → (text, selection) transforms
- [ ] Inline toggles: bold, italic, strikethrough, inline code
- [ ] Line ops: bullet / numbered / task list, blockquote, headings H1–H6
- [ ] Insertions: link, image, table, horizontal rule, fenced code block
- [ ] Auto-continuation of lists/quotes on Enter (incl. exit-on-empty)
- [ ] Toolbar + Format/Insert menus wired to the engine

## Phase 5 — Find/replace & editor comfort ⬜
- [ ] Find and Replace (AvaloniaEdit SearchPanel)
- [ ] Status bar: cursor position, word/char count (tested counter)
- [ ] Word wrap and line-number toggles, font zoom in/out/reset

## Phase 6 — HTML export ⬜
- [ ] `HtmlExporter`: Markdig advanced pipeline + embedded GitHub-ish CSS (tested)
- [ ] Export As HTML… menu action

## Phase 7 — Settings, recents, themes ⬜
- [ ] JSON settings persistence in per-user app-data dir (tested)
- [ ] Recent files menu (tested MRU logic)
- [ ] Light / dark theme toggle (app theme + editor TextMate theme)

## Phase 8 — Polish ⬜
- [ ] Drag & drop file open
- [ ] Platform-aware shortcuts (Cmd on macOS, Ctrl elsewhere)
- [ ] Welcome document, About dialog
- [ ] README with build/run instructions for all platforms

## Backlog / ideas (post-1.0)
- Autosave, session restore, multiple tabs
- Spell check
- Export to PDF
- Synchronized bidirectional scroll
