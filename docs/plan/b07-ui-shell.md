# B07 — UI shell

**Goal**: responsive UI Toolkit shell implementing `IShell` — desktop panels ↔ mobile sheets, toolbar with overflow, toasts, safe areas.
**Estimate**: 1.5 h · **Needs**: A2
**Owns**: `Scripts/Presentation/Shell/`, `UI/Shell/` (UXML/USS), `Tests/EditMode/Shell/`, `Tests/PlayMode/Shell/`

## Key decisions
- UI Toolkit has no media queries: `ResponsiveLayout` toggles `.compact` on the root when width < 768 px; USS handles the rest.
- Panels from other phases are plain `VisualElement`s; the shell only provides slots/sheets — it knows nothing about features.
- Full-screen/layout containers use `PickingMode.Ignore`; only real controls and panels are pickable. `PointerInput` (B05) treats any pickable element under the pointer as UI and won't orbit/select through it.

## Tasks
- [ ] `MainLayout.uxml` + `Shell.uss` (imports `Theme.uss`): top toolbar, left/right/bottom slots, sheet host, toast area.
- [ ] `ResponsiveLayout` (pure decision + thin MonoBehaviour): width → compact; `Screen.safeArea` → root padding; re-evaluates on resize/orientation.
- [ ] `Sheet` element: bottom sheet with drag handle, half/full heights, close button, one sheet at a time.
- [ ] Toolbar: `AddToolbarItem` (id, label, glyph, tooltip, toggle?, `priority`), `SetToolbarItemState`; compact mode keeps the top-priority items visible and moves the rest to an overflow menu. Text/Unicode glyphs (ask before downloading an icon set).
- [ ] Toasts: queue, auto-hide 4 s, error style.
- [ ] `ShellView : MonoBehaviour, IShell` on a `UIDocument`.
- [ ] Desktop: slots collapsible; compact: slots hidden, panels open as sheets.

## Tests
**EditMode**: breakpoint decision (767 → compact, 768 → not); overflow split keeps highest-priority items visible for a given width; safe-area → padding maths.
**PlayMode**: adding a toolbar item creates a ≥ 44 px button; compact switch moves low-priority items to overflow; `ShowSheet` shows one sheet and replaces the previous.

## PC + mobile checks
- [ ] Device Simulator: 360 × 780 portrait, landscape phone, notch safe area, 1920 × 1080.

## Hand-off to C
- C1: `ShellView` in `Main.unity`; pass as `IShell`.
- C2: mount feature panels (info right, layers left, takeoff bottom, legend left) and register toolbar items.
