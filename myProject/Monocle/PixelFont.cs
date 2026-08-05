using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Microsoft.Xna.Framework;

namespace Monocle
{
    public class PixelFont
    {
        public string Face;
        public List<PixelFontSize> Sizes = new List<PixelFontSize>();

        private List<VirtualTexture> managedTextures = new List<VirtualTexture>();

        public PixelFont(string face)
        {
            Face = face;
        }

        public PixelFontSize AddFontSize(string path, Atlas atlas = null, bool outline = false)
        {
            var data = Calc.LoadXML(path)["font"];
            return AddFontSize(path, data, atlas, outline);
        }

        public PixelFontSize AddFontSize(string path, XmlElement data, Atlas atlas = null, bool outline = false)
        {
            // check if size already exists
            float size = data["info"].AttrFloat("size");
            foreach (var fontSize in Sizes)
                if (fontSize.Size == size)
                    return fontSize;

            // load textures
            List<MTexture> textures = new List<MTexture>();
            foreach (XmlElement page in data["pages"])
            {
                string file = page.Attr("file");
                string atlasPath = Path.GetFileNameWithoutExtension(file);

                if (atlas != null && atlas.Has(atlasPath))
                    textures.Add(atlas[atlasPath]);
                else
                {
                    VirtualTexture texture = VirtualContent.CreateTexture(Path.Combine(Path.GetDirectoryName(path).Substring(Engine.ContentDirectory.Length + 1), file));
                    textures.Add(new MTexture(texture));
                    managedTextures.Add(texture);
                }
            }

            // create font size
            PixelFontSize fontSizeToAdd = new PixelFontSize()
            {
                Textures = textures,
                Characters = new Dictionary<int, PixelFontCharacter>(),
                LineHeight = data["common"].AttrInt("lineHeight"),
                Size = size,
                Outline = outline
            };

            // get characters
            foreach (XmlElement character in data["chars"])
            {
                int id = character.AttrInt("id");
                int page = character.AttrInt("page", 0);
                fontSizeToAdd.Characters.Add(id, new PixelFontCharacter(id, textures[page], character));
            }

            // get kerning
            if (data["kernings"] != null)
                foreach (XmlElement kerning in data["kernings"])
                {
                    int from = kerning.AttrInt("first");
                    int to = kerning.AttrInt("second");
                    int push = kerning.AttrInt("amount");

                    PixelFontCharacter c;
                    if (fontSizeToAdd.Characters.TryGetValue(from, out c))
                        c.Kerning.Add(to, push);
                }

            // add font size
            Sizes.Add(fontSizeToAdd);
            Sizes.Sort((a, b) => Math.Sign(a.Size - b.Size));

            return fontSizeToAdd;
        }

        public PixelFontSize Get(float size)
        {
            for (int i = 0, j = Sizes.Count - 1; i < j; i++)
                if (Sizes[i].Size >= size)
                    return Sizes[i];
            return Sizes[Sizes.Count - 1];
        }

        public bool Has(float size)
        {
            for (int i = 0, j = Sizes.Count - 1; i < j; i++)
                if (Sizes[i].Size == size)
                    return true;
            return false;
        }

        public void Draw(float baseSize, char character, Vector2 position, Vector2 justify, Vector2 scale, Color color)
        {
            var fontSize = Get(baseSize * Math.Max(scale.X, scale.Y));
            scale *= baseSize / fontSize.Size;
            fontSize.Draw(character, position, justify, scale, color);
        }

        public void Draw(float baseSize, string text, Vector2 position, Vector2 justify, Vector2 scale, Color color, float edgeDepth, Color edgeColor, float stroke, Color strokeColor)
        {
            var fontSize = Get(baseSize * Math.Max(scale.X, scale.Y));
            scale *= baseSize / fontSize.Size;
            fontSize.Draw(text, position, justify, scale, color, edgeDepth, edgeColor, stroke, strokeColor);
        }

        public void Draw(float baseSize, string text, Vector2 position, Color color)
        {
            Vector2 scale = Vector2.One;
            var fontSize = Get(baseSize * Math.Max(scale.X, scale.Y));
            scale *= baseSize / fontSize.Size;
            fontSize.Draw(text, position, Vector2.Zero, Vector2.One, color, 0f, Color.Transparent, 0f, Color.Transparent);
        }

        public void Draw(float baseSize, string text, Vector2 position, Vector2 justify, Vector2 scale, Color color)
        {
            var fontSize = Get(baseSize * Math.Max(scale.X, scale.Y));
            scale *= baseSize / fontSize.Size;
            fontSize.Draw(text, position, justify, scale, color, 0f, Color.Transparent, 0f, Color.Transparent);
        }

        public void DrawOutline(float baseSize, string text, Vector2 position, Vector2 justify, Vector2 scale, Color color, float stroke, Color strokeColor)
        {
            var fontSize = Get(baseSize * Math.Max(scale.X, scale.Y));
            scale *= baseSize / fontSize.Size;
            fontSize.Draw(text, position, justify, scale, color, 0f, Color.Transparent, stroke, strokeColor);
        }

        public void DrawEdgeOutline(float baseSize, string text, Vector2 position, Vector2 justify, Vector2 scale, Color color, float edgeDepth, Color edgeColor, float stroke = 0f, Color strokeColor = default(Color))
        {
            var fontSize = Get(baseSize * Math.Max(scale.X, scale.Y));
            scale *= baseSize / fontSize.Size;
            fontSize.Draw(text, position, justify, scale, color, edgeDepth, edgeColor, stroke, strokeColor);
        }

        public void Dispose()
        {
            foreach (var texture in managedTextures)
                texture.Dispose();
            Sizes.Clear();
        }
    }
}
