
using UnityEngine;
using UnityEngine.InputSystem;

namespace ThreeRealms
{
    /// <summary>Health / Pulse / Charge bars, FPS, virtual stick, look drag, and a debug menu.</summary>
    [DefaultExecutionOrder(-200)]
    public class GameHud : MonoBehaviour
    {
        public FrameVitals Vitals;
        public FrameInput Input;
        public FrameHardpoints Hardpoints;
        public QualityTierController Quality;
        public FollowCamera CameraRig;
        public FallRecovery Recovery;

        public bool ShowDebug;
        float _fps;
        float _fpsAccum;
        int _fpsFrames;
        int _stickFinger = -1;
        int _lookFinger = -1;
        Vector2 _stickCenter;
        Vector2 _stickValue;
        Vector2 _lookLast;
        bool _snapTestOk;
        string _snapReport = "";
        string _pulseWarn;

        const float StickRadius = 90f;

        float Scale => Mathf.Clamp(Screen.height / 700f, 0.85f, 2.2f);

        void Update()
        {
            _fpsFrames++;
            _fpsAccum += Time.unscaledDeltaTime;
            if (_fpsAccum >= 0.25f)
            {
                _fps = _fpsFrames / _fpsAccum;
                _fpsFrames = 0;
                _fpsAccum = 0f;
            }
            // Poll touches here (before FrameInput) so HIT/DODGE/JUMP fire the same frame.
            // IMGUI GUI.Button is unreliable on iOS/WebGL with the Input System only.
            HandleTouch(Scale);
            DamagePopup.Cull();
        }

        void OnGUI()
        {
            float s = Mathf.Clamp(Screen.height / 700f, 0.85f, 2.2f);
            var barW = 220f * s;
            var barH = 16f * s;
            float x = 16f * s;
            float y = 14f * s;

            DrawBar(new Rect(x, y, barW, barH), "Health", Vitals != null ? Vitals.Health01 : 0f,
                new Color(0.75f, 0.25f, 0.25f), new Color(0.35f, 0.1f, 0.1f), s);
            y += barH + 8f * s;
            DrawBar(new Rect(x, y, barW, barH), "Pulse", Vitals != null ? Vitals.Pulse01 : 0f,
                new Color(0.25f, 0.65f, 0.95f), new Color(0.08f, 0.2f, 0.35f), s);
            y += barH + 8f * s;
            DrawBar(new Rect(x, y, barW, barH), "Charge", Vitals != null ? Vitals.Charge01 : 0f,
                new Color(0.95f, 0.75f, 0.2f), new Color(0.28f, 0.2f, 0.05f), s);

            var fpsStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(15 * s), alignment = TextAnchor.UpperRight };
            fpsStyle.normal.textColor = _fps >= 28f ? Color.white : new Color(1f, 0.5f, 0.4f);
            GUI.Label(new Rect(Screen.width - 150f * s, 10f * s, 138f * s, 28f * s),
                $"FPS {_fps:0.0}", fpsStyle);

            DrawStick(s);
            DrawButtons(s);
            DrawPopups(s);

            var dbg = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(14 * s) };
            if (GUI.Button(new Rect(Screen.width - 150f * s, 38f * s, 134f * s, 36f * s), ShowDebug ? "Debug ▴" : "Debug ▾", dbg))
                ShowDebug = !ShowDebug;

            if (ShowDebug) DrawDebug(s);

            if (!string.IsNullOrEmpty(_pulseWarn) && Time.unscaledTime < 0.01f) _pulseWarn = null;

            DrawFallFlash();
        }

        void DrawFallFlash()
        {
            if (Recovery == null || Recovery.Flash01 <= 0f) return;
            var prev = GUI.color;
            GUI.color = new Color(0.06f, 0.07f, 0.09f, Recovery.Flash01 * 0.82f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), WhiteTex());
            GUI.color = prev;
        }

        void ActionRects(float s, out Rect hit, out Rect dodge, out Rect jump)
        {
            // IMGUI space: origin top-left, same as GUI.Box.
            // Thumb cluster, bottom-right:
            //   [DODGE] [JUMP]
            //           [HIT ]
            float bw = 88f * s, bh = 88f * s;
            float gap = 12f * s;
            float ax = Screen.width - 16f * s - bw;
            float ay = Screen.height - 16f * s - bh;
            hit = new Rect(ax, ay, bw, bh);
            jump = new Rect(ax, ay - gap - bh, bw, bh);
            dodge = new Rect(ax - 16f * s - bw, ay - 10f * s, bw, bh);
        }

        static Vector2 ToGui(Vector2 screen) => new Vector2(screen.x, Screen.height - screen.y);

        bool InActionButtons(Vector2 screen, float s, out bool hitBtn, out bool dodgeBtn, out bool jumpBtn)
        {
            ActionRects(s, out var hit, out var dodge, out var jump);
            var gui = ToGui(screen);
            hitBtn = hit.Contains(gui);
            dodgeBtn = dodge.Contains(gui);
            jumpBtn = jump.Contains(gui);
            return hitBtn || dodgeBtn || jumpBtn;
        }

        bool InActionZone(Vector2 screen, float s)
        {
            // HIT + JUMP stacked, DODGE to the left. Taller than the old two-button cluster.
            return screen.x > Screen.width - 240f * s && screen.y < 280f * s;
        }

        void PressAction(bool hitBtn, bool dodgeBtn, bool jumpBtn)
        {
            if (Input == null) return;
            if (hitBtn) Input.UiAttack = true;
            if (jumpBtn) Input.UiJump = true;
            if (dodgeBtn)
            {
                Input.UiDodge = true;
                if (Vitals != null && Vitals.Pulse < 5f) _pulseWarn = "Need 5 Pulse";
            }
        }

        void HandleTouch(float s)
        {
            if (Input == null) return;
            bool usedPointer = false;

            if (Touchscreen.current != null)
            {
                foreach (var t in Touchscreen.current.touches)
                {
                    if (!t.press.isPressed && !t.press.wasPressedThisFrame && !t.press.wasReleasedThisFrame) continue;
                    var pos = t.position.ReadValue();
                    int id = t.touchId.ReadValue();
                    var phasePressed = t.press.isPressed;
                    var began = t.press.wasPressedThisFrame;
                    var ended = t.press.wasReleasedThisFrame;
                    usedPointer = true;
                    if (InActionZone(pos, s) || id == _stickFinger || id == _lookFinger)
                        Input.UiPointerConsumed = true;
                    RoutePointer(id, pos, began, phasePressed, ended, s);
                }
            }

            var mouse = Mouse.current;
            if (mouse != null)
            {
                var pos = mouse.position.ReadValue();
                bool down = mouse.leftButton.isPressed;
                bool began = mouse.leftButton.wasPressedThisFrame;
                bool ended = mouse.leftButton.wasReleasedThisFrame;
                if (down || began || ended)
                {
                    if (InActionButtons(pos, s, out var hitBtn, out var dodgeBtn, out var jumpBtn))
                    {
                        Input.UiPointerConsumed = true;
                        if (began) PressAction(hitBtn, dodgeBtn, jumpBtn);
                        usedPointer = true;
                    }
                    else if (down && pos.x < Screen.width * 0.4f && pos.y < Screen.height * 0.55f)
                    {
                        // Click-drag on the left half as a stick when no dedicated stick finger (editor / desktop).
                        if (Touchscreen.current == null || Touchscreen.current.touches.Count == 0)
                        {
                            Input.UiPointerConsumed = true;
                            RoutePointer(1001, pos, began, true, ended, s);
                            usedPointer = true;
                        }
                    }
                    else if (InActionZone(pos, s))
                    {
                        Input.UiPointerConsumed = true;
                    }
                }
                if (!down && _stickFinger == 1001)
                {
                    _stickFinger = -1;
                    _stickValue = Vector2.zero;
                    Input.UiMove = Vector2.zero;
                }
            }

            if (!usedPointer && _stickFinger < 0)
            {
                _stickValue = Vector2.zero;
            }
        }

        void RoutePointer(int id, Vector2 pos, bool began, bool held, bool ended, float s)
        {
            // Action buttons first so look-drag / stick never steal HIT/DODGE/JUMP,
            // including a second finger while the stick is held.
            if (InActionButtons(pos, s, out var hitBtn, out var dodgeBtn, out var jumpBtn))
            {
                if (began) PressAction(hitBtn, dodgeBtn, jumpBtn);
                return;
            }
            if (id != _stickFinger && id != _lookFinger && InActionZone(pos, s))
                return;

            if (began)
            {
                if (pos.x < Screen.width * 0.45f && pos.y < Screen.height * 0.6f && _stickFinger < 0)
                {
                    _stickFinger = id;
                    _stickCenter = pos;
                    _stickValue = Vector2.zero;
                    if (Input != null) Input.UiPointerConsumed = true;
                }
                else if (pos.x >= Screen.width * 0.45f && _lookFinger < 0)
                {
                    _lookFinger = id;
                    _lookLast = pos;
                    if (Input != null) Input.UiPointerConsumed = true;
                }
            }

            if (held && id == _stickFinger)
            {
                var delta = pos - _stickCenter;
                var r = StickRadius * s;
                if (delta.magnitude > r) delta = delta.normalized * r;
                _stickValue = delta / r;
                if (Input != null) Input.UiMove = _stickValue;
            }
            if (held && id == _lookFinger)
            {
                var delta = pos - _lookLast;
                _lookLast = pos;
                if (Input != null) Input.UiLook += delta * (Input.TouchLookSensitivity);
            }

            if (ended)
            {
                if (id == _stickFinger)
                {
                    _stickFinger = -1;
                    _stickValue = Vector2.zero;
                    if (Input != null) Input.UiMove = Vector2.zero;
                }
                if (id == _lookFinger) _lookFinger = -1;
            }
        }

        void DrawStick(float s)
        {
            if (_stickFinger < 0 && (Input == null || Input.Move.sqrMagnitude < 0.01f))
            {
                // Resting ghost stick, bottom-left.
                var rest = new Vector2(110f * s, 130f * s);
                DrawCircle(rest, StickRadius * s, new Color(1, 1, 1, 0.12f));
                DrawCircle(rest, 28f * s, new Color(1, 1, 1, 0.22f));
                return;
            }
            var center = _stickFinger >= 0 ? _stickCenter : new Vector2(110f * s, 130f * s);
            DrawCircle(center, StickRadius * s, new Color(1, 1, 1, 0.18f));
            var knob = center + _stickValue * (StickRadius * s);
            DrawCircle(knob, 28f * s, new Color(1, 1, 1, 0.45f));
        }

        void DrawButtons(float s)
        {
            var style = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(18 * s), fontStyle = FontStyle.Bold };
            ActionRects(s, out var hit, out var dodge, out var jump);
            // Visual only. Input is handled in HandleTouch so multi-touch + iOS work.
            GUI.Box(hit, "HIT", style);
            GUI.Box(dodge, "DODGE", style);
            GUI.Box(jump, "JUMP", style);
        }

        void DrawPopups(float s)
        {
            var cam = UnityEngine.Camera.main;
            if (cam == null) return;
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(22 * s),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            foreach (var p in DamagePopup.Active)
            {
                if (p == null) continue;
                var sp = cam.WorldToScreenPoint(p.CurrentWorld);
                if (sp.z < 0f) continue;
                var a = 1f - Mathf.Clamp01(p.Age / p.Life);
                style.normal.textColor = new Color(p.Color.r, p.Color.g, p.Color.b, a);
                var r = new Rect(sp.x - 40f * s, Screen.height - sp.y - 24f * s, 80f * s, 40f * s);
                GUI.Label(r, p.Text, style);
            }
        }

        void DrawDebug(float s)
        {
            var box = new Rect(16f * s, 110f * s, 320f * s, 260f * s);
            GUI.Box(box, "");
            GUILayout.BeginArea(new Rect(box.x + 10f * s, box.y + 8f * s, box.width - 20f * s, box.height - 16f * s));
            var ls = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(14 * s) };
            var bs = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(14 * s) };
            GUILayout.Label("Quality", ls);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Low", bs)) Quality?.SetTier(QualityTier.Low);
            if (GUILayout.Button("Medium", bs)) Quality?.SetTier(QualityTier.Medium);
            if (GUILayout.Button("High", bs)) Quality?.SetTier(QualityTier.High);
            GUILayout.EndHorizontal();
            var cur = Quality != null ? Quality.Current.ToString() : "?";
            GUILayout.Label("Current: " + cur + "  target " + (cur == "High" ? "60" : "30") + " fps", ls);

            GUILayout.Space(8);
            GUILayout.Label("Hardpoint snap test", ls);
            if (GUILayout.Button("Unsnap all, then re-snap", bs))
                RunSnapTest();
            GUILayout.Label(_snapReport, ls);
            if (_snapTestOk) GUILayout.Label("All 8 hardpoints SNAP OK", ls);
            GUILayout.EndArea();
        }

        void RunSnapTest()
        {
            if (Hardpoints == null) { _snapReport = "No Frame."; _snapTestOk = false; return; }
            var parts = new System.Collections.Generic.List<FramePart>();
            foreach (var id in HardpointNames.All)
            {
                var p = Hardpoints.GetMounted(id) ?? Hardpoints.Unsnap(id);
                if (p == null)
                {
                    // already unsnapped? try find in scene
                    foreach (var fp in Object.FindObjectsByType<FramePart>(FindObjectsSortMode.None))
                        if (fp.Slot == id) { p = fp; break; }
                }
                else
                {
                    p = Hardpoints.GetMounted(id);
                }
            }
            Hardpoints.UnsnapAll();
            foreach (var fp in Object.FindObjectsByType<FramePart>(FindObjectsSortMode.None))
                Hardpoints.Snap(fp);
            _snapTestOk = Hardpoints.AllFitted();
            _snapReport = Hardpoints.SnapReport();
        }

        static void DrawBar(Rect r, string label, float fill, Color on, Color off, float s)
        {
            var prev = GUI.color;
            GUI.color = off;
            GUI.DrawTexture(r, WhiteTex());
            GUI.color = on;
            var inner = new Rect(r.x, r.y, r.width * Mathf.Clamp01(fill), r.height);
            GUI.DrawTexture(inner, WhiteTex());
            GUI.color = prev;
            var st = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(11 * s), alignment = TextAnchor.MiddleLeft };
            st.normal.textColor = Color.white;
            GUI.Label(new Rect(r.x + 4f, r.y - 1f, r.width, r.height), label, st);
        }

        static Texture2D _white;
        static Texture2D WhiteTex()
        {
            if (_white == null)
            {
                _white = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                _white.SetPixel(0, 0, Color.white);
                _white.Apply();
                _white.hideFlags = HideFlags.HideAndDontSave;
            }
            return _white;
        }

        static void DrawCircle(Vector2 center, float radius, Color c)
        {
            var prev = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(new Rect(center.x - radius, Screen.height - center.y - radius, radius * 2f, radius * 2f), WhiteTex());
            GUI.color = prev;
        }
    }
}
