using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace FishMode.Common.VanillaChanges;

public class Recipes : ModSystem
{
    public override void PostAddRecipes()
    {
        foreach(var recipe in Main.recipe)
            if (recipe.HasResult(ItemID.TerrasparkBoots))
            {
                recipe.RemoveIngredient(ItemID.FrostsparkBoots);
                recipe.AddIngredient(ItemID.LightningBoots);
            }
    }
}
