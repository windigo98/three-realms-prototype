
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace ThreeRealms
{
    /// <summary>
    /// Reads Touch, KeyboardMouse, and Gamepad into the same actions.
    /// On-screen left stick and right look drag are applied by GameHud.
    /// </summary>
    public class FrameInput : MonoBehaviour
    {
        public Vector2 Move;
        public Vector2 Look;
        public bool DodgePressed;
        public bool AttackPressed;
        public bool CycleTargetPressed;
        public bool ToggleDebugPressed;
        public bool PointerLocked;

        [HideInInspector] public Vector2 UiMove;
        [HideInInspector] public Vector2 UiLook;
        [HideInInspector] public bool UiDodge;
        [HideInInspector] public bool UiAttack;
        [HideInInspector] public bool UiCycleTarget;

        public float LookSensitivity = 1.4f;
        public float GamepadLookSensitivity = 90f;
        public float TouchLookSensitivity = 0.18f;

        bool _dodgeHeld;
        bool _attackHeld;
        bool _cycleHeld;
        bool _debugHeld;
        bool _rmbLook;

        public void ConsumeButtons()
        {
            DodgePressed = false;
            AttackPressed = false;
            CycleTargetPressed = false;
            ToggleDebugPressed = false;
        }

        void Update()
        {
            Vector2 move = UiMove;
            Vector2 look = UiLook;
            bool dodge = UiDodge;
            bool attack = UiAttack;
            bool cycle = UiCycleTarget;
            bool debug = false;

            var kb = Keyboard.current;
            if (kb != null)
            {
                Vector2 k = Vector2.zero;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) k.y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) k.y -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) k.x += 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) k.x -= 1f;
                if (k.sqrMagnitude > 1f) k.Normalize();
                if (k.sqrMagnitude > 0.01f) move = k;

                dodge |= Edge(kb.spaceKey, kb.leftShiftKey);
                attack |= Edge(kb.jKey) || WasPressed(Mouse.current != null ? Mouse.current.leftButton : null);
                cycle |= Edge(kb.tabKey);
                debug |= Edge(kb.backquoteKey, kb.f1Key);
            }

            var mouse = Mouse.current;
            if (mouse != null)
            {
                _rmbLook = mouse.rightButton.isPressed;
                if (PointerLocked || _rmbLook)
                {
                    Vector2 delta = mouse.delta.ReadValue();
                    look += delta * LookSensitivity;
                }
            }

            var pad = Gamepad.current;
            if (pad != null)
            {
                var ls = pad.leftStick.ReadValue();
                if (ls.sqrMagnitude > 0.04f) move = Vector2.ClampMagnitude(ls, 1f);
                var rs = pad.rightStick.ReadValue();
                if (rs.sqrMagnitude > 0.04f)
                    look += rs * GamepadLookSensitivity * Time.unscaledDeltaTime * 60f;
                dodge |= Edge(pad.buttonEast, pad.leftShoulder, pad.rightShoulder);
                attack |= Edge(pad.buttonWest, pad.buttonSouth, pad.rightTrigger);
                cycle |= Edge(pad.leftTrigger, pad.dpad.up);
                debug |= Edge(pad.selectButton, pad.startButton);
            }

            Move = Vector2.ClampMagnitude(move, 1f);
            Look = look;
            DodgePressed = dodge;
            AttackPressed = attack;
            CycleTargetPressed = cycle;
            ToggleDebugPressed = debug;

            UiDodge = false;
            UiAttack = false;
            UiCycleTarget = false;
            UiLook = Vector2.zero;
        }

        bool Edge(params ButtonControl[] buttons)
        {
            foreach (var b in buttons)
            {
                if (b != null && b.wasPressedThisFrame) return true;
            }
            return false;
        }

        static bool WasPressed(ButtonControl b) => b != null && b.wasPressedThisFrame;
    }
}
