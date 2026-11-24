namespace ThreeRealms
{
    /// <summary>The eight starting Frame hardpoints.</summary>
    public enum HardpointId
    {
        CO = 0, // Core
        CH = 1, // Chassis
        LA = 2, // Left Arm
        RA = 3, // Right Arm
        LL = 4, // Left Leg
        RL = 5, // Right Leg
        WM = 6, // Weapon Mount
        SM = 7  // Shift Module
    }

    public static class HardpointNames
    {
        public static readonly HardpointId[] All =
        {
            HardpointId.CO, HardpointId.CH, HardpointId.LA, HardpointId.RA,
            HardpointId.LL, HardpointId.RL, HardpointId.WM, HardpointId.SM
        };

        public static string Code(HardpointId id) => id.ToString();

        public static string Display(HardpointId id) => id switch
        {
            HardpointId.CO => "Core",
            HardpointId.CH => "Chassis",
            HardpointId.LA => "Left Arm",
            HardpointId.RA => "Right Arm",
            HardpointId.LL => "Left Leg",
            HardpointId.RL => "Right Leg",
            HardpointId.WM => "Weapon Mount",
            HardpointId.SM => "Shift Module",
            _ => id.ToString()
        };
    }
}
