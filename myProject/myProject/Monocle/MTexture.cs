using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Monocle
{
    public class MTexture
    {
        public string AtlasPath;

        public VirtualTexture Texture { get; private set; }
        public Rectangle ClipRect { get; private set; }
        public Vector2 DrawOffset { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public Vector2 Center { get; private set; }
        public float LeftUV { get; private set; }
        public float RightUV { get; private set; }
        public float TopUV { get; private set; }
        public float BottomUV { get; private set; }

        public MTexture()
        {
        }

        public MTexture(VirtualTexture texture)
        {
            Texture = texture;
            AtlasPath = null;
            ClipRect = new Rectangle(0, 0, Texture.Width, Texture.Height);
            DrawOffset = Vector2.Zero;
            Width = ClipRect.Width;
            Height = ClipRect.Height;
            SetUtil();
        }

        public MTexture(MTexture parent, int x, int y, int width, int height)
        {
            Texture = parent.Texture;
            AtlasPath = null;

            ClipRect = parent.GetRelativeRect(x, y, width, height);
            DrawOffset = new Vector2(-Math.Min(x - parent.DrawOffset.X, 0), -Math.Min(y - parent.DrawOffset.Y, 0));
            Width = width;
            Height = height;
            SetUtil();
        }

        public MTexture(MTexture parent, Rectangle clipRect)
            : this(parent, clipRect.X, clipRect.Y, clipRect.Width, clipRect.Height)
        {
        }

        public MTexture(MTexture parent, string atlasPath, Rectangle clipRect, Vector2 drawOffset, int width, int height)
        {
            Texture = parent.Texture;
            AtlasPath = atlasPath;

            ClipRect = parent.GetRelativeRect(clipRect);
            DrawOffset = drawOffset;
            Width = width;
            Height = height;
            SetUtil();
        }

        public MTexture(MTexture parent, string atlasPath, Rectangle clipRect)
            : this(parent, clipRect)
        {
            AtlasPath = atlasPath;
        }

        public MTexture(VirtualTexture texture, Vector2 drawOffset, int frameWidth, int frameHeight)
        {
            Texture = texture;

            ClipRect = new Rectangle(0, 0, texture.Width, texture.Height);
            DrawOffset = drawOffset;
            Width = frameWidth;
            Height = frameHeight;
            SetUtil();
        }

        private void SetUtil()
        {
            Center = new Vector2(Width, Height) * 0.5f;
            LeftUV = ClipRect.Left / (float)Texture.Width;
            RightUV = ClipRect.Right / (float)Texture.Width;
            TopUV = ClipRect.Top / (float)Texture.Height;
            BottomUV = ClipRect.Bottom / (float)Texture.Height;
        }

        public void Unload()
        {
            Texture.Dispose();
            Texture = null;
        }

        public MTexture GetSubtexture(int x, int y, int width, int height, MTexture applyTo = null)
        {
            if (applyTo == null)
                return new MTexture(this, x, y, width, height);
            else
            {
                applyTo.Texture = Texture;
                applyTo.AtlasPath = null;

                applyTo.ClipRect = GetRelativeRect(x, y, width, height);
                applyTo.DrawOffset = new Vector2(-Math.Min(x - DrawOffset.X, 0), -Math.Min(y - DrawOffset.Y, 0));
                applyTo.Width = width;
                applyTo.Height = height;
                applyTo.SetUtil();

                return applyTo;
            }
        }

        public MTexture GetSubtexture(Rectangle rect)
        {
            return new MTexture(this, rect);
        }

        public override string ToString()
        {
            if (AtlasPath != null)
                return AtlasPath;
            else if (Texture.Path != null)
                return Texture.Path;
            else
                return "Texture [" + Texture.Width + ", " + Texture.Height + "]";
        }

        public Rectangle GetRelativeRect(Rectangle rect)
        {
            return GetRelativeRect(rect.X, rect.Y, rect.Width, rect.Height);
        }

        public Rectangle GetRelativeRect(int x, int y, int width, int height)
        {
            int atX = (int)(ClipRect.X - DrawOffset.X + x);
            int atY = (int)(ClipRect.Y - DrawOffset.Y + y);

            int rX = (int)MathHelper.Clamp(atX, ClipRect.Left, ClipRect.Right);
            int rY = (int)MathHelper.Clamp(atY, ClipRect.Top, ClipRect.Bottom);
            int rW = Math.Max(0, Math.Min(atX + width, ClipRect.Right) - rX);
            int rH = Math.Max(0, Math.Min(atY + height, ClipRect.Bottom) - rY);

            return new Rectangle(rX, rY, rW, rH);
        }

        public int TotalPixels
        {
            get { return Width * Height; }
        }


        #region Draw

        public void Draw(Vector2 position)
        {
            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, ClipRect, Color.White, 0, -DrawOffset, 1f, SpriteEffects.None, 0);
        }

        public void Draw(Vector2 position, Vector2 origin)
        {
            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, ClipRect, Color.White, 0, origin - DrawOffset, 1f, SpriteEffects.None, 0);
        }

        public void Draw(Vector2 position, Vector2 origin, Color color)
        {
            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, ClipRect, color, 0, origin - DrawOffset, 1f, SpriteEffects.None, 0);
        }

        public void Draw(Vector2 position, Vector2 origin, Color color, float scale)
        {
            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, ClipRect, color, 0, origin - DrawOffset, scale, SpriteEffects.None, 0);
        }

        public void Draw(Vector2 position, Vector2 origin, Color color, float scale, float rotation)
        {
            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, ClipRect, color, rotation, origin - DrawOffset, scale, SpriteEffects.None, 0);
        }

        public void Draw(Vector2 position, Vector2 origin, Color color, float scale, float rotation, SpriteEffects flip)
        {
            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, ClipRect, color, rotation, origin - DrawOffset, scale, flip, 0);
        }

        public void Draw(Vector2 position, Vector2 origin, Color color, Vector2 scale)
        {
            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, ClipRect, color, 0, origin - DrawOffset, scale, SpriteEffects.None, 0);
        }

        public void Draw(Vector2 position, Vector2 origin, Color color, Vector2 scale, float rotation)
        {
            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, ClipRect, color, rotation, origin - DrawOffset, scale, SpriteEffects.None, 0);
        }

        public void Draw(Vector2 position, Vector2 origin, Color color, Vector2 scale, float rotation, SpriteEffects flip)
        {
            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, ClipRect, color, rotation, origin - DrawOffset, scale, flip, 0);
        }

        public void Draw(Vector2 position, Vector2 origin, Color color, Vector2 scale, float rotation, Rectangle clip)
        {
            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, GetRelativeRect(clip), color, rotation, origin - DrawOffset, scale, SpriteEffects.None, 0);
        }


        #endregion

        #region Draw Centered

        public void DrawCentered(Vector2 position)
        {
            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, ClipRect, Color.White, 0, Center - DrawOffset, 1f, SpriteEffects.None, 0);
        }

        public void DrawCentered(Vector2 position, Color color)
        {
            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, ClipRect, color, 0, Center - DrawOffset, 1f, SpriteEffects.None, 0);
        }

        public void DrawCentered(Vector2 position, Color color, float scale)
        {
            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, ClipRect, color, 0, Center - DrawOffset, scale, SpriteEffects.None, 0);
        }

        public void DrawCentered(Vector2 position, Color color, float scale, float rotation)
        {
            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, ClipRect, color, rotation, Center - DrawOffset, scale, SpriteEffects.None, 0);
        }

        public void DrawCentered(Vector2 position, Color color, float scale, float rotation, SpriteEffects flip)
        {
            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, ClipRect, color, rotation, Center - DrawOffset, scale, flip, 0);
        }

        public void DrawCentered(Vector2 position, Color color, Vector2 scale)
        {
            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, ClipRect, color, 0, Center - DrawOffset, scale, SpriteEffects.None, 0);
        }

        public void DrawCentered(Vector2 position, Color color, Vector2 scale, float rotation)
        {
            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, ClipRect, color, rotation, Center - DrawOffset, scale, SpriteEffects.None, 0);
        }

        public void DrawCentered(Vector2 position, Color color, Vector2 scale, float rotation, SpriteEffects flip)
        {
            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, ClipRect, color, rotation, Center - DrawOffset, scale, flip, 0);
        }


        #endregion

        #region Draw Justified

        public void DrawJustified(Vector2 position, Vector2 justify)
        {
            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, ClipRect, Color.White, 0, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, 1f, SpriteEffects.None, 0);
        }

        public void DrawJustified(Vector2 position, Vector2 justify, Color color)
        {
            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, ClipRect, color, 0, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, 1f, SpriteEffects.None, 0);
        }

        public void DrawJustified(Vector2 position, Vector2 justify, Color color, float scale)
        {
            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, ClipRect, color, 0, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, scale, SpriteEffects.None, 0);
        }

        public void DrawJustified(Vector2 position, Vector2 justify, Color color, float scale, float rotation)
        {
            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, ClipRect, color, rotation, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, scale, SpriteEffects.None, 0);
        }

        public void DrawJustified(Vector2 position, Vector2 justify, Color color, float scale, float rotation, SpriteEffects flip)
        {
            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, ClipRect, color, rotation, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, scale, flip, 0);
        }

        public void DrawJustified(Vector2 position, Vector2 justify, Color color, Vector2 scale)
        {
            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, ClipRect, color, 0, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, scale, SpriteEffects.None, 0);
        }

        public void DrawJustified(Vector2 position, Vector2 justify, Color color, Vector2 scale, float rotation)
        {
            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, ClipRect, color, rotation, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, scale, SpriteEffects.None, 0);
        }

        public void DrawJustified(Vector2 position, Vector2 justify, Color color, Vector2 scale, float rotation, SpriteEffects flip)
        {
            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, ClipRect, color, rotation, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, scale, flip, 0);
        }


        #endregion

        #region Draw Outline

        public void DrawOutline(Vector2 position)
        {
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                    if (i != 0 || j != 0)
                        Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position + new Vector2(i, j), ClipRect, Color.Black, 0, -DrawOffset, 1f, SpriteEffects.None, 0);

            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, ClipRect, Color.White, 0, -DrawOffset, 1f, SpriteEffects.None, 0);
        }

        public void DrawOutline(Vector2 position, Vector2 origin)
        {
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                    if (i != 0 || j != 0)
                        Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position + new Vector2(i, j), ClipRect, Color.Black, 0, origin - DrawOffset, 1f, SpriteEffects.None, 0);

            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, ClipRect, Color.White, 0, origin - DrawOffset, 1f, SpriteEffects.None, 0);
        }

        public void DrawOutline(Vector2 position, Vector2 origin, Color color)
        {
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                    if (i != 0 || j != 0)
                        Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position + new Vector2(i, j), ClipRect, Color.Black, 0, origin - DrawOffset, 1f, SpriteEffects.None, 0);

            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, ClipRect, color, 0, origin - DrawOffset, 1f, SpriteEffects.None, 0);
        }

        public void DrawOutline(Vector2 position, Vector2 origin, Color color, float scale)
        {
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                    if (i != 0 || j != 0)
                        Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position + new Vector2(i, j), ClipRect, Color.Black, 0, origin - DrawOffset, scale, SpriteEffects.None, 0);

            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, ClipRect, color, 0, origin - DrawOffset, scale, SpriteEffects.None, 0);
        }

        public void DrawOutline(Vector2 position, Vector2 origin, Color color, float scale, float rotation)
        {
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                    if (i != 0 || j != 0)
                        Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position + new Vector2(i, j), ClipRect, Color.Black, rotation, origin - DrawOffset, scale, SpriteEffects.None, 0);

            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, ClipRect, color, rotation, origin - DrawOffset, scale, SpriteEffects.None, 0);
        }

        public void DrawOutline(Vector2 position, Vector2 origin, Color color, float scale, float rotation, SpriteEffects flip)
        {
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                    if (i != 0 || j != 0)
                        Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position + new Vector2(i, j), ClipRect, Color.Black, rotation, origin - DrawOffset, scale, flip, 0);

            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, ClipRect, color, rotation, origin - DrawOffset, scale, flip, 0);
        }

        public void DrawOutline(Vector2 position, Vector2 origin, Color color, Vector2 scale)
        {
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                    if (i != 0 || j != 0)
                        Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position + new Vector2(i, j), ClipRect, Color.Black, 0, origin - DrawOffset, scale, SpriteEffects.None, 0);

            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, ClipRect, color, 0, origin - DrawOffset, scale, SpriteEffects.None, 0);
        }

        public void DrawOutline(Vector2 position, Vector2 origin, Color color, Vector2 scale, float rotation)
        {
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                    if (i != 0 || j != 0)
                        Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position + new Vector2(i, j), ClipRect, Color.Black, rotation, origin - DrawOffset, scale, SpriteEffects.None, 0);

            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, ClipRect, color, rotation, origin - DrawOffset, scale, SpriteEffects.None, 0);
        }

        public void DrawOutline(Vector2 position, Vector2 origin, Color color, Vector2 scale, float rotation, SpriteEffects flip)
        {
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                    if (i != 0 || j != 0)
                        Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position + new Vector2(i, j), ClipRect, Color.Black, rotation, origin - DrawOffset, scale, flip, 0);

            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, ClipRect, color, rotation, origin - DrawOffset, scale, flip, 0);
        }


        #endregion

        #region Draw Outline Centered

        public void DrawOutlineCentered(Vector2 position)
        {
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                    if (i != 0 || j != 0)
                        Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position + new Vector2(i, j), ClipRect, Color.Black, 0, Center - DrawOffset, 1f, SpriteEffects.None, 0);

            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, ClipRect, Color.White, 0, Center - DrawOffset, 1f, SpriteEffects.None, 0);
        }

        public void DrawOutlineCentered(Vector2 position, Color color)
        {
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                    if (i != 0 || j != 0)
                        Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position + new Vector2(i, j), ClipRect, Color.Black, 0, Center - DrawOffset, 1f, SpriteEffects.None, 0);

            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, ClipRect, color, 0, Center - DrawOffset, 1f, SpriteEffects.None, 0);
        }

        public void DrawOutlineCentered(Vector2 position, Color color, float scale)
        {
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                    if (i != 0 || j != 0)
                        Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position + new Vector2(i, j), ClipRect, Color.Black, 0, Center - DrawOffset, scale, SpriteEffects.None, 0);

            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, ClipRect, color, 0, Center - DrawOffset, scale, SpriteEffects.None, 0);
        }

        public void DrawOutlineCentered(Vector2 position, Color color, float scale, float rotation)
        {
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                    if (i != 0 || j != 0)
                        Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position + new Vector2(i, j), ClipRect, Color.Black, rotation, Center - DrawOffset, scale, SpriteEffects.None, 0);

            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, ClipRect, color, rotation, Center - DrawOffset, scale, SpriteEffects.None, 0);
        }

        public void DrawOutlineCentered(Vector2 position, Color color, float scale, float rotation, SpriteEffects flip)
        {
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                    if (i != 0 || j != 0)
                        Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position + new Vector2(i, j), ClipRect, Color.Black, rotation, Center - DrawOffset, scale, flip, 0);

            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, ClipRect, color, rotation, Center - DrawOffset, scale, flip, 0);
        }

        public void DrawOutlineCentered(Vector2 position, Color color, Vector2 scale)
        {
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                    if (i != 0 || j != 0)
                        Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position + new Vector2(i, j), ClipRect, Color.Black, 0, Center - DrawOffset, scale, SpriteEffects.None, 0);

            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, ClipRect, color, 0, Center - DrawOffset, scale, SpriteEffects.None, 0);
        }

        public void DrawOutlineCentered(Vector2 position, Color color, Vector2 scale, float rotation)
        {
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                    if (i != 0 || j != 0)
                        Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position + new Vector2(i, j), ClipRect, Color.Black, rotation, Center - DrawOffset, scale, SpriteEffects.None, 0);

            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, ClipRect, color, rotation, Center - DrawOffset, scale, SpriteEffects.None, 0);
        }

        public void DrawOutlineCentered(Vector2 position, Color color, Vector2 scale, float rotation, SpriteEffects flip)
        {
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                    if (i != 0 || j != 0)
                        Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position + new Vector2(i, j), ClipRect, Color.Black, rotation, Center - DrawOffset, scale, flip, 0);

            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, ClipRect, color, rotation, Center - DrawOffset, scale, flip, 0);
        }


        #endregion

        #region Draw Outline Justified

        public void DrawOutlineJustified(Vector2 position, Vector2 justify)
        {
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                    if (i != 0 || j != 0)
                        Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position + new Vector2(i, j), ClipRect, Color.Black, 0, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, 1f, SpriteEffects.None, 0);

            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, ClipRect, Color.White, 0, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, 1f, SpriteEffects.None, 0);
        }

        public void DrawOutlineJustified(Vector2 position, Vector2 justify, Color color)
        {
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                    if (i != 0 || j != 0)
                        Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position + new Vector2(i, j), ClipRect, Color.Black, 0, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, 1f, SpriteEffects.None, 0);

            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, ClipRect, color, 0, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, 1f, SpriteEffects.None, 0);
        }

        public void DrawOutlineJustified(Vector2 position, Vector2 justify, Color color, float scale)
        {
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                    if (i != 0 || j != 0)
                        Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position + new Vector2(i, j), ClipRect, Color.Black, 0, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, scale, SpriteEffects.None, 0);

            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, ClipRect, color, 0, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, scale, SpriteEffects.None, 0);
        }

        public void DrawOutlineJustified(Vector2 position, Vector2 justify, Color color, float scale, float rotation)
        {
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                    if (i != 0 || j != 0)
                        Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position + new Vector2(i, j), ClipRect, Color.Black, rotation, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, scale, SpriteEffects.None, 0);

            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, ClipRect, color, rotation, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, scale, SpriteEffects.None, 0);
        }

        public void DrawOutlineJustified(Vector2 position, Vector2 justify, Color color, float scale, float rotation, SpriteEffects flip)
        {
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                    if (i != 0 || j != 0)
                        Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position + new Vector2(i, j), ClipRect, Color.Black, rotation, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, scale, flip, 0);

            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, ClipRect, color, rotation, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, scale, flip, 0);
        }

        public void DrawOutlineJustified(Vector2 position, Vector2 justify, Color color, Vector2 scale)
        {
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                    if (i != 0 || j != 0)
                        Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position + new Vector2(i, j), ClipRect, Color.Black, 0, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, scale, SpriteEffects.None, 0);

            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, ClipRect, color, 0, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, scale, SpriteEffects.None, 0);
        }

        public void DrawOutlineJustified(Vector2 position, Vector2 justify, Color color, Vector2 scale, float rotation)
        {
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                    if (i != 0 || j != 0)
                        Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position + new Vector2(i, j), ClipRect, Color.Black, rotation, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, scale, SpriteEffects.None, 0);

            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, ClipRect, color, rotation, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, scale, SpriteEffects.None, 0);
        }

        public void DrawOutlineJustified(Vector2 position, Vector2 justify, Color color, Vector2 scale, float rotation, SpriteEffects flip)
        {
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                    if (i != 0 || j != 0)
                        Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position + new Vector2(i, j), ClipRect, Color.Black, rotation, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, scale, flip, 0);

            Monocle.Draw.SpriteBatch.Draw(Texture.Texture, position, ClipRect, color, rotation, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, scale, flip, 0);
        }


        #endregion
    }
}
