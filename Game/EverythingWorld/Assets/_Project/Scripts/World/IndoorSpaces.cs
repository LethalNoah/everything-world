using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The insides of buildings, registered by the land kits that build them, so the sky knows when you're
    /// under a roof (no snow or rain falling on you indoors).
    public static class IndoorSpaces
    {
        struct Room { public Vector3 center; public Quaternion inverse; public Vector3 half; }
        static readonly List<Room> rooms = new List<Room>();

        public static void Clear() => rooms.Clear();

        /// A box-shaped room: its centre, rotation and full size.
        public static void Add(Vector3 center, Quaternion rotation, Vector3 size) =>
            rooms.Add(new Room { center = center, inverse = Quaternion.Inverse(rotation), half = size / 2f });

        /// Every room: its centre, rotation and full size (the photo tour looks inside each one).
        public static IEnumerable<(Vector3 center, Quaternion rotation, Vector3 size)> All
        {
            get { foreach (var r in rooms) yield return (r.center, Quaternion.Inverse(r.inverse), r.half * 2f); }
        }

        public static bool Contains(Vector3 p)
        {
            foreach (var r in rooms)
            {
                var local = r.inverse * (p - r.center);
                if (Mathf.Abs(local.x) <= r.half.x && Mathf.Abs(local.y) <= r.half.y && Mathf.Abs(local.z) <= r.half.z) return true;
            }
            return false;
        }
    }
}
