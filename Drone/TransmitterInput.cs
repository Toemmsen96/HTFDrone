using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Utilities;

namespace HTFDrone.Drone
{
    /// <summary>
    /// Reads stick input from a Taranis QX7 (or any transmitter) running in USB-joystick/HID
    /// mode. The game's shipped Input Manager has no joystick axes registered, so legacy
    /// Input.GetAxis can't see it - instead we go through the new Input System (already used by
    /// the game itself, see Explosive.PrimaryInput). Depending on OS/USB mode the transmitter can
    /// show up either as a standard Gamepad (most common - left/right stick + triggers) or as a
    /// generic HID Joystick (raw indexed axes); we try Gamepad first and fall back to Joystick.
    /// </summary>
    internal static class TransmitterInput
    {
        public struct Sticks
        {
            public float Roll;
            public float Pitch;
            public float Throttle;
            public float Yaw;
            public bool HasInput;
        }

        private static Gamepad _cachedGamepad;
        private static Joystick _cachedJoystick;
        private static readonly List<AxisControl> _axisControls = new List<AxisControl>();

        public static Sticks Read()
        {
            if (TryGetGamepad(out Gamepad gamepad))
            {
                return ReadFromGamepad(gamepad);
            }
            if (TryGetAxes(out IReadOnlyList<AxisControl> axes))
            {
                return ReadFromAxes(axes);
            }
            return default;
        }

        public static bool DetonatePressed()
        {
            if (Input.GetKeyDown(DroneState.DetonateKey))
            {
                return true;
            }
            if (TryGetGamepad(out Gamepad gamepad))
            {
                return gamepad.rightTrigger.wasPressedThisFrame || gamepad.leftTrigger.wasPressedThisFrame
                    || gamepad.buttonSouth.wasPressedThisFrame || gamepad.startButton.wasPressedThisFrame;
            }
            if (TryGetJoystick(out Joystick joystick) && joystick.trigger != null)
            {
                return joystick.trigger.wasPressedThisFrame;
            }
            return false;
        }

        /// <summary>Describes whatever transmitter/joystick/gamepad was detected and its current live values, for figuring out DroneState's mapping. Call once from a debug command.</summary>
        public static IEnumerable<string> DescribeAxes()
        {
            if (TryGetGamepad(out Gamepad gamepad))
            {
                yield return $"Gamepad detected: {gamepad.displayName}";
                yield return $"leftStick  = {gamepad.leftStick.ReadValue()}";
                yield return $"rightStick = {gamepad.rightStick.ReadValue()}";
                yield return $"leftTrigger  = {gamepad.leftTrigger.ReadValue():0.00}";
                yield return $"rightTrigger = {gamepad.rightTrigger.ReadValue():0.00}";
                yield break;
            }

            if (TryGetAxes(out IReadOnlyList<AxisControl> axes))
            {
                yield return $"Joystick detected: {_cachedJoystick.displayName}";
                for (int i = 0; i < axes.Count; i++)
                {
                    yield return $"[{i}] {axes[i].displayName} ({axes[i].path}) = {axes[i].ReadValue():0.00}";
                }
                yield break;
            }

            yield return "No joystick/gamepad/transmitter detected.";
        }

        private static Sticks ReadFromGamepad(Gamepad gamepad)
        {
            Vector2 primary = DroneState.ThrottleYawOnLeftStick ? gamepad.leftStick.ReadValue() : gamepad.rightStick.ReadValue();
            Vector2 secondary = DroneState.ThrottleYawOnLeftStick ? gamepad.rightStick.ReadValue() : gamepad.leftStick.ReadValue();

            Sticks sticks = default;
            sticks.Throttle = ApplyDeadzone(primary.y);
            sticks.Yaw = ApplyDeadzone(primary.x);
            sticks.Pitch = ApplyDeadzone(secondary.y);
            sticks.Roll = ApplyDeadzone(secondary.x);
            sticks.HasInput = sticks.Throttle != 0f || sticks.Yaw != 0f || sticks.Pitch != 0f || sticks.Roll != 0f;
            return sticks;
        }

        private static Sticks ReadFromAxes(IReadOnlyList<AxisControl> axes)
        {
            Sticks sticks = default;
            sticks.Roll = ReadAxis(axes, DroneState.RollAxisIndex);
            sticks.Pitch = ReadAxis(axes, DroneState.PitchAxisIndex);
            sticks.Throttle = ReadAxis(axes, DroneState.ThrottleAxisIndex);
            sticks.Yaw = ReadAxis(axes, DroneState.YawAxisIndex);
            sticks.HasInput = sticks.Roll != 0f || sticks.Pitch != 0f || sticks.Throttle != 0f || sticks.Yaw != 0f;
            return sticks;
        }

        private static float ReadAxis(IReadOnlyList<AxisControl> axes, int index)
        {
            if (index < 0 || index >= axes.Count)
            {
                return 0f;
            }
            return ApplyDeadzone(axes[index].ReadValue());
        }

        private static float ApplyDeadzone(float value)
        {
            return Mathf.Abs(value) < DroneState.StickDeadzone ? 0f : value;
        }

        private static bool TryGetGamepad(out Gamepad gamepad)
        {
            if (_cachedGamepad != null && _cachedGamepad.added)
            {
                gamepad = _cachedGamepad;
                return true;
            }

            ReadOnlyArray<Gamepad> all = Gamepad.all;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] != null && all[i].added)
                {
                    _cachedGamepad = all[i];
                    gamepad = _cachedGamepad;
                    return true;
                }
            }

            _cachedGamepad = null;
            gamepad = null;
            return false;
        }

        private static bool TryGetAxes(out IReadOnlyList<AxisControl> axes)
        {
            axes = _axisControls;
            if (!TryGetJoystick(out Joystick joystick))
            {
                return false;
            }

            _axisControls.Clear();
            ReadOnlyArray<InputControl> controls = joystick.allControls;
            for (int i = 0; i < controls.Count; i++)
            {
                // ButtonControl derives from AxisControl - skip buttons/hat/trigger, we only want
                // continuous stick axes here.
                if (controls[i] is AxisControl axis && !(axis is ButtonControl))
                {
                    _axisControls.Add(axis);
                }
            }
            return true;
        }

        private static bool TryGetJoystick(out Joystick joystick)
        {
            if (_cachedJoystick != null && _cachedJoystick.added)
            {
                joystick = _cachedJoystick;
                return true;
            }

            ReadOnlyArray<Joystick> all = Joystick.all;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] != null && all[i].added)
                {
                    _cachedJoystick = all[i];
                    joystick = _cachedJoystick;
                    return true;
                }
            }

            _cachedJoystick = null;
            joystick = null;
            return false;
        }
    }
}
