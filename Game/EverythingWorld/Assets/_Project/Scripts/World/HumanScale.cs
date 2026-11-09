namespace EverythingWorld.World
{
    /// Sizes every land kit builds to, so buildings are made for a group of friends, not one person.
    /// The player is about 2 m tall with eyes at about 1.7 m.
    public static class HumanScale
    {
        /// Doorways: wide enough for two people side by side, tall enough not to feel cramped.
        public const float DoorWidth = 2.8f, DoorHeight = 3.4f;
        /// Rooms people gather in should fit 4-6 standing comfortably: at least this wide and deep.
        public const float RoomMin = 8f;
        /// Walls of buildings you walk into.
        public const float WallHeight = 5f;
        /// Main walkways inside (aisles) and paths outside.
        public const float AisleWidth = 3f, PathWidth = 2.2f;
        /// Seats: bench height and depth.
        public const float SeatHeight = .45f, SeatDepth = .45f;
    }
}
