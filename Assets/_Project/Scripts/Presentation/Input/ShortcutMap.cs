using UnityEngine.InputSystem;

namespace StructureViewer.Presentation.Input
{
    // Keyboard shortcut → action id. Every id is also a toolbar button (or the Esc behaviour of one), so the keyboard is never required.
    public static class ShortcutMap
    {
        public const string Focus = "focus";
        public const string FitAll = "fit-all";
        public const string Escape = "escape";
        public const string Isolate = "isolate";
        public const string Hide = "hide";
        public const string ShowAll = "show-all";
        public const string Undo = "undo";
        public const string Redo = "redo";
        public const string Measure = "measure";
        public const string Mode1 = "mode-1";
        public const string Mode2 = "mode-2";
        public const string Mode3 = "mode-3";
        public const string Mode4 = "mode-4";

        // Keys the input adapter polls; anything else never maps to an action.
        public static readonly Key[] Keys =
        {
            Key.F, Key.Home, Key.Escape, Key.I, Key.H, Key.Z, Key.Y, Key.M,
            Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4
        };

        // ctrl means Ctrl or Cmd. Returns null when the combination means nothing.
        public static string Resolve(Key key, bool ctrl, bool shift)
        {
            if (ctrl)
            {
                return key switch
                {
                    Key.Z => shift ? Redo : Undo,
                    Key.Y => Redo,
                    _ => null
                };
            }

            return key switch
            {
                Key.F => Focus,
                Key.Home => FitAll,
                Key.Escape => Escape,
                Key.I => Isolate,
                Key.H => shift ? ShowAll : Hide,
                Key.M => Measure,
                Key.Digit1 or Key.Numpad1 => Mode1,
                Key.Digit2 or Key.Numpad2 => Mode2,
                Key.Digit3 or Key.Numpad3 => Mode3,
                Key.Digit4 or Key.Numpad4 => Mode4,
                _ => null
            };
        }
    }
}
