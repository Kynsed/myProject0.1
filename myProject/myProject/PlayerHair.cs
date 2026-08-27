using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;

namespace myProject
{
    // NOTE: stub visual (cabelo). Port fiel dos campos que o Player ajusta; sem render.
    public class PlayerHair : Component
    {
        public Color Color = Player.NormalHairColor;
        public Color Border = Color.Black;
        public float Alpha = 1f;
        public Facings Facing;
        public bool DrawPlayerSpriteOutline;
        public bool SimulateMotion = true;
        public Vector2 StepPerSegment = new Vector2(0f, 2f);
        public float StepInFacingPerSegment = 0.5f;
        public float StepApproach = 64f;
        public float StepYSinePerSegment;
        public List<Vector2> Nodes = new List<Vector2>();
        public PlayerSprite Sprite;

        public PlayerHair(PlayerSprite sprite) : base(true, true)
        {
            Sprite = sprite;
        }

        public void Start() { }
    }
}
