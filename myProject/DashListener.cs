using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace myProject
{
    // Port fiel (afeta movimento: ouve eventos de dash).
    [Tracked(false)]
    public class DashListener : Component
    {
        public Action<Vector2> OnDash;
        public Action OnSet;

        public DashListener() : base(false, false)
        {
        }
    }
}
