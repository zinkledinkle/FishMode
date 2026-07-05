using Terraria.ModLoader;

namespace FishMode;

public class Keybinds : ModSystem
{
    public static ModKeybind LockBind { get; private set; }
    public static ModKeybind Skill1 {  get; private set; }
    public static ModKeybind Skill2 {  get; private set; }
    public static ModKeybind Skill3 {  get; private set; }
    public static ModKeybind Skill4 {  get; private set; }
    public static ModKeybind Skill5 {  get; private set; }
    public static ModKeybind OpenKrillTree { get; private set;  }
    public override void Load()
    {
        LockBind = KeybindLoader.RegisterKeybind(Mod, "LockOn", "R");
        Skill1 = KeybindLoader.RegisterKeybind(Mod, "Skill1", "Z");
        Skill2 = KeybindLoader.RegisterKeybind(Mod, "Skill2", "X");
        Skill3 = KeybindLoader.RegisterKeybind(Mod, "Skill3", "C");
        Skill4 = KeybindLoader.RegisterKeybind(Mod, "Skill4", "V");
        Skill5 = KeybindLoader.RegisterKeybind(Mod, "Skill5", "B");
        OpenKrillTree = KeybindLoader.RegisterKeybind(Mod, "OpenKrillTree", "K");
    }
}