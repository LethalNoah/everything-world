using EverythingWorld.Music;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EverythingWorld.World
{
    /// Goes on the player in the island world:
    /// - tells the MusicDirector which island you're on, so you hear that island's radio station
    ///   (on bridges and at sea you stay tuned to the last island)
    /// - keeps you out of deep water (no swimming yet)
    /// - fast travel: near a tower, press 1-8 to step out of another tower
    [RequireComponent(typeof(CharacterController))]
    public class IslandExplorer : MonoBehaviour
    {
        [Tooltip("How deep you may wade before being turned back, in metres.")]
        public float maxWadeDepth = .9f;
        [Tooltip("How close you need to be to a tower to use it, in metres.")]
        public float towerReach = 16f;

        CharacterController controller;
        Vector3 lastSafe;
        float nextIslandCheck;
        int currentIsland = -2;
        TowerInfo nearTower;
        GUIStyle style;

        public string IslandName { get; private set; } = "";

        void Start()
        {
            controller = GetComponent<CharacterController>();
            var world = IslandWorldBuilder.Instance;
            if (world != null)
            {
                var l = world.Data.layout;
                PlaceAt(new Vector3(l.spawnX, 0, l.spawnZ));
            }
            lastSafe = transform.position;
        }

        void Update()
        {
            var world = IslandWorldBuilder.Instance;
            if (world == null) return;
            var p = transform.position;
            var feet = p.y + controller.center.y - controller.height / 2;

            // deep water: back to the last dry spot
            if (feet < world.Data.layout.waterLevel - maxWadeDepth) PlaceAt(lastSafe, keepHeight: true);
            else if (controller.isGrounded && feet > world.Data.layout.waterLevel - .3f) lastSafe = p;

            if (Time.time >= nextIslandCheck)
            {
                nextIslandCheck = Time.time + .25f;
                var isl = world.Data.IslandAt(p.x, p.z);
                var id = isl?.sceneId ?? -1;
                if (id != currentIsland)
                {
                    currentIsland = id;
                    if (isl != null) IslandName = isl.name;
                    if (MusicDirector.Instance != null) MusicDirector.Instance.SetArea(id);
                }
                nearTower = null;
                foreach (var t in world.Towers)
                    if (Vector3.Distance(new Vector3(p.x, t.position.y, p.z), t.position) < towerReach) nearTower = t.GetComponent<TowerInfo>();
            }

            var keyboard = Keyboard.current;
            if (nearTower != null && keyboard != null)
                for (var i = 0; i < world.Towers.Count && i < 9; i++)
                    if (keyboard[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame && world.Towers[i].GetComponent<TowerInfo>() != nearTower)
                    {
                        var t = world.Towers[i];
                        PlaceAt(t.position - Vector3.forward * 10f);
                        transform.rotation = Quaternion.Euler(0, 180, 0); // facing away from the door, out into the island
                        break;
                    }
        }

        void PlaceAt(Vector3 p, bool keepHeight = false)
        {
            var world = IslandWorldBuilder.Instance;
            if (!keepHeight) p.y = Mathf.Max(world.Data.HeightAt(p.x, p.z), world.Data.layout.waterLevel) + 1.2f;
            controller.enabled = false;  // a CharacterController ignores direct moves while enabled
            transform.position = p;
            controller.enabled = true;
            lastSafe = p;
        }

        void OnGUI()
        {
            if (nearTower == null || IslandWorldBuilder.Instance == null) return;
            style ??= new GUIStyle(GUI.skin.box) { fontSize = 17, alignment = TextAnchor.UpperLeft };
            var towers = IslandWorldBuilder.Instance.Towers;
            var text = $"{nearTower.towerName}: fast travel\n";
            for (var i = 0; i < towers.Count && i < 9; i++)
            {
                var info = towers[i].GetComponent<TowerInfo>();
                text += info == nearTower ? $"\n   (you are here) {info.towerName}" : $"\n{i + 1}  {info.towerName}  ({info.realm})";
            }
            GUI.Box(new Rect(Screen.width - 432, 12, 420, 40 + 21 * towers.Count), text, style);
        }
    }
}
