using System;
using System.IO;
using System.Reflection;
using System.Runtime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Monocle
{
    public class Engine : Game
    {
        public string Title;
        public Version Version;

        // references
        public static Engine Instance { get; private set; }
        public static GraphicsDeviceManager Graphics { get; private set; }
        public static Commands Commands { get; private set; }
        public static Pooler Pooler { get; private set; }
        public static Action OverloadGameLoop;

        // screen size
        public static int Width { get; private set; }
        public static int Height { get; private set; }
        public static int ViewWidth { get; private set; }
        public static int ViewHeight { get; private set; }
        public static Matrix ScreenMatrix;
        public static Viewport Viewport { get; private set; }
        public static Color ClearColor;
        public static bool ExitOnEscapeKeypress;

        public static int ViewPadding
        {
            get
            {
                return viewPadding;
            }
            set
            {
                viewPadding = value;
                Instance.UpdateView();
            }
        }

        private static int viewPadding = 0;
        private static bool resizing;

        // time
        public static float DeltaTime { get; private set; }
        public static float RawDeltaTime { get; private set; }
        public static float TimeRate = 1f;
        public static float TimeRateB = 1f;
        public static float FreezeTimer;
        public static int FPS;
        public static ulong FrameCounter { get; private set; }

        private TimeSpan counterElapsed = TimeSpan.Zero;
        private int fpsCounter;

        // content directory
        private static string AssemblyDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

        public static string ContentDirectory
        {
            get { return Path.Combine(AssemblyDirectory, Instance.Content.RootDirectory); }
        }

        // scene
        private Scene scene;
        private Scene nextScene;

        // NOTE: campos de dash-assist (Celeste) restaurados como stub p/ o Player compilar.
        // Assist-mode podado: ficam em default (false), sem efeito no movimento base.
        public static bool DashAssistFreeze;
        public static bool DashAssistFreezePress;

        public Engine(int width, int height, int windowWidth, int windowHeight, string windowTitle, bool fullscreen, bool vsync)
        {
            Instance = this;

            Title = Window.Title = windowTitle;
            Width = width;
            Height = height;
            ClearColor = Color.Black;
            InactiveSleepTime = new TimeSpan(0);

            Graphics = new GraphicsDeviceManager(this);
            Graphics.DeviceReset += OnGraphicsReset;
            Graphics.DeviceCreated += OnGraphicsCreate;
            Graphics.SynchronizeWithVerticalRetrace = vsync;
            Graphics.PreferMultiSampling = false;
            Graphics.GraphicsProfile = GraphicsProfile.HiDef;
            Graphics.PreferredBackBufferFormat = SurfaceFormat.Color;
            Graphics.PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8;

            Window.AllowUserResizing = true;
            Window.ClientSizeChanged += OnClientSizeChanged;

            if (fullscreen)
            {
                Graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
                Graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
                Graphics.IsFullScreen = true;
            }
            else
            {
                Graphics.PreferredBackBufferWidth = windowWidth;
                Graphics.PreferredBackBufferHeight = windowHeight;
                Graphics.IsFullScreen = false;
            }

            Content.RootDirectory = "Content";

            IsMouseVisible = false;
            ExitOnEscapeKeypress = true;

            GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
        }

        protected virtual void OnClientSizeChanged(object sender, EventArgs e)
        {
            if (Window.ClientBounds.Width > 0 && Window.ClientBounds.Height > 0 && !resizing)
            {
                resizing = true;

                Graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
                Graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;
                UpdateView();

                resizing = false;
            }
        }

        protected virtual void OnGraphicsReset(object sender, EventArgs e)
        {
            UpdateView();

            if (scene != null)
                scene.HandleGraphicsReset();
            if (nextScene != null && nextScene != scene)
                nextScene.HandleGraphicsReset();
        }

        protected virtual void OnGraphicsCreate(object sender, EventArgs e)
        {
            UpdateView();

            if (scene != null)
                scene.HandleGraphicsCreate();
            if (nextScene != null && nextScene != scene)
                nextScene.HandleGraphicsCreate();
        }

        protected override void OnActivated(object sender, EventArgs args)
        {
            base.OnActivated(sender, args);

            if (scene != null)
                scene.GainFocus();
        }

        protected override void OnDeactivated(object sender, EventArgs args)
        {
            base.OnDeactivated(sender, args);

            if (scene != null)
                scene.LoseFocus();
        }

        protected override void Initialize()
        {
            base.Initialize();

            MInput.Initialize();
            Tracker.Initialize();
            Pooler = new Pooler();
            Commands = new Commands();
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            VirtualContent.Reload();
            Monocle.Draw.Initialize(GraphicsDevice);
        }

        protected override void UnloadContent()
        {
            base.UnloadContent();

            VirtualContent.Unload();
        }

        protected override void Update(GameTime gameTime)
        {
            RawDeltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            DeltaTime = RawDeltaTime * TimeRate * TimeRateB;
            FrameCounter++;

            // update input
            MInput.Update();

            if (ExitOnEscapeKeypress && MInput.Keyboard.Pressed(Keys.Escape))
            {
                Exit();
                return;
            }

            if (OverloadGameLoop != null)
            {
                OverloadGameLoop();
                base.Update(gameTime);
                return;
            }

            // NOTE: Celeste-specific DashAssistFreeze block removed
            // (used Input.Dash, PlayerDashAssist, and Level)

            // update current scene
            if (FreezeTimer > 0)
                FreezeTimer = Math.Max(FreezeTimer - RawDeltaTime, 0);
            else if (scene != null)
            {
                scene.BeforeUpdate();
                scene.Update();
                scene.AfterUpdate();
            }

            // debug console
            if (Commands.Open)
                Commands.UpdateOpen();
            else if (Commands.Enabled)
                Commands.UpdateClosed();

            // changing scenes
            if (scene != nextScene)
            {
                Scene from = scene;
                if (scene != null)
                    scene.End();
                scene = nextScene;
                OnSceneTransition(from, nextScene);
                if (scene != null)
                    scene.Begin();
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            RenderCore();

            base.Draw(gameTime);
            if (Commands.Open)
                Commands.Render();

            // frame counter
            fpsCounter++;
            counterElapsed += gameTime.ElapsedGameTime;
            if (counterElapsed >= TimeSpan.FromSeconds(1))
            {
                FPS = fpsCounter;
                fpsCounter = 0;
                counterElapsed -= TimeSpan.FromSeconds(1);
            }
        }

        /// <summary>
        /// Override if you want to change the core rendering functionality of Monocle Engine.
        /// By default, this simply sets the render target to null, clears the screen, and renders the current Scene
        /// </summary>
        protected virtual void RenderCore()
        {
            if (scene != null)
                scene.BeforeRender();

            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Viewport = Viewport;
            GraphicsDevice.Clear(ClearColor);

            if (scene != null)
            {
                scene.Render();
                scene.AfterRender();
            }
        }

        // NOTE: MonoGame 3.8.2+ mudou assinatura pra ExitingEventArgs.
        // Em FNA (e MonoGame antigo) o tipo é EventArgs — troque se mudar de backend.
        protected override void OnExiting(object sender, ExitingEventArgs args)
        {
            base.OnExiting(sender, args);
            MInput.Shutdown();
        }

        public void RunWithLogging()
        {
            try
            {
                Run();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                ErrorLog.Write(e);
                ErrorLog.Open();
            }
        }

        #region Scene

        /// <summary>
        /// Called after a Scene ends, before the next Scene begins
        /// </summary>
        protected virtual void OnSceneTransition(Scene from, Scene to)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();

            TimeRate = 1f;
        }

        /// <summary>
        /// The currently active Scene. Note that if set, the Scene will not actually change until the end of the Update
        /// </summary>
        public static Scene Scene
        {
            get { return Instance.scene; }
            set { Instance.nextScene = value; }
        }

        #endregion

        #region Screen

        public static void SetWindowed(int width, int height)
        {
            if (width > 0 && height > 0)
            {
                resizing = true;
                Graphics.PreferredBackBufferWidth = width;
                Graphics.PreferredBackBufferHeight = height;
                Graphics.IsFullScreen = false;
                Graphics.ApplyChanges();
                Console.WriteLine("WINDOW-" + width + "x" + height);
                resizing = false;
            }
        }

        public static void SetFullscreen()
        {
            resizing = true;
            Graphics.PreferredBackBufferWidth = Graphics.GraphicsDevice.Adapter.CurrentDisplayMode.Width;
            Graphics.PreferredBackBufferHeight = Graphics.GraphicsDevice.Adapter.CurrentDisplayMode.Height;
            Graphics.IsFullScreen = true;
            Graphics.ApplyChanges();
            Console.WriteLine("FULLSCREEN");
            resizing = false;
        }

        private void UpdateView()
        {
            float screenWidth = GraphicsDevice.PresentationParameters.BackBufferWidth;
            float screenHeight = GraphicsDevice.PresentationParameters.BackBufferHeight;

            // get View Size
            if (screenWidth / Width > screenHeight / Height)
            {
                ViewWidth = (int)(screenHeight / Height * Width);
                ViewHeight = (int)screenHeight;
            }
            else
            {
                ViewWidth = (int)screenWidth;
                ViewHeight = (int)(screenWidth / Width * Height);
            }

            // apply View Padding
            float aspect = ViewHeight / (float)ViewWidth;
            ViewWidth -= ViewPadding * 2;
            ViewHeight -= (int)(aspect * ViewPadding * 2);

            // update screen matrix
            ScreenMatrix = Matrix.CreateScale(ViewWidth / (float)Width);

            // update viewport
            Viewport = new Viewport
            {
                X = (int)(screenWidth / 2 - ViewWidth / 2),
                Y = (int)(screenHeight / 2 - ViewHeight / 2),
                Width = ViewWidth,
                Height = ViewHeight,
                MinDepth = 0,
                MaxDepth = 1
            };
        }

        #endregion
    }
}
