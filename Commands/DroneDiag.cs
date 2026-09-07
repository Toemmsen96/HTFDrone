using System.Text;
using CTDynamicModMenu.Commands;
using HTFDrone.Drone;

namespace HTFDrone.Commands
{
    /// <summary>
    /// Lists every detected joystick/transmitter axis and its current live value, so you can
    /// figure out which index is which stick on your Taranis QX7 (move one stick at a time and
    /// see which number changes) and set it with /droneaxis.
    /// </summary>
    internal class DroneDiag : CustomCommand
    {
        public override string Name => "Drone Transmitter Diagnostics";
        public override string Description => "Lists detected joystick axes and their live values, to help map your transmitter.";
        public override string Format => "/dronediag";
        public override string Category => "Drone";

        public override void Execute(CommandInput message)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string line in TransmitterInput.DescribeAxes())
            {
                sb.AppendLine(line);
            }
            CTDynamicModMenu.CTDynamicModMenu.Instance.DisplayMessage(sb.ToString());
        }
    }
}
