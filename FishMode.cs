using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace FishMode;

public class FishMode : Mod
{

}
public class FishModeConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ClientSide;
    public enum MovementType
    {
        LookAndLock,
        WASD
    }
    [Cycle]
    [DefaultValue(MovementType.LookAndLock)]
    [TooltipArgs()]
    public MovementType MovementMode {  get; set; }
    [DefaultValue(true)]
    public bool ScreenShake { get; set; }

    [Header("experimental")]

    [DefaultValue(24f)]
    [Range(12f, 64f)]
    public int BodyWidth { get; set; }
    [DefaultValue(64f)]
    [Range(50f, 120f)]
    public int BaseSegmentLength { get; set; }
    [DefaultValue(false)]
    public bool DebugDraw { get; set; }
}