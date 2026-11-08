namespace EverythingWorld.Music
{
    /// Where the "play this track" command goes.
    /// DryRunMusicOutput only shows and logs it; SpotifyMusicOutput controls the player's own Spotify app.
    public interface IMusicOutput
    {
        /// Play a track starting at positionMs.
        /// continuation = true when the station simply moved on to its next song while you were listening
        /// (rather than you walking into a zone). Outputs should then respect a pause or a song you picked yourself.
        void Play(TrackInfo track, SceneInfo scene, int positionMs, bool continuation);
    }
}
