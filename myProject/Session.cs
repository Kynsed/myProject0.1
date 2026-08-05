using Microsoft.Xna.Framework;

namespace myProject
{
    // NOTE: stub de estado de sessao (conteudo). So a superficie lida pelo Player/Level.
    public class Session
    {
        public enum CoreModes { None, Hot, Cold }

        public AudioState Audio = new AudioState();
        public PlayerInventory Inventory = PlayerInventory.Default;
        public int Dashes;
        public string Level;
        public Vector2? RespawnPoint;
        public AreaKey Area;
        public int Deaths;
        public int DeathsInCurrentLevel;
    }

    public class AudioState
    {
        public MusicState Music = new MusicState();
        public void Apply(bool forceSixteenthNoteHack = false) { }
    }

    public class MusicState
    {
        public string Event;
    }
}
