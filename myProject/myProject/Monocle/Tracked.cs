using System;

namespace Monocle
{
    [AttributeUsage(AttributeTargets.Class)]
    public class Tracked : Attribute
    {
        public bool Inherited;

        public Tracked(bool inherited = false)
        {
            Inherited = inherited;
        }
    }
}
