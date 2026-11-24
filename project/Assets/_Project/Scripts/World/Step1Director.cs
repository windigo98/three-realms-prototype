
using UnityEngine;
using UnityEngine.InputSystem;

namespace ThreeRealms
{
    [DefaultExecutionOrder(100)]
    public class Step1Director : MonoBehaviour
    {
        public GuardianMotor Motor;
        public DodgeController Dodge;
        public ComboAttack Combo;
        public StickyTargeting Targeting;
        public FrameInput Input;
        public GameHud Hud;

        void Update()
        {
            if (Input == null) return;

            if (Input.ToggleDebugPressed && Hud != null)
                Hud.ShowDebug = !Hud.ShowDebug;

            if (Input.DodgePressed && Dodge != null)
                Dodge.TryDodge();

            if (Dodge != null) Dodge.Tick();

            if (Targeting != null) Targeting.Tick(Input.CycleTargetPressed);

            bool attacking = Combo != null && Combo.IsAttacking;
            if (Dodge == null || !Dodge.IsDodging)
            {
                if (Motor != null && !attacking)
                    Motor.Move(1f);
                else if (Motor != null && attacking)
                    Motor.Move(0.35f);
            }

            if (attacking && Targeting != null && Motor != null)
                Targeting.FaceTarget(Motor.transform, 14f);

            if (Combo != null) Combo.Tick();

            TryPointerLock();
        }

        void TryPointerLock()
        {
            var mouse = Mouse.current;
            if (mouse == null || Input == null) return;
            if (mouse.rightButton.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                Input.PointerLocked = true;
            }
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                Input.PointerLocked = false;
            }
        }
    }
}
