# B07 — UI shell

**Goal**: responsive UI Toolkit shell implementing `IShell` — desktop panels ↔ mobile sheets, toolbar with overflow, toasts, safe areas.
**Estimate**: 1.5 h · **Needs**: A2
**Owns**: `Scripts/Presentation/Shell/`, `UI/Shell/` (UXML/USS), `Tests/EditMode/Shell/`, `Tests/PlayMode/Shell/`

## Key decisions
- UI Toolkit has no media queries: `ResponsiveLayout` toggles `.compact` on the root when width < 768 px; USS handles the rest.
- Panels from other phases are plain `VisualElement`s; the shell only provides slots/sheets — it knows nothing about features.
- Full-screen/layout containers use `PickingMode.Ignore`; only real controls and panels are pickable. `PointerInput` (B05) treats any pickable element under the pointer as UI and won't orbit/select through it.

## Tasks
- [x] `MainLayout.uxml` + `Shell.uss` (imports `Theme.uss`): top toolbar, left/right/bottom slots, sheet host, toast area.
- [x] `ResponsiveLayout` (pure decision + thin MonoBehaviour): width → compact; `Screen.safeArea` → root padding; re-evaluates on resize/orientation.
- [x] `Sheet` element: bottom sheet with drag handle, half/full heights, close button, one sheet at a time.
- [x] Toolbar: `AddToolbarItem` (id, label, glyph, tooltip, toggle?, `priority`), `SetToolbarItemState`; compact mode keeps the top-priority items visible and moves the rest to an overflow menu. Text/Unicode glyphs (ask before downloading an icon set).
- [x] Toasts: queue, auto-hide 4 s, error style.
- [x] `ShellView : MonoBehaviour, IShell` on a `UIDocument`.
- [x] Desktop: slots collapsible; compact: slots hidden, panels open as sheets.

## Tests
**EditMode**: breakpoint decision (767 → compact, 768 → not); overflow split keeps highest-priority items visible for a given width; safe-area → padding maths.
**PlayMode**: adding a toolbar item creates a ≥ 44 px button; compact switch moves low-priority items to overflow; `ShowSheet` shows one sheet and replaces the previous.

## PC + mobile checks
- [ ] Device Simulator: 360 × 780 portrait, landscape phone, notch safe area, 1920 × 1080.

## Notes
- Code: `ResponsiveRules` (breakpoint, safe-area insets), `ToolbarOverflow.Split`, `SheetSnap` are pure and EditMode-tested; `ShellView` (on the `UIDocument`, `DefaultExecutionOrder(-50)`) wires `ToolbarView` + `SheetView`. No separate `ResponsiveLayout` MonoBehaviour: `ShellView` re-evaluates on the root's `GeometryChangedEvent`.
- `compact` goes on the document root (`ShellView.CompactClass`) so overlays (sheet, toasts) can react too.
- Safe area: shell root gets padding; overlays (absolute, which ignores padding) get the insets as offsets.
- Toolbar: fixed item widths (72 px desktop, 56 px compact, overflow 44 px) so the split is deterministic; the overflow menu sits on a scrim that closes it and swallows the tap. Toggle state is not flipped by the shell: features call `SetToolbarItemState`. Tooltips only for mouse, after 0.5 s.
- Toasts: up to 3 stacked, the oldest drops when a 4th arrives, each hides after 4 s; not pickable.
- Sheet: tap handle toggles half/full, swipe up → full, swipe down → half → closed; desktop shows it as a 480 px centred sheet. Replaced content is removed from the hierarchy; `SheetHidden` (added to `IShell`) fires when it closes.
- Empty slots hide their frame and collapse button (250 ms poll: UI Toolkit has no child-added event).
- Glyphs limited to characters the default font has (`×`, `•••`, `‹ ›`).

## Hand-off to C
- C1: Setup Scene adds a `Shell` object: `UIDocument` (`UI/PanelSettings.asset`, `UI/Shell/MainLayout.uxml`) + `ShellView`. Access `IShell` from Start or later (ShellView builds in OnEnable); re-enabling the object rebuilds the tree and drops mounted panels.
- C1: `ShellView` in `Main.unity`; pass as `IShell`.
- C2: mount feature panels (info right, layers left, takeoff bottom, legend left) and register toolbar items.
