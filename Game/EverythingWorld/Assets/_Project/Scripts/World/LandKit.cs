using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The look of one land, built from its WORLD_GUIDE.md entry: its ground colours, its landmark and
    /// scenery (Dress), and how its artist areas look (BuildArtist). Lands without a kit keep the plain
    /// blockout for now. Add new kits to the list in For().
    public abstract class LandKit
    {
        /// The land's name, as in scene_art.json.
        public abstract string Land { get; }

        /// Ground colour for a terrain triangle on this land; return false to use the default colours.
        /// patch is 0..1 noise, for patches about 40 m across.
        public virtual bool Ground(float height, float slope, float patch, out Color color)
        {
            color = default;
            return false;
        }

        /// Runs before the terrain is built: change heights in data.heights on this land (dig a lake...).
        public virtual void ShapeGround(IslandWorldData data, int islandIndex) { }

        /// The landmark, scenery and props.
        public abstract void Dress(LandContext ctx);

        /// One artist's area, on their plot.
        public abstract void BuildArtist(LandContext ctx, IslandWorldData.Artist artist);

        /// Called once after the scenery and every artist area are built.
        public virtual void Finish(LandContext ctx) { }

        static Dictionary<string, LandKit> kits;

        public static LandKit For(string land)
        {
            if (kits == null)
            {
                kits = new Dictionary<string, LandKit>();
                foreach (var k in new LandKit[] { new LanternwoodKit(), new HubKit(), new IndieDownsKit(), new MeadowsKit(), new SmallHoursKit() }) kits[k.Land] = k;
            }
            return land != null && kits.TryGetValue(land, out var kit) ? kit : null;
        }
    }
}
