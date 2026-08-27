namespace myProject
{
    // NOTE: assist-mode podado. Estrutura fiel (campos), defaults = off => movimento base fiel.
    public struct Assists
    {
        public bool Invincible;
        public Assists.DashModes DashMode;
        public bool DashAssist;
        public bool InfiniteStamina;
        public bool ThreeSixtyDashing;
        public bool InvisibleMotion;
        public bool NoGrabbing;
        public bool LowFriction;
        public bool SuperDashing;
        public bool Hiccups;
        public bool PlayAsBadeline;

        public enum DashModes
        {
            Normal,
            Two,
            Infinite
        }
    }
}
