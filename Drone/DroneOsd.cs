using UnityEngine;

namespace HTFDrone.Drone
{
    /// <summary>
    /// Minimal FPV goggles-style on-screen display, drawn with IMGUI so it needs no
    /// Canvas/EventSystem/font-asset setup. Shows speed, altitude above water, a center
    /// crosshair, flight timer, and an armed/target indicator.
    /// </summary>
    internal static class DroneOsd
    {
        private static GUIStyle _labelStyle;
        private static Texture2D _barTexture;

        public static float TimeAlive;
        public static Transform CurrentTarget;
        public static bool ManualControl;
        public static TransmitterInput.Sticks LastSticks;

        public static void Draw(Rigidbody droneRig, Transform droneTransform)
        {
            EnsureStyles();

            float speed = droneRig ? droneRig.linearVelocity.magnitude : 0f;
            float altitude = droneTransform.position.y - WaterManager.WaterHeight;

            DrawCrosshair();
            DrawCorners();

            string mode = ManualControl ? "MANUAL" : "AUTO";
            string armed = CurrentTarget ? "LOCK" : "----";
            Color prevColor = GUI.color;
            GUI.color = Color.green;

            GUI.Label(new Rect(20, 20, 300, 30), $"FPV DRONE  [{mode}]", _labelStyle);
            GUI.Label(new Rect(20, 45, 300, 30), $"SPD {speed:0.0} m/s", _labelStyle);
            GUI.Label(new Rect(20, 70, 300, 30), $"ALT {altitude:0.0} m", _labelStyle);
            GUI.Label(new Rect(20, 95, 300, 30), $"T+ {TimeAlive:0.0}s", _labelStyle);
            GUI.Label(new Rect(20, 120, 400, 30), $"R{LastSticks.Roll:+0.00;-0.00;0.00} P{LastSticks.Pitch:+0.00;-0.00;0.00} T{LastSticks.Throttle:+0.00;-0.00;0.00} Y{LastSticks.Yaw:+0.00;-0.00;0.00}", _labelStyle);

            GUI.color = CurrentTarget ? Color.red : Color.green;
            GUI.Label(new Rect(Screen.width - 160, 20, 140, 30), armed, _labelStyle);

            GUI.color = prevColor;
        }

        private static void DrawCrosshair()
        {
            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;
            Color prevColor = GUI.color;
            GUI.color = Color.green;

            GUI.DrawTexture(new Rect(cx - 20, cy - 1, 40, 2), _barTexture);
            GUI.DrawTexture(new Rect(cx - 1, cy - 20, 2, 40), _barTexture);
            GUI.DrawTexture(new Rect(cx - 1, cy - 1, 2, 2), _barTexture);

            GUI.color = prevColor;
        }

        private static void DrawCorners()
        {
            Color prevColor = GUI.color;
            GUI.color = new Color(0f, 1f, 0f, 0.6f);
            float w = Screen.width;
            float h = Screen.height;
            const float len = 26f;
            const float thick = 2f;

            // Four corner brackets, FPV-goggles style.
            GUI.DrawTexture(new Rect(10, 10, len, thick), _barTexture);
            GUI.DrawTexture(new Rect(10, 10, thick, len), _barTexture);

            GUI.DrawTexture(new Rect(w - 10 - len, 10, len, thick), _barTexture);
            GUI.DrawTexture(new Rect(w - 10 - thick, 10, thick, len), _barTexture);

            GUI.DrawTexture(new Rect(10, h - 10 - thick, len, thick), _barTexture);
            GUI.DrawTexture(new Rect(10, h - 10 - len, thick, len), _barTexture);

            GUI.DrawTexture(new Rect(w - 10 - len, h - 10 - thick, len, thick), _barTexture);
            GUI.DrawTexture(new Rect(w - 10 - thick, h - 10 - len, thick, len), _barTexture);

            GUI.color = prevColor;
        }

        private static void EnsureStyles()
        {
            if (_barTexture == null)
            {
                _barTexture = Texture2D.whiteTexture;
            }
            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle
                {
                    fontSize = 16,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.green }
                };
            }
        }
    }
}
