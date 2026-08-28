using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using myProject;

// Roda o jogo de verdade por N frames e salva o backbuffer em PNG. Serve p/ verificar
// render sem depender de alguem olhar a tela — e a unica forma de auditar a saida grafica.
namespace MonocleSmoke
{
    public class ShotGame : PlayGame
    {
        private readonly string path;
        private readonly int waitFrames;
        private int frames;

        public ShotGame(string path, int waitFrames = 30)
        {
            this.path = path;
            this.waitFrames = waitFrames;
        }

        public static int Run(string[] args)
        {
            string path = (args.Length > 1) ? args[1] : "shot.png";
            int wait = 30;
            if (args.Length > 2)
                int.TryParse(args[2], out wait);

            hideHitboxes = false;
            for (int i = 1; i < args.Length; i++)
                if (args[i] == "nohitbox")
                    hideHitboxes = true;

            using (ShotGame game = new ShotGame(Path.GetFullPath(path), wait))
                game.Run();
            return 0;
        }

        private static bool hideHitboxes;

        protected override void EndDraw()
        {
            PlayScene scene = Engine.Scene as PlayScene;
            if (hideHitboxes && scene != null && scene.Hitboxes != null)
                scene.Hitboxes.Visible = false;

            base.EndDraw();

            if (++frames < waitFrames)
                return;

            int w = GraphicsDevice.PresentationParameters.BackBufferWidth;
            int h = GraphicsDevice.PresentationParameters.BackBufferHeight;
            Color[] data = new Color[w * h];
            GraphicsDevice.GetBackBufferData(data);

            using (Texture2D tex = new Texture2D(GraphicsDevice, w, h))
            {
                tex.SetData(data);
                using (FileStream fs = File.Create(path))
                    tex.SaveAsPng(fs, w, h);
            }

            Console.WriteLine("shot: " + path + " (" + w + "x" + h + ", frame " + frames + ")");
            Exit();
        }
    }
}
