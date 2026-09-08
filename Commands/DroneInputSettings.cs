using CTDynamicModMenu.Commands;
using HTFDrone.Drone;

namespace HTFDrone.Commands
{
    /// <summary>
    /// Sliders for transmitter input: deadzone, per-axis sensitivity, and the raw-joystick axis
    /// mapping. Sensitivity is applied centrally in TransmitterInput.ApplyRates, so it works the
    /// same whether the transmitter enumerated as a Gamepad or a raw HID Joystick.
    ///
    /// The axis indices only matter in raw-joystick mode - run /dronediag to see what was
    /// detected and which index is which before moving them. /droneaxis sets the same four by
    /// name. Note these are indices, not sensitivities: moving one repatches which physical
    /// stick drives what.
    /// </summary>
    internal class DroneInputSettings : CustomCommand
    {
        public override string Name => "Drone Input";
        public override string Description => "Stick deadzone and raw-joystick axis mapping. Run /dronediag first to see detected axes.";
        public override string Format => "/droneinput";
        public override string Category => "Drone";

        public override bool HasConfig => true;
        public override bool PersistConfig => true;

        public DroneInputSettings()
        {
            AddSlider(new CommandSlider(
                "Stick Deadzone", 0f, 0.5f, DroneState.StickDeadzone, 0.01f, false,
                "Stick travel ignored around centre, to soak up transmitter jitter",
                value => DroneState.StickDeadzone = value));

            // Sensitivity per axis. Top of range is 3x stock rather than 1x, because the useful
            // direction is usually up - stock rate is already tame for acro. A negative value
            // would invert, but that is what the Invert* flags are for, so the range stops at 0.
            AddSlider(new CommandSlider(
                "Pitch Sensitivity", 0f, 3f, DroneState.PitchSensitivity, 0.05f, false,
                "Multiplier on pitch stick travel (1 = stock)",
                value => DroneState.PitchSensitivity = value));

            AddSlider(new CommandSlider(
                "Roll Sensitivity", 0f, 3f, DroneState.RollSensitivity, 0.05f, false,
                "Multiplier on roll stick travel (1 = stock)",
                value => DroneState.RollSensitivity = value));

            AddSlider(new CommandSlider(
                "Yaw Sensitivity", 0f, 3f, DroneState.YawSensitivity, 0.05f, false,
                "Multiplier on yaw stick travel (1 = stock)",
                value => DroneState.YawSensitivity = value));

            AddSlider(new CommandSlider(
                "Throttle Sensitivity", 0f, 3f, DroneState.ThrottleSensitivity, 0.05f, false,
                "Multiplier on throttle stick travel (1 = stock)",
                value => DroneState.ThrottleSensitivity = value));

            // Inversion as 0/1 sliders. The menu renders a command as either a toggle or a
            // button, and this command is already a button, so a per-axis bool has nowhere else
            // to live - four separate toggle commands for this would bloat the Drone category.
            AddIntSlider("Invert Pitch", 0, 1, DroneState.InvertPitch ? 1 : 0,
                "1 reverses the pitch channel")
                .OnValueChanged = value => DroneState.InvertPitch = value >= 0.5f;

            AddIntSlider("Invert Roll", 0, 1, DroneState.InvertRoll ? 1 : 0,
                "1 reverses the roll channel")
                .OnValueChanged = value => DroneState.InvertRoll = value >= 0.5f;

            AddIntSlider("Invert Yaw", 0, 1, DroneState.InvertYaw ? 1 : 0,
                "1 reverses the yaw channel")
                .OnValueChanged = value => DroneState.InvertYaw = value >= 0.5f;

            AddIntSlider("Invert Throttle", 0, 1, DroneState.InvertThrottle ? 1 : 0,
                "1 reverses the throttle channel")
                .OnValueChanged = value => DroneState.InvertThrottle = value >= 0.5f;

            // Whole numbers - these index into the joystick's axis-typed controls, so a
            // fractional value would be meaningless. 0-15 covers any sane HID report.
            AddIntSlider("Roll Axis", 0, 15, DroneState.RollAxisIndex,
                "HID axis index driving roll (raw joystick mode only)")
                .OnValueChanged = value => DroneState.RollAxisIndex = (int)value;

            AddIntSlider("Pitch Axis", 0, 15, DroneState.PitchAxisIndex,
                "HID axis index driving pitch (raw joystick mode only)")
                .OnValueChanged = value => DroneState.PitchAxisIndex = (int)value;

            AddIntSlider("Throttle Axis", 0, 15, DroneState.ThrottleAxisIndex,
                "HID axis index driving throttle (raw joystick mode only)")
                .OnValueChanged = value => DroneState.ThrottleAxisIndex = (int)value;

            AddIntSlider("Yaw Axis", 0, 15, DroneState.YawAxisIndex,
                "HID axis index driving yaw (raw joystick mode only)")
                .OnValueChanged = value => DroneState.YawAxisIndex = (int)value;
        }

        public override void LoadCustomConfig()
        {
            DroneState.StickDeadzone = GetSliderValue("Stick Deadzone", DroneState.StickDeadzone);
            DroneState.PitchSensitivity = GetSliderValue("Pitch Sensitivity", DroneState.PitchSensitivity);
            DroneState.RollSensitivity = GetSliderValue("Roll Sensitivity", DroneState.RollSensitivity);
            DroneState.YawSensitivity = GetSliderValue("Yaw Sensitivity", DroneState.YawSensitivity);
            DroneState.ThrottleSensitivity = GetSliderValue("Throttle Sensitivity", DroneState.ThrottleSensitivity);
            DroneState.InvertPitch = GetSliderIntValue("Invert Pitch", DroneState.InvertPitch ? 1 : 0) != 0;
            DroneState.InvertRoll = GetSliderIntValue("Invert Roll", DroneState.InvertRoll ? 1 : 0) != 0;
            DroneState.InvertYaw = GetSliderIntValue("Invert Yaw", DroneState.InvertYaw ? 1 : 0) != 0;
            DroneState.InvertThrottle = GetSliderIntValue("Invert Throttle", DroneState.InvertThrottle ? 1 : 0) != 0;
            DroneState.RollAxisIndex = GetSliderIntValue("Roll Axis", DroneState.RollAxisIndex);
            DroneState.PitchAxisIndex = GetSliderIntValue("Pitch Axis", DroneState.PitchAxisIndex);
            DroneState.ThrottleAxisIndex = GetSliderIntValue("Throttle Axis", DroneState.ThrottleAxisIndex);
            DroneState.YawAxisIndex = GetSliderIntValue("Yaw Axis", DroneState.YawAxisIndex);
        }

        public override void Execute(CommandInput message)
        {
            CTDynamicModMenu.CTDynamicModMenu.Instance.DisplayMessage(
                $"Deadzone {DroneState.StickDeadzone:0.00}, sens P{DroneState.PitchSensitivity:0.00} " +
                $"R{DroneState.RollSensitivity:0.00} Y{DroneState.YawSensitivity:0.00} T{DroneState.ThrottleSensitivity:0.00}, " +
                $"axes R{DroneState.RollAxisIndex} P{DroneState.PitchAxisIndex} " +
                $"T{DroneState.ThrottleAxisIndex} Y{DroneState.YawAxisIndex}");
        }
    }
}
