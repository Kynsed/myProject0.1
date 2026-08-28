using System;
using Microsoft.Xna.Framework;
using Monocle;
using myProject;

// Cobre o estado StarFly com RENDER de verdade — o unico caminho que exercita a linha do
// flash branco no Player.Render. Dois bugs moram aqui:
//   1) sem a arte do flash, o Draw lancava NullReference (guarda em Player.cs, GFX.Loaded);
//   2) 'startStarFly' declarado como loop prende o StarFlyCoroutine p/ sempre.
// Nenhum dos dois e alcancavel pelos harnesses headless: o (1) so aparece no render e o
// (2) so aparece deixando o estado rodar por muitos frames.
namespace MonocleSmoke
{
    public static class StarFlyTest
    {
        private static int fails;

        public static int Run()
        {
            Console.WriteLine("== starfly-test (estado StarFly com render) ==");
            fails = 0;

            StarFlyTestGame game = new StarFlyTestGame();
            try
            {
                using (game)
                    game.Run();
                Check("render do StarFly roda sem excecao", true, game.Frames + " frames");
            }
            catch (Exception e)
            {
                Check("render do StarFly roda sem excecao", false, e.GetType().Name + ": " + e.Message);
                Console.WriteLine(e.StackTrace);
            }

            Check("StartStarFly foi aceito", game.Started, "Started=" + game.Started);
            Check("entrou no estado StarFly (19)", game.SawState19, "viu estado 19=" + game.SawState19);
            Check("o flash branco foi renderizado (frames com anim startStarFly)",
                game.IntroRenderFrames > 0, "frames=" + game.IntroRenderFrames);
            Check("startStarFly TERMINA e destrava o StarFlyCoroutine",
                game.LeftIntroAnim, "saiu da anim=" + game.LeftIntroAnim
                    + " apos " + game.IntroRenderFrames + " frames");

            Console.WriteLine(fails == 0 ? "== STARFLY OK ==" : ("== " + fails + " FALHA(S) =="));
            return fails == 0 ? 0 : 1;
        }

        internal static void Check(string name, bool ok, string detail)
        {
            Console.WriteLine((ok ? "  PASS  " : "  FAIL  ") + name + "  [" + detail + "]");
            if (!ok)
                fails++;
        }
    }

    public class StarFlyTestGame : PlayGame
    {
        private const int TriggerFrame = 30;
        private const int MaxFrames = 240;

        private int frames;

        public bool Started { get; private set; }
        public bool SawState19 { get; private set; }
        public bool LeftIntroAnim { get; private set; }
        public int IntroRenderFrames { get; private set; }
        public int Frames { get { return frames; } }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            frames++;

            Level level = Engine.Scene as Level;
            Player player = (level == null) ? null : level.Tracker.GetEntity<Player>();
            if (player == null)
            {
                if (frames > MaxFrames)
                    Exit();
                return;
            }

            if (frames == TriggerFrame && !Started)
                Started = player.StartStarFly();

            if (Started)
            {
                if (player.StateMachine.State == 19)
                    SawState19 = true;

                if (player.Sprite.CurrentAnimationID == "startStarFly")
                    IntroRenderFrames++;
                else if (IntroRenderFrames > 0)
                    LeftIntroAnim = true;
            }

            if (frames > MaxFrames)
                Exit();
        }
    }
}
