namespace EverythingWorld.Music
{
    /// Where the "play this track" command goes.
    /// Today: DryRunMusicOutput (only shows and logs it).
    /// Later: a Spotify output that controls the player's own Spotify app.
    public interface IMusicOutput
    {
        void Play(TrackInfo track, SceneInfo scene);
    }
}
