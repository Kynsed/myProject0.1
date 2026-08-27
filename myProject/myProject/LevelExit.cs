using Monocle;

namespace myProject
{
    // NOTE: stub de saida de nivel (conteudo). Scene de transicao; no-op.
    public class LevelExit : Scene
    {
        public enum Mode { Completed, GiveUp, Restart, GoldenBerryRestart, GoldenBerryCancel, SaveAndQuit }

        public string GoldenStrawberryEntryLevel;

        public LevelExit(Mode mode, Session session, object snow = null)
        {
        }
    }
}
