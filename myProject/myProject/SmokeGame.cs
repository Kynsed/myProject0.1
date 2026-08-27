using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;

// Smoke test de runtime: parser .data (formato RLE proprietario do Celeste, codigo unsafe).
// SEM classe Program: entry point e o Program.cs.
namespace MonocleSmoke
{
    public class SmokeGame : Engine
    {
        public SmokeGame()
            : base(320, 180, 1280, 720, "Monocle Smoke", false, true)
        {
        }

        protected override void Initialize()
        {
            base.Initialize();
            Scene = new SmokeScene();
            Console.WriteLine("ESC fecha.");
        }
    }

    public class SmokeScene : Scene
    {
        public override void Begin()
        {
            base.Begin();
            Add(new EverythingRenderer());
            Add(new SmokeEntity());
        }
    }

    public class SmokeEntity : Entity
    {
        private MTexture tex;
        private float t;

        public SmokeEntity()
            : base(new Vector2(160, 90))
        {
            // Carrega Content/test.data -> branch .data do VirtualTexture (unsafe/RLE)
            try
            {
                tex = new MTexture(VirtualContent.CreateTexture("test.data"));
                Console.WriteLine(".data carregado: " + tex.Width + "x" + tex.Height);
            }
            catch (Exception e)
            {
                Console.WriteLine("test.data nao carregado (" + e.Message + ").");
            }
        }

        public override void Update()
        {
            base.Update();
            t += Engine.DeltaTime;
            Position = new Vector2(160 + (float)Math.Cos(t) * 40f, 90 + (float)Math.Sin(t) * 40f);
        }

        public override void Render()
        {
            base.Render();

            if (tex != null)
                tex.DrawCentered(Position, Color.White, 6f);   // 16x16 -> 96x96 pra visualizar
            else
                Draw.Rect(Position.X - 8, Position.Y - 8, 16, 16, Color.Magenta);

            Draw.HollowRect(20, 20, 280, 140, Color.LimeGreen);
        }
    }
}
