using Microsoft.Xna.Framework;
using MonoMod.Cil;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Generation;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.ObjectData;
using Terraria.WorldBuilding;

namespace FishMode.Common.VanillaChanges;

public class WaterWorld : ModSystem
{
    public override void Load()
    {
        //remove desert lava thing
        WorldGen.ModifyPass((PassLegacy)WorldGen.VanillaGenPasses["Final Cleanup"], static (il) =>
        {
            var c = new ILCursor(il);
            while (c.TryGotoNext(i => i.MatchLdcI4(WallID.Sandstone))) { }
            c.Next.Operand = int.MaxValue; //lol
            c.GotoNext(i => i.MatchLdcI4(WallID.HardenedSand));
            c.Next.Operand = int.MaxValue;
        });
    }
    public override void PostSetupContent()
    {
        for (int i = 0; i < Main.tileWaterDeath.Length; i++) Main.tileWaterDeath[i] = false;
        for (int i = 0; i < TileObjectData._data.Count; i++) TileObjectData._data[i]?.WaterDeath = false;
    }
    public override void ModifyWorldGenTasks(List<GenPass> tasks)
    {
        int index = tasks.FindIndex(pass => pass.Name == "Settle Liquids Again");
        if (index == -1) return;
        tasks.Insert(index, new WaterWorldPass());
    }
    public class WaterWorldPass : GenPass
    {
        public WaterWorldPass() : base("FishMode/WaterWorld", 1f) { }
        protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
        {
            progress.Message = "Drowning EVERYTHING";
            Drown();
            Bowl();
        }
        private static void Drown()
        {
            int lol = WorldGen.genRand.Next((int)(Main.maxTilesX * 0.07f), (int)(Main.maxTilesX * 0.09f));
            for (int i = 0; i < lol; i++)
            {
                int size = 50;
                var x = WorldGen.genRand.Next(size, Main.maxTilesX - size);
                var y = WorldGen.genRand.Next((int)(Main.maxTilesY * 0.15f), Main.maxTilesY - 100);
                WorldUtils.Gen(new(x, y), new Shapes.Circle(size, size), new Actions.SetLiquid(LiquidID.Water));
            }
            //(int numChunksX, int numChunksY) = (60, 20);
            //int chunkSizeX = Main.maxTilesX / numChunksX;

            //float top = 0.1f;
            //float bottom = 0.15f;
            //float reduceY = 1 - (top + bottom);
            //int chunkSizeY = (int)(Main.maxTilesY * reduceY) / numChunksY;
            //int startY = (int)(Main.maxTilesY * top);
            //int endY = (int)(Main.maxTilesY * (1 - bottom));

            //int remainderX = Main.maxTilesX - chunkSizeX * numChunksX;
            //for (int x = remainderX / 2; x < (numChunksX - 1) * chunkSizeX + remainderX / 2; x += chunkSizeX)
            //{
            //    for (int y = startY; y < endY; y += chunkSizeY)
            //    {
            //        if (Main.rand.NextBool(2)) continue;
            //        WorldUtils.Gen(new Point(x, y), new Shapes.Rectangle(chunkSizeX, chunkSizeY), new Actions.SetLiquid(LiquidID.Water));
            //    }
            //}
        }
        private static void Bowl()
        {
            int sizeX = (int)(Main.maxTilesX * 0.02f);
            int sizeY = (int)(Main.maxTilesX * 0.001f);
            WorldUtils.Gen(new Point(Main.spawnTileX, Main.spawnTileY), new Shapes.Circle(sizeX, sizeY), new Actions.Clear());
            WorldGen.Lakinater(Main.spawnTileX, Main.spawnTileY, 2);
        }
    }
}
