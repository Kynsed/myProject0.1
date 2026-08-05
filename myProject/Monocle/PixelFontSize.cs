using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;

namespace Monocle
{
    public class PixelFontSize
    {
        public List<MTexture> Textures;
        public Dictionary<int, PixelFontCharacter> Characters;
        public int LineHeight;
        public float Size;
        public bool Outline;

        private StringBuilder temp = new StringBuilder();

        public string AutoNewline(string text, int width)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            temp.Clear();

            string[] words = Regex.Split(text, "(\\s)");
            float lineWidth = 0f;

            foreach (string word in words)
            {
                float wordWidth = Measure(word).X;
                if (wordWidth + lineWidth > width)
                {
                    temp.Append('\n');
                    lineWidth = 0;

                    if (word.Equals(" "))
                        continue;
                }

                // this word is longer than the max-width, split where ever we can
                if (wordWidth > width)
                {
                    int start = 0;
                    for (int i = 1; i < word.Length; i++)
                        if (i - start > 1 && Measure(word.Substring(start, i - start - 1)).X > width)
                        {
                            temp.Append(word.Substring(start, i - start - 1));
                            temp.Append('\n');
                            start = i - 1;
                        }

                    string remaining = word.Substring(start, word.Length - start);
                    temp.Append(remaining);
                    lineWidth += Measure(remaining).X;
                }
                // normal word, add it
                else
                {
                    lineWidth += wordWidth;
                    temp.Append(word);
                }
            }

            return temp.ToString();
        }

        public PixelFontCharacter Get(int id)
        {
            PixelFontCharacter val;
            if (Characters.TryGetValue(id, out val))
                return val;
            else
                return null;
        }

        public Vector2 Measure(char text)
        {
            PixelFontCharacter c;
            if (Characters.TryGetValue(text, out c))
                return new Vector2(c.XAdvance, LineHeight);
            else
                return Vector2.Zero;
        }

        public Vector2 Measure(string text)
        {
            if (string.IsNullOrEmpty(text))
                return Vector2.Zero;

            Vector2 size = new Vector2(0, LineHeight);
            float currentLineWidth = 0f;

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    size.Y += LineHeight;
                    if (currentLineWidth > size.X)
                        size.X = currentLineWidth;
                    currentLineWidth = 0f;
                }
                else
                {
                    PixelFontCharacter c;
                    if (Characters.TryGetValue(text[i], out c))
                    {
                        currentLineWidth += c.XAdvance;

                        int kerning;
                        if (i < text.Length - 1 && c.Kerning.TryGetValue(text[i + 1], out kerning))
                            currentLineWidth += kerning;
                    }
                }
            }

            if (currentLineWidth > size.X)
                size.X = currentLineWidth;

            return size;
        }

        public float WidthToNextLine(string text, int start)
        {
            if (string.IsNullOrEmpty(text))
                return 0f;

            float currentLineWidth = 0f;

            for (int i = start, j = text.Length; i < j; i++)
            {
                if (text[i] == '\n')
                    break;

                PixelFontCharacter c;
                if (Characters.TryGetValue(text[i], out c))
                {
                    currentLineWidth += c.XAdvance;

                    int kerning;
                    if (i < j - 1 && c.Kerning.TryGetValue(text[i + 1], out kerning))
                        currentLineWidth += kerning;
                }
            }

            return currentLineWidth;
        }

        public float HeightOf(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0f;

            int lines = 1;
            if (text.IndexOf('\n') >= 0)
                for (int i = 0; i < text.Length; i++)
                    if (text[i] == '\n')
                        lines++;
            return lines * LineHeight;
        }

        public void Draw(char character, Vector2 position, Vector2 justify, Vector2 scale, Color color)
        {
            if (char.IsWhiteSpace(character))
                return;

            PixelFontCharacter c;
            if (Characters.TryGetValue(character, out c))
            {
                Vector2 measure = Measure(character);
                Vector2 justified = new Vector2(measure.X * justify.X, measure.Y * justify.Y);
                Vector2 pos = position + (new Vector2(c.XOffset, c.YOffset) - justified) * scale;
                c.Texture.Draw(pos.Floored(), Vector2.Zero, color, scale);
            }
        }

        public void Draw(string text, Vector2 position, Vector2 justify, Vector2 scale, Color color, float edgeDepth, Color edgeColor, float stroke, Color strokeColor)
        {
            if (string.IsNullOrEmpty(text))
                return;

            Vector2 offset = Vector2.Zero;
            Vector2 justified = new Vector2(justify.X != 0 ? WidthToNextLine(text, 0) : 0, HeightOf(text) * justify.Y);
            justified.X *= justify.X;

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    offset.X = 0;
                    offset.Y += LineHeight;
                    if (justify.X != 0)
                        justified.X = WidthToNextLine(text, i + 1) * justify.X;
                    continue;
                }

                PixelFontCharacter c;
                if (Characters.TryGetValue(text[i], out c))
                {
                    Vector2 pos = position + (offset + new Vector2(c.XOffset, c.YOffset) - justified) * scale;

                    // draw stroke
                    if (stroke > 0 && !Outline)
                    {
                        if (edgeDepth > 0)
                        {
                            c.Texture.Draw(pos + new Vector2(0, -stroke), Vector2.Zero, strokeColor, scale);
                            for (float y = -stroke; y < edgeDepth + stroke; y += stroke)
                            {
                                c.Texture.Draw(pos + new Vector2(-stroke, y), Vector2.Zero, strokeColor, scale);
                                c.Texture.Draw(pos + new Vector2(stroke, y), Vector2.Zero, strokeColor, scale);
                            }
                            c.Texture.Draw(pos + new Vector2(-stroke, edgeDepth + stroke), Vector2.Zero, strokeColor, scale);
                            c.Texture.Draw(pos + new Vector2(0, edgeDepth + stroke), Vector2.Zero, strokeColor, scale);
                            c.Texture.Draw(pos + new Vector2(stroke, edgeDepth + stroke), Vector2.Zero, strokeColor, scale);
                        }
                        else
                        {
                            c.Texture.Draw(pos + new Vector2(-1, -1) * stroke, Vector2.Zero, strokeColor, scale);
                            c.Texture.Draw(pos + new Vector2(0, -1) * stroke, Vector2.Zero, strokeColor, scale);
                            c.Texture.Draw(pos + new Vector2(1, -1) * stroke, Vector2.Zero, strokeColor, scale);
                            c.Texture.Draw(pos + new Vector2(-1, 0) * stroke, Vector2.Zero, strokeColor, scale);
                            c.Texture.Draw(pos + new Vector2(1, 0) * stroke, Vector2.Zero, strokeColor, scale);
                            c.Texture.Draw(pos + new Vector2(-1, 1) * stroke, Vector2.Zero, strokeColor, scale);
                            c.Texture.Draw(pos + new Vector2(0, 1) * stroke, Vector2.Zero, strokeColor, scale);
                            c.Texture.Draw(pos + new Vector2(1, 1) * stroke, Vector2.Zero, strokeColor, scale);
                        }
                    }

                    // draw edge
                    if (edgeDepth > 0)
                        c.Texture.Draw(pos + Vector2.UnitY * edgeDepth, Vector2.Zero, edgeColor, scale);

                    // draw normal
                    c.Texture.Draw(pos, Vector2.Zero, color, scale);

                    offset.X += c.XAdvance;

                    int kerning;
                    if (i < text.Length - 1 && c.Kerning.TryGetValue(text[i + 1], out kerning))
                        offset.X += kerning;
                }
            }
        }

        public void Draw(string text, Vector2 position, Color color)
        {
            Draw(text, position, Vector2.Zero, Vector2.One, color, 0f, Color.Transparent, 0f, Color.Transparent);
        }

        public void Draw(string text, Vector2 position, Vector2 justify, Vector2 scale, Color color)
        {
            Draw(text, position, justify, scale, color, 0f, Color.Transparent, 0f, Color.Transparent);
        }

        public void DrawOutline(string text, Vector2 position, Vector2 justify, Vector2 scale, Color color, float stroke, Color strokeColor)
        {
            Draw(text, position, justify, scale, color, 0f, Color.Transparent, stroke, strokeColor);
        }

        public void DrawEdgeOutline(string text, Vector2 position, Vector2 justify, Vector2 scale, Color color, float edgeDepth, Color edgeColor, float stroke = 0f, Color strokeColor = default(Color))
        {
            Draw(text, position, justify, scale, color, edgeDepth, edgeColor, stroke, strokeColor);
        }
    }
}
