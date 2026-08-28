using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;
using Monocle;

namespace myProject
{
    // Port fiel (celeste_source/Celeste/PlayerSprite.cs).
    //
    // NOTE: duas adaptacoes, ambas por causa dos harnesses headless, que constroem o
    // Player sem GraphicsDevice — logo sem atlas e sem banco de sprites. No Celeste isso
    // nunca acontece.
    //   1) o CreateOn so roda se houver banco com a entrada;
    //   2) Play tolera id sem animacao registrada (senao Sprite.Play faz animations[id]
    //      e lanca). Com conteudo carregado o fallback nao dispara: o --sprite-test
    //      garante que todo id pedido pelo Player existe no banco.
    //   3) as quatro propriedades de metadata usam AtlasPath ?? "" — o frame dummy do
    //      item 2 e um MTexture sem AtlasPath, e TryGetValue com chave null lanca
    //      ArgumentNullException. O source indexa direto porque la a textura sempre vem
    //      do atlas.
    public class PlayerSprite : Sprite
    {
        public PlayerSpriteMode Mode { get; private set; }

        public PlayerSprite(PlayerSpriteMode mode) : base(null, null)
        {
            Mode = mode;

            string id = "";
            if (mode == PlayerSpriteMode.Madeline)
                id = "player";
            else if (mode == PlayerSpriteMode.MadelineNoBackpack)
                id = "player_no_backpack";
            else if (mode == PlayerSpriteMode.Badeline)
                id = "badeline";
            else if (mode == PlayerSpriteMode.MadelineAsBadeline)
                id = "player_badeline";
            else if (mode == PlayerSpriteMode.Playback)
                id = "player_playback";

            spriteName = id;
            if (GFX.SpriteBank != null && GFX.SpriteBank.Has(id))
                GFX.SpriteBank.CreateOn(this, id);
        }

        public Vector2 HairOffset
        {
            get
            {
                PlayerAnimMetadata meta;
                if (Texture != null && FrameMetadata.TryGetValue(Texture.AtlasPath ?? "", out meta))
                    return meta.HairOffset;
                return Vector2.Zero;
            }
        }

        public float CarryYOffset
        {
            get
            {
                PlayerAnimMetadata meta;
                if (Texture != null && FrameMetadata.TryGetValue(Texture.AtlasPath ?? "", out meta))
                    return meta.CarryYOffset * Scale.Y;
                return 0f;
            }
        }

        public int HairFrame
        {
            get
            {
                PlayerAnimMetadata meta;
                if (Texture != null && FrameMetadata.TryGetValue(Texture.AtlasPath ?? "", out meta))
                    return meta.Frame;
                return 0;
            }
        }

        public bool HasHair
        {
            get
            {
                PlayerAnimMetadata meta;
                return Texture != null
                    && FrameMetadata.TryGetValue(Texture.AtlasPath ?? "", out meta)
                    && meta.HasHair;
            }
        }

        public bool Running
        {
            get
            {
                return LastAnimationID != null
                    && (LastAnimationID == "flip" || LastAnimationID.StartsWith("run"));
            }
        }

        public bool DreamDashing
        {
            get { return LastAnimationID != null && LastAnimationID.StartsWith("dreamDash"); }
        }

        public override void Play(string id, bool restart = false, bool randomizeFrame = false)
        {
            if (!Has(id))
                AddLoop(id, 0.1f, DummyFrame);
            base.Play(id, restart, randomizeFrame);
        }

        public override void Render()
        {
            // O frame dummy do modo headless nao tem textura — desenhar lancaria NullReference.
            if (Texture == null || Texture.Texture == null)
                return;

            Vector2 renderPosition = RenderPosition;
            // Calc.Floored, nao Vector2.Floor: o MonoGame 3.8 tem um Floor() de instancia
            // que retorna void e ofusca a extensao do Calc.
            RenderPosition = RenderPosition.Floored();
            base.Render();
            RenderPosition = renderPosition;
        }

        public static void CreateFramesMetadata(string sprite)
        {
            foreach (SpriteDataSource source in GFX.SpriteBank.SpriteData[sprite].Sources)
            {
                XmlElement metadata = source.XML["Metadata"];
                if (metadata == null)
                    continue;

                string path = source.Path;
                if (!string.IsNullOrEmpty(source.OverridePath))
                    path = source.OverridePath;

                foreach (object obj in metadata.GetElementsByTagName("Frames"))
                {
                    XmlElement xml = (XmlElement)obj;
                    string prefix = path + xml.Attr("path", "");
                    string[] hair = xml.Attr("hair").Split('|');
                    string[] carry = xml.Attr("carry", "").Split(',');

                    for (int i = 0; i < Math.Max(hair.Length, carry.Length); i++)
                    {
                        PlayerAnimMetadata meta = new PlayerAnimMetadata();
                        string key = prefix + ((i < 10) ? "0" : "") + i;
                        if (i == 0 && !GFX.Game.Has(key))
                            key = prefix;
                        FrameMetadata[key] = meta;

                        if (i < hair.Length)
                        {
                            if (hair[i].Equals("x", StringComparison.OrdinalIgnoreCase) || hair[i].Length <= 0)
                            {
                                meta.HasHair = false;
                            }
                            else
                            {
                                string[] parts = hair[i].Split(':');
                                string[] offset = parts[0].Split(',');
                                meta.HasHair = true;
                                meta.HairOffset = new Vector2(Convert.ToInt32(offset[0]), Convert.ToInt32(offset[1]));
                                meta.Frame = (parts.Length >= 2) ? Convert.ToInt32(parts[1]) : 0;
                            }
                        }

                        if (i < carry.Length && carry[i].Length > 0)
                            meta.CarryYOffset = int.Parse(carry[i]);
                    }
                }
            }
        }

        public static void ClearFramesMetadata()
        {
            FrameMetadata.Clear();
        }

        public const string Idle = "idle";
        public const string Shaking = "shaking";
        public const string FrontEdge = "edge";
        public const string LookUp = "lookUp";
        public const string Walk = "walk";
        public const string RunSlow = "runSlow";
        public const string RunFast = "runFast";
        public const string RunWind = "runWind";
        public const string RunStumble = "runStumble";
        public const string JumpSlow = "jumpSlow";
        public const string FallSlow = "fallSlow";
        public const string Fall = "fall";
        public const string JumpFast = "jumpFast";
        public const string FallFast = "fallFast";
        public const string FallBig = "bigFall";
        public const string LandInPose = "fallPose";
        public const string Tired = "tired";
        public const string TiredStill = "tiredStill";
        public const string WallSlide = "wallslide";
        public const string ClimbUp = "climbUp";
        public const string ClimbDown = "climbDown";
        public const string ClimbLookBackStart = "climbLookBackStart";
        public const string ClimbLookBack = "climbLookBack";
        public const string Dangling = "dangling";
        public const string Duck = "duck";
        public const string Dash = "dash";
        public const string Sleep = "sleep";
        public const string Sleeping = "asleep";
        public const string Flip = "flip";
        public const string Skid = "skid";
        public const string DreamDashIn = "dreamDashIn";
        public const string DreamDashLoop = "dreamDashLoop";
        public const string DreamDashOut = "dreamDashOut";
        public const string SwimIdle = "swimIdle";
        public const string SwimUp = "swimUp";
        public const string SwimDown = "swimDown";
        public const string StartStarFly = "startStarFly";
        public const string StarFly = "starFly";
        public const string StarMorph = "starMorph";
        public const string IdleCarry = "idle_carry";
        public const string RunCarry = "runSlow_carry";
        public const string JumpCarry = "jumpSlow_carry";
        public const string FallCarry = "fallSlow_carry";
        public const string PickUp = "pickup";
        public const string Throw = "throw";
        public const string Launch = "launch";
        public const string TentacleGrab = "tentacle_grab";
        public const string TentacleGrabbed = "tentacle_grabbed";
        public const string TentaclePull = "tentacle_pull";
        public const string TentacleDangling = "tentacle_dangling";
        public const string SitDown = "sitDown";

        public int HairCount = 4;

        private string spriteName;

        private static readonly MTexture DummyFrame = new MTexture();

        private static Dictionary<string, PlayerAnimMetadata> FrameMetadata =
            new Dictionary<string, PlayerAnimMetadata>(StringComparer.OrdinalIgnoreCase);
    }
}
