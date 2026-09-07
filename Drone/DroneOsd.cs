using UnityEngine;

namespace HTFDrone.Drone
{
    /// <summary>
    /// A Betaflight/INAV-style OSD, drawn with IMGUI so it needs no Canvas/EventSystem/font-asset
    /// setup. Laid out the way a real analog OSD is: elements pinned to the screen edges in a
    /// character grid, glyph-prefixed readouts, a rolling artificial horizon read against a fixed
    /// centre crosshair, a centred warning line, and coordinates along the bottom.
    ///
    /// Everything is white, as the OSD character set is - the analog filter (scanlines, vignette,
    /// chroma fringing, static) supplies the colour cast and the "this is a video feed" feel.
    /// </summary>
    internal static class DroneOsd
    {
        // Every size below is in "design pixels" against this reference height, then scaled to the
        // real screen. Without this the OSD is tiny on a 1440p display and huge on a small window.
        private const float ReferenceHeight = 1080f;

        private static readonly Color OsdColor = Color.white;

        private static GUIStyle _labelStyle;
        private static GUIStyle _rightStyle;
        private static GUIStyle _centeredStyle;
        private static GUIStyle _bigCenteredStyle;
        private static Texture2D _barTexture;
        private static Texture2D _noiseTexture;
        private static float _batteryVoltage = 16.8f; // fully charged 4S pack
        private static float _mahDrawn;
        private static float _lastFlightTimeSeeded = -1f;
        private static float _lastStyleScale = -1f;

        public static float TimeAlive;
        public static TransmitterInput.Sticks LastSticks;

        /// <summary>Where the drone was launched from - the "home" the OSD arrow and distance point at.</summary>
        public static Vector3 HomePosition;
        public static bool HasHome;

        private static float Scale => Screen.height / ReferenceHeight;

        private static float S(float designPixels)
        {
            return designPixels * Scale;
        }

        public static void Draw(Rigidbody droneRig, Transform droneTransform)
        {
            EnsureStyles();

            float speed = droneRig ? droneRig.linearVelocity.magnitude : 0f;
            float altitude = droneTransform.position.y - WaterManager.WaterHeight;
            Vector3 euler = droneTransform.eulerAngles;
            float pitchDeg = Mathf.DeltaAngle(0f, euler.x) * -1f; // nose up = positive pitch, OSD convention
            float rollDeg = Mathf.DeltaAngle(0f, euler.z);

            UpdateFakeBattery(speed);

            DrawArtificialHorizon(pitchDeg, rollDeg);
            DrawCrosshair();
            DrawHomeArrowAndDistance(droneTransform);
            DrawTopLeftLinkStats();
            DrawTopRightIdentity();
            DrawLeftColumn(altitude, speed);
            DrawRightColumn();
            DrawBottomRow(droneTransform);
            DrawWarnings();
            DrawAnalogArtifacts();
        }

        private static void UpdateFakeBattery(float speed)
        {
            if (TimeAlive < _lastFlightTimeSeeded)
            {
                // New flight - reset the pack.
                _restVoltage = FullVoltage;
                _batteryVoltage = FullVoltage;
                _mahDrawn = 0f;
            }
            _lastFlightTimeSeeded = TimeAlive;

            float throttle01 = Mathf.Clamp01((LastSticks.Throttle + 1f) * 0.5f);

            // A real pack has two separate effects, and conflating them is what made the warning
            // fire on a timer regardless of how it was flown:
            //
            //  - Resting voltage falls slowly as capacity is actually consumed. In infinite mode
            //    there's no flight limit, so nothing is consumed and it stays put.
            //  - Sag: voltage drops sharply under load and springs back when you ease off. This
            //    is what makes punching the throttle flash LOW VOLTAGE and then clear.
            if (!DroneState.InfiniteFlight)
            {
                _restVoltage = Mathf.Max(MinRestVoltage, _restVoltage - (0.02f + throttle01 * 0.05f) * Time.deltaTime);
            }

            float sag = throttle01 * throttle01 * SagAtFullThrottle;
            float target = _restVoltage - sag;

            // Sag onsets fast and recovers slower, like a pack under and off load.
            float responseSpeed = (target < _batteryVoltage) ? 14f : 3f;
            _batteryVoltage = Mathf.Lerp(_batteryVoltage, target, responseSpeed * Time.deltaTime);

            // Consumed capacity climbs with throttle, the way a current sensor would report it.
            _mahDrawn += (18f + throttle01 * 90f + speed * 3f) * Time.deltaTime;
        }

        private const float FullVoltage = 16.8f;    // 4S fully charged
        private const float MinRestVoltage = 14.0f; // ~3.5V/cell, "land now"
        private const float SagAtFullThrottle = 2.4f;
        private const float LowVoltageThreshold = 14.8f;

        private static float _restVoltage = FullVoltage;

        private static bool IsLowVoltage => _batteryVoltage <= LowVoltageThreshold;

        /// <summary>Blink helper - real OSDs flash warnings rather than recolouring them.</summary>
        private static bool Blink => Mathf.PingPong(Time.time * 4f, 1f) > 0.5f;

        // --- Top row -------------------------------------------------------------------------

        private static void DrawTopLeftLinkStats()
        {
            GUI.color = OsdColor;
            float x = S(30f);

            // Link quality and RSSI, as an analog setup reports them.
            GUI.Label(new Rect(x, S(24f), S(500f), S(44f)), $"{Glyphs.Signal} 2:100", _labelStyle);
            GUI.Label(new Rect(x, S(70f), S(500f), S(44f)), $"{Glyphs.Signal} -130", _labelStyle);
            GUI.Label(new Rect(x, S(116f), S(500f), S(44f)), $"{Glyphs.Sat} 14", _labelStyle);
            GUI.color = Color.white;
        }

        private static void DrawTopRightIdentity()
        {
            GUI.color = OsdColor;
            float right = Screen.width - S(30f);
            float w = S(600f);

            GUI.Label(new Rect(right - w, S(24f), w, S(44f)), "PILOT NAME", _rightStyle);
            GUI.Label(new Rect(right - w, S(70f), w, S(44f)), "HTF DRONE", _rightStyle);
            GUI.Label(new Rect(right - w, S(116f), w, S(44f)), "R:2:200:P", _rightStyle);
            GUI.color = Color.white;
        }

        // --- Side columns --------------------------------------------------------------------

        private static void DrawLeftColumn(float altitude, float speed)
        {
            GUI.color = OsdColor;
            float x = S(30f);
            float y = Screen.height * 0.5f - S(30f);

            GUI.Label(new Rect(x, y, S(500f), S(44f)), $"ALT {altitude:0.0}m", _labelStyle);
            GUI.Label(new Rect(x, y + S(50f), S(500f), S(44f)), $"{Glyphs.Speed} {speed * 3.6f:0} km/h", _labelStyle);
            GUI.color = Color.white;
        }

        private static void DrawRightColumn()
        {
            GUI.color = OsdColor;
            float right = Screen.width - S(30f);
            float w = S(500f);
            float y = Screen.height * 0.5f - S(30f);

            // Throttle percentage, straight off the stick.
            float throttlePercent = Mathf.Clamp01((LastSticks.Throttle + 1f) * 0.5f) * 100f;
            GUI.Label(new Rect(right - w, y, w, S(44f)), $"{throttlePercent:0}%", _rightStyle);
            GUI.Label(new Rect(right - w, y + S(50f), w, S(44f)), $"{_mahDrawn:0} mAh", _rightStyle);
            GUI.color = Color.white;
        }

        // --- Bottom row ----------------------------------------------------------------------

        private static void DrawBottomRow(Transform droneTransform)
        {
            GUI.color = OsdColor;
            float x = S(30f);
            float right = Screen.width - S(30f);
            float w = S(600f);

            // Pack voltage bottom left, with a per-cell figure beside it, as in the reference.
            float cellVoltage = _batteryVoltage / 4f;
            if (!IsLowVoltage || Blink)
            {
                GUI.Label(new Rect(x, Screen.height - S(150f), S(500f), S(44f)),
                    $"{Glyphs.Battery} {_batteryVoltage:0.0}v", _labelStyle);
                GUI.Label(new Rect(Screen.width * 0.5f - S(150f), Screen.height - S(150f), S(400f), S(44f)),
                    $"{Glyphs.Battery} {cellVoltage:0.00}v", _labelStyle);
            }

            // Flight mode bottom right, and the flight timer beneath it. Always acro - the drone
            // is hand-flown, with no self-levelling or autopilot mode to switch to.
            GUI.Label(new Rect(right - w, Screen.height - S(150f), w, S(44f)), "ACRO", _rightStyle);
            GUI.Label(new Rect(right - w, Screen.height - S(104f), w, S(44f)),
                $"{Glyphs.Timer} {FlightTimerText()}", _rightStyle);

            // Coordinates along the very bottom, as GPS-equipped setups show them. Derived from
            // world position so they actually track the drone rather than being decoration.
            Vector3 pos = droneTransform.position;
            GUI.Label(new Rect(x, Screen.height - S(58f), S(700f), S(44f)),
                $"LAT {pos.z:+00.0000000;-00.0000000}", _labelStyle);
            GUI.Label(new Rect(right - w, Screen.height - S(58f), w, S(44f)),
                $"LON {pos.x:+000.0000000;-000.0000000}", _rightStyle);

            GUI.color = Color.white;
        }

        private static string FlightTimerText()
        {
            if (DroneState.InfiniteFlight)
            {
                return FormatTime(TimeAlive);
            }
            float remaining = Mathf.Max(0f, DroneState.MaxFlightTime - TimeAlive);
            return "-" + FormatTime(remaining);
        }

        // --- Centre ------------------------------------------------------------------------

        /// <summary>
        /// The centred warning line. Real OSDs reserve this spot for one flashing message at a
        /// time, in priority order, rather than stacking them.
        /// </summary>
        private static void DrawWarnings()
        {
            string warning = null;
            if (IsLowVoltage)
            {
                warning = "LOW VOLTAGE";
            }
            else if (!DroneState.InfiniteFlight && DroneState.MaxFlightTime - TimeAlive <= 5f)
            {
                warning = "TIMER CRITICAL";
            }

            if (warning == null || !Blink)
            {
                return;
            }

            GUI.color = OsdColor;
            GUI.Label(new Rect(Screen.width * 0.5f - S(400f), Screen.height * 0.5f + S(90f), S(800f), S(52f)),
                warning, _bigCenteredStyle);
            GUI.color = Color.white;
        }

        /// <summary>
        /// Home marker: distance back to the launch point, with an arrow pointing the way home
        /// relative to where the drone is facing - the same instrument a real OSD gives you for
        /// finding your way back when you've lost your bearings.
        /// </summary>
        private static void DrawHomeArrowAndDistance(Transform droneTransform)
        {
            if (!HasHome)
            {
                return;
            }

            Vector3 toHome = HomePosition - droneTransform.position;
            float distance = toHome.magnitude;

            GUI.color = OsdColor;
            GUI.Label(new Rect(Screen.width * 0.5f - S(200f), Screen.height * 0.5f - S(210f), S(400f), S(44f)),
                $"{Glyphs.Home} {distance:0}m", _centeredStyle);

            // Arrow rotates to point home, measured against the drone's own heading so it stays
            // meaningful however the craft is turned.
            Vector3 flatToHome = new Vector3(toHome.x, 0f, toHome.z);
            if (flatToHome.sqrMagnitude > 0.01f)
            {
                float bearing = Vector3.SignedAngle(
                    new Vector3(droneTransform.forward.x, 0f, droneTransform.forward.z),
                    flatToHome,
                    Vector3.up);

                Vector2 pivot = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f - S(150f));
                Matrix4x4 prev = GUI.matrix;
                GUIUtility.RotateAroundPivot(bearing, pivot);
                GUI.Label(new Rect(pivot.x - S(100f), pivot.y - S(26f), S(200f), S(52f)),
                    Glyphs.ArrowUp, _bigCenteredStyle);
                GUI.matrix = prev;
            }

            GUI.color = Color.white;
        }

        /// <summary>
        /// Betaflight's artificial horizon: a single horizon line that rolls with bank and slides
        /// with pitch, drawn as a row of short dashes either side of centre. No numbered pitch
        /// ladder - Betaflight doesn't have one; the horizon line itself plus the fixed crosshair
        /// is the whole instrument, which is why it stays readable on a noisy analog feed.
        /// </summary>
        private static void DrawArtificialHorizon(float pitchDeg, float rollDeg)
        {
            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;
            float pixelsPerDegree = S(10f);

            // Clamp how far the horizon slides so it stays on screen when pointing straight up or
            // down, the way the real OSD pins it at the edge of its window.
            float pitchOffset = Mathf.Clamp(pitchDeg * pixelsPerDegree, -S(320f), S(320f));

            Matrix4x4 prevMatrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(rollDeg, new Vector2(cx, cy));

            GUI.color = OsdColor;

            // Dashes either side of a centre gap, matching Betaflight's character-cell horizon.
            float dashWidth = S(46f);
            float dashHeight = S(5f);
            float gap = S(70f);
            float spacing = S(56f);

            for (int i = 0; i < 4; i++)
            {
                float offset = gap + i * spacing;
                float y = cy + pitchOffset - dashHeight * 0.5f;
                GUI.DrawTexture(new Rect(cx - offset - dashWidth, y, dashWidth, dashHeight), _barTexture);
                GUI.DrawTexture(new Rect(cx + offset, y, dashWidth, dashHeight), _barTexture);
            }

            GUI.matrix = prevMatrix;
            GUI.color = Color.white;
        }

        /// <summary>
        /// The fixed centre reticle: a short bar either side of a gap, screen-locked. In
        /// Betaflight this is what the rolling horizon is read against.
        /// </summary>
        private static void DrawCrosshair()
        {
            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;
            float barWidth = S(40f);
            float thickness = S(5f);
            float gap = S(22f);

            GUI.color = OsdColor;
            GUI.DrawTexture(new Rect(cx - gap - barWidth, cy - thickness * 0.5f, barWidth, thickness), _barTexture);
            GUI.DrawTexture(new Rect(cx + gap, cy - thickness * 0.5f, barWidth, thickness), _barTexture);
            // Small centre pip, so level is unambiguous.
            GUI.DrawTexture(new Rect(cx - thickness * 0.5f, cy - thickness * 0.5f, thickness, thickness), _barTexture);
            GUI.color = Color.white;
        }

        private static string FormatTime(float seconds)
        {
            int m = Mathf.FloorToInt(seconds / 60f);
            int s = Mathf.FloorToInt(seconds % 60f);
            return $"{m:00}:{s:00}";
        }

        /// <summary>Cheap analog-VTX look: scanlines, a vignette, faint chroma-fringe edges on the
        /// screen border, and the occasional flicker of static - all IMGUI overlay draws, no
        /// custom shader/render-texture pipeline needed.</summary>
        private static void DrawAnalogArtifacts()
        {
            float w = Screen.width;
            float h = Screen.height;

            // Scanlines.
            GUI.color = new Color(0f, 0f, 0f, 0.12f);
            float lineStep = Mathf.Max(3f, S(4f));
            for (float y = 0; y < h; y += lineStep)
            {
                GUI.DrawTexture(new Rect(0, y, w, 1f), _barTexture);
            }

            // Vignette (four soft dark bars is a cheap approximation without a radial shader).
            GUI.color = new Color(0f, 0f, 0f, 0.35f);
            float vig = S(60f);
            GUI.DrawTexture(new Rect(0, 0, w, vig * 0.4f), _barTexture);
            GUI.DrawTexture(new Rect(0, h - vig * 0.4f, w, vig * 0.4f), _barTexture);
            GUI.DrawTexture(new Rect(0, 0, vig, h), _barTexture);
            GUI.DrawTexture(new Rect(w - vig, 0, vig, h), _barTexture);

            // Faint red/blue chroma-fringe hairlines along the very edge, like an analog
            // receiver's colour separation.
            GUI.color = new Color(1f, 0f, 0f, 0.15f);
            GUI.DrawTexture(new Rect(0, 0, S(2f), h), _barTexture);
            GUI.color = new Color(0f, 0.4f, 1f, 0.15f);
            GUI.DrawTexture(new Rect(w - S(2f), 0, S(2f), h), _barTexture);

            // Occasional static/signal-noise flicker, like a weak analog link.
            EnsureNoiseTexture();
            if (Random.value < 0.06f)
            {
                GUI.color = new Color(1f, 1f, 1f, Random.Range(0.03f, 0.12f));
                GUI.DrawTexture(new Rect(0, 0, w, h), _noiseTexture);
            }

            GUI.color = Color.white;
        }

        private static void EnsureNoiseTexture()
        {
            if (_noiseTexture != null)
            {
                return;
            }
            const int size = 64;
            _noiseTexture = new Texture2D(size, size, TextureFormat.Alpha8, mipChain: false);
            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color(1f, 1f, 1f, Random.value);
            }
            _noiseTexture.SetPixels(pixels);
            _noiseTexture.filterMode = FilterMode.Point;
            _noiseTexture.wrapMode = TextureWrapMode.Repeat;
            _noiseTexture.Apply();
        }

        private static void EnsureStyles()
        {
            if (_barTexture == null)
            {
                _barTexture = Texture2D.whiteTexture;
            }

            // Font sizes are baked into a GUIStyle, so they have to be rebuilt if the window is
            // resized rather than just scaled at draw time like the rects are.
            if (_labelStyle != null && Mathf.Approximately(_lastStyleScale, Scale))
            {
                return;
            }
            _lastStyleScale = Scale;

            int bodySize = Mathf.Max(11, Mathf.RoundToInt(S(32f)));
            int bigSize = Mathf.Max(13, Mathf.RoundToInt(S(40f)));

            _labelStyle = new GUIStyle
            {
                fontSize = bodySize,
                fontStyle = FontStyle.Bold,
                normal = { textColor = OsdColor }
            };

            _rightStyle = new GUIStyle(_labelStyle)
            {
                alignment = TextAnchor.UpperRight
            };

            _centeredStyle = new GUIStyle(_labelStyle)
            {
                alignment = TextAnchor.MiddleCenter
            };

            _bigCenteredStyle = new GUIStyle
            {
                fontSize = bigSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = OsdColor }
            };
        }

        /// <summary>
        /// Stand-ins for the OSD character set's icons. The real font isn't available here, so
        /// these are the closest glyphs Unity's built-in font can render - anything more exotic
        /// shows up as a missing-glyph box, which looks worse than a plain letter.
        /// </summary>
        private static class Glyphs
        {
            public const string Battery = "▮";  // ▮ battery body
            public const string Signal = "█";   // █ signal bar block
            public const string Sat = "⊙";      // ⊙ satellite
            public const string Home = "⌂";     // ⌂ home
            public const string Timer = "◷";    // ◷ clock
            public const string Speed = "◴";    // ◴ speedo
            public const string ArrowUp = "↑";  // ↑ direction home
        }
    }
}
