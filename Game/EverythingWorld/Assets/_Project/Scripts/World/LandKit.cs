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

        /// Songs that make something special happen on this land (track id, what happens), so the photo tour
        /// (ShotTour) can show them.
        /// Which way an artist's scene faces (where you'd stand to look at it), for the photo tour. By default,
        /// toward where you arrive.
        public virtual Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a)
        {
            var isl = data.IslandAt(a.x, a.z);
            if (isl == null) return Vector3.forward;
            var d = new Vector3(isl.labelX - a.x, 0f, isl.labelZ - a.z);
            return d.sqrMagnitude < 1f ? Vector3.forward : d.normalized;
        }

        /// The artist is where it happens (null: everywhere, shot from where you arrive).
        public virtual (string song, string label, string artist)[] SpecialSongs => new (string, string, string)[0];

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
                foreach (var k in new LandKit[] { new LanternwoodKit(), new HubKit(), new IndieDownsKit(), new MeadowsKit(), new SmallHoursKit(), new DriftwoodKit(), new FensKit(), new ElectricKit(), new GarageKit(), new ClassicKit(), new BayouKit(), new HairKit(), new ArenaKit(), new PsychKit(), new JamKit(), new YachtKit(), new FolkKit(), new BluegrassKit(), new ProgKit(), new IronKit(), new MoorsKit(), new GrungeKit(), new DocksKit(), new CalderaKit(), new DjentKit(), new PowerKit(), new MathKit(), new JRockKit(), new PunkKit(), new QuarryKit(), new OddballKit(), new SoulKit(), new FunkKit(), new DiscoKit(), new FusionKit(), new JazzKit(), new DeltaKit(), new AfroKit(), new FarKit(), new BoomKit(), new VelvetKit(), new RootsKit(), new NeoKit(), new HipKit(), new LofiKit(), new PopKit(), new HeartKit(), new GlitKit(), new CountryKit(), new SynthKit(), new LatinKit(), new MemeKit(), new StudioKit(), new ScoresKit(), new BitKit(), new ConcertKit(), new HighKit(), new MistKit(), new FloorKit() }) kits[k.Land] = k;
            }
            return land != null && kits.TryGetValue(land, out var kit) ? kit : null;
        }
    }
}
