using System;

namespace Monocle
{
    public static class Ease
    {
        public delegate float Easer(float t);

        public static readonly Easer Linear = (float t) => t;

        public static readonly Easer SineIn = (float t) => -(float)Math.Cos(1.5707964f * t) + 1f;
        public static readonly Easer SineOut = (float t) => (float)Math.Sin(1.5707964f * t);
        public static readonly Easer SineInOut = (float t) => -(float)Math.Cos(3.1415927f * t) / 2f + 0.5f;

        public static readonly Easer QuadIn = (float t) => t * t;
        public static readonly Easer QuadOut = Invert(QuadIn);
        public static readonly Easer QuadInOut = Follow(QuadIn, QuadOut);

        public static readonly Easer CubeIn = (float t) => t * t * t;
        public static readonly Easer CubeOut = Invert(CubeIn);
        public static readonly Easer CubeInOut = Follow(CubeIn, CubeOut);

        public static readonly Easer QuintIn = (float t) => t * t * t * t * t;
        public static readonly Easer QuintOut = Invert(QuintIn);
        public static readonly Easer QuintInOut = Follow(QuintIn, QuintOut);

        public static readonly Easer ExpoIn = (float t) => (float)Math.Pow(2.0, 10f * (t - 1f));
        public static readonly Easer ExpoOut = Invert(ExpoIn);
        public static readonly Easer ExpoInOut = Follow(ExpoIn, ExpoOut);

        public static readonly Easer BackIn = (float t) => t * t * (2.70158f * t - 1.70158f);
        public static readonly Easer BackOut = Invert(BackIn);
        public static readonly Easer BackInOut = Follow(BackIn, BackOut);

        public static readonly Easer BigBackIn = (float t) => t * t * (4f * t - 3f);
        public static readonly Easer BigBackOut = Invert(BigBackIn);
        public static readonly Easer BigBackInOut = Follow(BigBackIn, BigBackOut);

        public static readonly Easer ElasticIn = delegate (float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            return 33f * t3 * t2 + -59f * t2 * t2 + 32f * t3 + -5f * t2;
        };

        public static readonly Easer ElasticOut = delegate (float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            return 33f * t3 * t2 + -106f * t2 * t2 + 126f * t3 + -67f * t2 + 15f * t;
        };

        public static readonly Easer ElasticInOut = Follow(ElasticIn, ElasticOut);

        private const float B1 = 0.36363637f;
        private const float B2 = 0.72727275f;
        private const float B3 = 0.54545456f;
        private const float B4 = 0.90909094f;
        private const float B5 = 0.8181818f;
        private const float B6 = 0.95454544f;

        public static readonly Easer BounceIn = delegate (float t)
        {
            t = 1f - t;
            if (t < B1)
                return 1f - 7.5625f * t * t;
            if (t < B2)
                return 1f - (7.5625f * (t - B3) * (t - B3) + 0.75f);
            if (t < B4)
                return 1f - (7.5625f * (t - B5) * (t - B5) + 0.9375f);
            return 1f - (7.5625f * (t - B6) * (t - B6) + 0.984375f);
        };

        public static readonly Easer BounceOut = delegate (float t)
        {
            if (t < B1)
                return 7.5625f * t * t;
            if (t < B2)
                return 7.5625f * (t - B3) * (t - B3) + 0.75f;
            if (t < B4)
                return 7.5625f * (t - B5) * (t - B5) + 0.9375f;
            return 7.5625f * (t - B6) * (t - B6) + 0.984375f;
        };

        public static readonly Easer BounceInOut = delegate (float t)
        {
            if (t < 0.5f)
            {
                t = 1f - t * 2f;
                if (t < B1)
                    return (1f - 7.5625f * t * t) / 2f;
                if (t < B2)
                    return (1f - (7.5625f * (t - B3) * (t - B3) + 0.75f)) / 2f;
                if (t < B4)
                    return (1f - (7.5625f * (t - B5) * (t - B5) + 0.9375f)) / 2f;
                return (1f - (7.5625f * (t - B6) * (t - B6) + 0.984375f)) / 2f;
            }
            else
            {
                t = t * 2f - 1f;
                if (t < B1)
                    return 7.5625f * t * t / 2f + 0.5f;
                if (t < B2)
                    return (7.5625f * (t - B3) * (t - B3) + 0.75f) / 2f + 0.5f;
                if (t < B4)
                    return (7.5625f * (t - B5) * (t - B5) + 0.9375f) / 2f + 0.5f;
                return (7.5625f * (t - B6) * (t - B6) + 0.984375f) / 2f + 0.5f;
            }
        };

        public static Easer Invert(Easer easer)
        {
            return (float t) => 1f - easer(1f - t);
        }

        public static Easer Follow(Easer first, Easer second)
        {
            return delegate (float t)
            {
                if (t > 0.5f)
                    return second(t * 2f - 1f) / 2f + 0.5f;
                return first(t * 2f) / 2f;
            };
        }

        public static float UpDown(float eased)
        {
            if (eased <= 0.5f)
                return eased * 2f;
            return 1f - (eased - 0.5f) * 2f;
        }
    }
}
