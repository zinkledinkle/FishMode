using Terraria.ModLoader;

namespace FishMode;

public class Keybinds : ModSystem
{
    public static ModKeybind LockBind { get; private set; }
    public override void Load()
    {
        LockBind = KeybindLoader.RegisterKeybind(Mod, "LockOn", "Z");
    }
}