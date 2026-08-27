using System;
using Microsoft.Xna.Framework;

namespace Monocle
{
    public struct SimpleCurve
    {
        public Vector2 Begin;
        public Vector2 End;
        public Vector2 Control;

        public SimpleCurve(Vector2 begin, Vector2 end, Vector2 control)
        {
            Begin = begin;
            End = end;
            Control = control;
        }

        public void DoubleControl()
        {
            Vector2 line = End - Begin;
            Vector2 mid = Begin + line / 2;
            Vector2 fromMid = Control - mid;
            Control += fromMid;
        }

        public Vector2 GetPoint(float percent)
        {
            float reverse = 1f - percent;
            return (reverse * reverse * Begin) + (2f * reverse * percent * Control) + (percent * percent * End);
        }

        public float GetLengthParametric(int resolution)
        {
            Vector2 last = Begin;
            float length = 0;
            for (int i = 1; i <= resolution; i++)
            {
                Vector2 point = GetPoint((float)i / resolution);
                length += (point - last).Length();
                last = point;
            }

            return length;
        }

        public void Render(Vector2 offset, Color color, int resolution)
        {
            Vector2 lastPoint = offset + Begin;
            for (int i = 1; i <= resolution; i++)
            {
                Vector2 point = offset + GetPoint((float)i / resolution);
                Draw.Line(lastPoint, point, color);
                lastPoint = point;
            }
        }

        public void Render(Vector2 offset, Color color, int resolution, float thickness)
        {
            Vector2 lastPoint = offset + Begin;
            for (int i = 1; i <= resolution; i++)
            {
                Vector2 point = offset + GetPoint((float)i / resolution);
                Draw.Line(lastPoint, point, color, thickness);
                lastPoint = point;
            }
        }

        public void Render(Color color, int resolution)
        {
            Render(Vector2.Zero, color, resolution);
        }

        public void Render(Color color, int resolution, float thickness)
        {
            Render(Vector2.Zero, color, resolution, thickness);
        }
    }
}
