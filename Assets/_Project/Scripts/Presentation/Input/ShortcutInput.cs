using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StructureViewer.Presentation.Input
{
    // Polls the keyboard and raises ShortcutMap action ids; the composition root runs the same action as the button.
    public sealed class ShortcutInput : MonoBehaviour
    {
        public event Action<string> Triggered;

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || Triggered == null)
                return;

            bool ctrl = keyboard.ctrlKey.isPressed || keyboard.leftMetaKey.isPressed || keyboard.rightMetaKey.isPressed;
            bool shift = keyboard.shiftKey.isPressed;
            foreach (var key in ShortcutMap.Keys)
            {
                if (!keyboard[key].wasPressedThisFrame)
                    continue;
                string action = ShortcutMap.Resolve(key, ctrl, shift);
                if (action != null)
                    Triggered.Invoke(action);
            }
        }
    }
}
