using FishMode.Content.KrillTree;
using MonoMod.Cil;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Config.UI;
using Terraria.ModLoader.UI;

namespace FishMode;

public class FishMode : Mod
{
    public override void Load()
    {
        var method = typeof(UIModConfig).GetMethod("Draw", BindingFlags.Instance | BindingFlags.Public);
        if (method == null) return;
        MonoModHooks.Modify(method, (il) =>
        {
            var c = new ILCursor(il);
            if (!c.TryGotoNext(i => i.MatchCall(typeof(UICommon).GetMethod("TooltipMouseText")))) return;
            c.EmitDelegate((string text) =>
            {
                var up = PlayerInput.CurrentProfile.InputModes[PlayerInput.CurrentInputMode].KeyStatus["Up"].FirstOrDefault();
                var lockon = Keybinds.LockBind.GetAssignedKeys(PlayerInput.CurrentInputMode).FirstOrDefault();
                return text.Replace("{UpBind}", up).Replace("{LockBind}", lockon);
            });
        });
    }
    public override void PostSetupContent()
    {
        KrillTree.EvaluateUnlocks();
    }
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
    public MovementType MovementMode {  get; set; }
    [DefaultValue(true)]
    public bool ScreenShake { get; set; }

    [Header("Experimental")]

    [DefaultValue(24f)]
    [Range(12f, 64f)]
    public int BodyWidth { get; set; }
    [DefaultValue(64f)]
    [Range(50f, 120f)]
    public int BaseSegmentLength { get; set; }
    [DefaultValue(false)]
    public bool DebugDraw { get; set; }
}