using EverythingWorld.Music;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EverythingWorld.World
{
    /// Goes on the player in the island world:
    /// - tells the MusicDirector which island you're on, so you hear that island's radio station
    ///   (on bridges and at sea you stay tuned to the last island)
    /// - keeps you out of deep water (no swimming yet)
    /// - fast travel: near a tower, press 1-9 to step out of another tower
    /// - inside the area of an artist with 3+ songs, you hear that artist's own station
    /// - tells the SkyDirector which land you're on, so the sky changes with the land
    /// - while you fly (SimplePlayerController, F) it keeps you above the ground and the sea instead of turning you back
    [RequireComponent(typeof(CharacterController))]
    public class IslandExplorer : MonoBehaviour
    {
        [Tooltip("How deep you may wade before being turned back, in metres.")]
        public float maxWadeDepth = .9f;
        [Tooltip("How close you need to be to a tower to use it, in metres.")]
        public float towerReach = 16f;
        [Tooltip("Start on this land (its name, for testing) instead of at the first tower. Empty = first tower.")]
        public string startLand = "The Everything Hub";

        CharacterController controller;
        EverythingWorld.Player.SimplePlayerController walker;
        Vector3 lastSafe;
        float nextIslandCheck;
        int currentIsland = -2, currentArea = -2;
        bool snapSky = true;
        TowerInfo nearTower;
        GUIStyle style;

        public string IslandName { get; private set; } = "";

        void Start()
        {
            controller = GetComponent<CharacterController>(); walker = GetComponent<EverythingWorld.Player.SimplePlayerController>();
            var world = IslandWorldBuilder.Instance;
            if (world != null)
            {
                var l = world.Data.layout;
                PlaceAt(new Vector3(l.spawnX, 0, l.spawnZ));
                foreach (var isl in l.islands)
                    if (!string.IsNullOrEmpty(startLand) && isl.name == startLand)
                    {
                        // a little way out from the land's centre (its plaza), looking back toward it
                        var c = new Vector3(isl.labelX, 0, isl.labelZ);
                        for (var d = 70f; d > 0f; d -= 10f)
                        {
                            var p = c + new Vector3(0, 0, -d);
                            if (world.Data.IslandAt(p.x, p.z) == isl) { PlaceAt(p); break; }
                        }
                        transform.rotation = Quaternion.LookRotation(Vector3.forward);
                    }
            }
            lastSafe = transform.position;
        }

        void Update()
        {
            var world = IslandWorldBuilder.Instance;
            if (world == null) return;
            var p = transform.position;
            var feet = p.y + controller.center.y - controller.height / 2;

            var flying = walker != null && walker.Flying;
            if (flying)
            {
                // flying: never below the ground or the sea
                var floor = Mathf.Max(world.Data.HeightAt(p.x, p.z), world.Data.layout.waterLevel) + .6f;
                if (feet < floor) { p.y += floor - feet; transform.position = p; }
            }
            // deep water: back to the last dry spot
            else if (feet < world.Data.layout.waterLevel - maxWadeDepth) PlaceAt(lastSafe, keepHeight: true);
            else if (controller.isGrounded && feet > world.Data.layout.waterLevel - .3f) lastSafe = p;

            if (Time.time >= nextIslandCheck)
            {
                nextIslandCheck = Time.time + .25f;
                var isl = world.Data.IslandAt(p.x, p.z);
                var id = isl?.sceneId ?? -1;
                if (id != currentIsland)
                {
                    currentIsland = id;
                    if (isl != null)
                    {
                        IslandName = isl.name;
                        var sky = SkyDirector.Instance;
                        if (sky != null) { sky.SetLand(isl.name); if (snapSky) sky.Snap(); }
                        snapSky = false;
                    }
                }
                // inside the area of an artist with their own station you hear only them
                var area = id < 0 ? -1 : world.IsDressed(id) ? ArtistStationAt(world.Data, id, p) : id;
                if (area != currentArea)
                {
                    currentArea = area;
                    if (MusicDirector.Instance != null) MusicDirector.Instance.SetArea(area);
                }
                nearTower = null;
                foreach (var t in world.Towers)
                    if (Vector3.Distance(new Vector3(p.x, t.position.y, p.z), t.position) < towerReach) nearTower = t.GetComponent<TowerInfo>();
            }

            var keyboard = Keyboard.current;
            // H at any tower: straight back to the hub
            if (nearTower != null && keyboard != null && keyboard.hKey.wasPressedThisFrame && nearTower.sceneId != 1000)
                foreach (var ht in world.Towers)
                    if (ht.GetComponent<TowerInfo>().sceneId == 1000)
                    {
                        PlaceAt(ht.position - Vector3.forward * 10f); snapSky = true; transform.rotation = Quaternion.Euler(0, 180, 0); break;
                    }
            if (nearTower != null && keyboard != null)
                for (var i = 0; i < world.Towers.Count && i < 9; i++)
                    if (keyboard[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame && world.Towers[i].GetComponent<TowerInfo>() != nearTower)
                    {
                        var t = world.Towers[i];
                        PlaceAt(t.position - Vector3.forward * 10f);
                        snapSky = true; // arrive under the new land's sky straight away
                        transform.rotation = Quaternion.Euler(0, 180, 0); // facing away from the door, out into the island
                        break;
                    }
        }

        /// The station to hear at this spot: an artist's own station inside their area, else the island's.
        static int ArtistStationAt(IslandWorldData data, int islandSceneId, Vector3 p)
        {
            foreach (var a in data.layout.artists ?? new IslandWorldData.Artist[0])
            {
                if (a.stationId == 0 || a.sceneId != islandSceneId) continue;
                float dx = p.x - a.x, dz = p.z - a.z;
                if (dx * dx + dz * dz < a.r * a.r) return a.stationId;
            }
            return islandSceneId;
        }

        /// Where a player can stand at (or near) a spot: on top of whatever is there (the ground, a deck, a dais, a
        /// floor up to 3 m up, never a roof), with room for their whole body. If that spot is blocked, it looks
        /// outward in rings for the nearest clear one. Returns the body's middle (the CharacterController's centre).
        public static Vector3 StandingSpot(Vector3 p, Transform ignore = null)
        {
            var world = IslandWorldBuilder.Instance;
            Vector3 Feet(Vector3 q)
            {
                var ground = Mathf.Max(world.Data.HeightAt(q.x, q.z), world.Data.layout.waterLevel);
                foreach (var h in Physics.RaycastAll(new Vector3(q.x, ground + 40f, q.z), Vector3.down, 60f, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (ignore != null && h.collider.transform.IsChildOf(ignore)) continue;
                    if (h.point.y > ground && h.point.y < ground + 3f && h.normal.y > .6f) ground = h.point.y;
                }
                return new Vector3(q.x, ground, q.z);
            }
            bool Clear(Vector3 feet)
            {
                foreach (var c in Physics.OverlapCapsule(feet + Vector3.up * .45f, feet + Vector3.up * 1.6f, .38f, ~0, QueryTriggerInteraction.Ignore))
                    if (ignore == null || !c.transform.IsChildOf(ignore)) return false;
                return true;
            }
            var f0 = Feet(p);
            if (Clear(f0)) return f0 + Vector3.up * 1.2f;
            for (var r = 1.5f; r <= 24f; r += 1.5f)
                for (var k = 0; k < 16; k++)
                {
                    var a = k * Mathf.PI / 8f; var q = p + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r;
                    var f = Feet(q); if (world.Data.IslandAt(q.x, q.z) != null && Clear(f)) return f + Vector3.up * 1.2f;
                }
            return f0 + Vector3.up * 1.2f;
        }

        void PlaceAt(Vector3 p, bool keepHeight = false)
        {
            var world = IslandWorldBuilder.Instance;
            if (!keepHeight) p = StandingSpot(p, transform);
            controller.enabled = false;  // a CharacterController ignores direct moves while enabled
            transform.position = p;
            controller.enabled = walker == null || !walker.Flying;
            lastSafe = p;
        }

        void OnGUI()
        {
            if (nearTower == null || IslandWorldBuilder.Instance == null) return;
            style ??= GameFonts.Box(17, TextAnchor.UpperLeft);
            var towers = IslandWorldBuilder.Instance.Towers;
            var text = $"{nearTower.towerName}: fast travel\n" + (nearTower.sceneId != 1000 ? "\nH  Back to the Hub" : "");
            for (var i = 0; i < towers.Count && i < 9; i++)
            {
                var info = towers[i].GetComponent<TowerInfo>();
                text += info == nearTower ? $"\n   (you are here) {info.towerName}" : $"\n{i + 1}  {info.towerName}  ({info.realm})";
            }
            GUI.Box(new Rect(Screen.width - 432, 12, 420, 62 + 21 * towers.Count), text, style);
        }
    }
}
