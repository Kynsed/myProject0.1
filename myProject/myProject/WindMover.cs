using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace myProject
{
    // Port fiel (afeta movimento: vento move a entidade).
    [Tracked(false)]
    public class WindMover : Component
    {
        public Action<Vector2> Move;

        public WindMover(Action<Vector2> move) : base(false, false)
        {
            Move = move;
        }
    }
}
