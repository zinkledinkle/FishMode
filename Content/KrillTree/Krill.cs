using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using ReLogic.Content;
using System;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace FishMode.Content.KrillTree;

public abstract class Krill : ModTexturedType, ILocalizedModType
{
    public string LocalizationCategory => "Krills";
    public LocalizedText DisplayName => Language.GetOrRegister(this.GetLocalizationKey("DisplayName"), PrettyPrintName);
    public LocalizedText Tooltip => Language.GetOrRegister(this.GetLocalizationKey("Tooltip"), PrettyPrintName);
    private protected Lazy<Asset<Texture2D>> _lazy;
    public Texture2D TextureValue => _lazy.Value.Value;
    public int ID { get; internal set; }
    public abstract int Level { get; }
    public abstract Vector2 Position { get; }
    public abstract List<string> Requires { get; }
    internal List<int> IDRequirements { get; } = [];
    public List<int> Unlocks { get; } = [];
    protected sealed override void Register()
    {
        ModTypeLookup<Krill>.Register(this);
        KrillTree.Register(this);
    }
    public abstract void Apply(Player player);
    internal protected Krill()
    {
        _lazy = new(() => ModContent.Request<Texture2D>(Texture));
    }
}
