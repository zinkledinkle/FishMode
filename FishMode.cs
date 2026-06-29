using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace FishMode;

public class FishMode : Mod
{

}
public class FishModeConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ServerSide;
    [DefaultValue(5)]
    [Range(3, 8)]
    public int BodySegmentCount { get; set; }
    [DefaultValue(24f)]
    [Range(12f, 64f)]
    public int BodyWidth { get; set; }
    [DefaultValue(64f)]
    [Range(50f, 120f)]
    public int BodyLength { get; set; }
}