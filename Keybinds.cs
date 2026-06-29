using Terraria.ModLoader;

namespace FishMode;

public class Keybinds : ModSystem
{
    public static ModKeybind DashBind { get; private set; }

    public override void Load()
    {
        DashBind = KeybindLoader.RegisterKeybind(Mod, "Dash", "C");
    }
}