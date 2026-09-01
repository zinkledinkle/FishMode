using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using ReLogic.Content;
using System;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using FishMode.Content.KrillTree.Krills;

namespace FishMode.Content.KrillTree;

public abstract class Krill : ModTexturedType, ILocalizedModType
{
    public string LocalizationCategory => "Krills";
    public LocalizedText DisplayName => Language.GetOrRegister(this.GetLocalizationKey("DisplayName"), PrettyPrintName);
    public LocalizedText Tooltip => Language.GetOrRegister(this.GetLocalizationKey("Tooltip"), () => "");
    private protected Lazy<Asset<Texture2D>> _lazy;
    public Texture2D TextureValue => _lazy.Value.Value;
    public int _id;
    public abstract int Level { get; }
    public abstract Vector2 Position { get; }
    internal List<int> IDRequirements { get; } = [];
    public virtual IReadOnlyList<Type> Requirements => [typeof(Waves)];
    public virtual IReadOnlyList<Type> CombinesWith => [];
    public List<int> Combinations { get; } = [];
    public List<int> Unlocks { get; } = [];
    protected sealed override void Register()
    {
        ModTypeLookup<Krill>.Register(this);
        KrillTree.Register(this);
    }
    public virtual void Reset() { }
    public abstract void Apply(Player player);
    public virtual void Visuals(Player player) { }
    public virtual void CombinationEffect(int otherType, Player player, ref bool deactivateOtherEffect) { }
    internal protected Krill()
    {
        _lazy = new(() =>
        {
            if (ModContent.RequestIfExists<Texture2D>(Texture, out var tex)) return tex;
            else return Assets.Textures.UI.NoSprite.Asset;
        });
    }
}