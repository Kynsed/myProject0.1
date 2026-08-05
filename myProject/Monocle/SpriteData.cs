using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;

namespace Monocle
{
    public class SpriteData
    {
        public List<SpriteDataSource> Sources = new List<SpriteDataSource>();
        public Sprite Sprite;
        public Atlas Atlas;

        public SpriteData(Atlas atlas)
        {
            Sprite = new Sprite(atlas, "");
            Atlas = atlas;
        }

        public void Add(XmlElement xml, string overridePath = null)
        {
            SpriteDataSource source = new SpriteDataSource();
            source.XML = xml;
            source.Path = source.XML.Attr("path");
            source.OverridePath = overridePath;

            string errorPrefix = "Sprite '" + source.XML.Name + "': ";

            // error checking
            {
                if (!source.XML.HasAttr("path") && string.IsNullOrEmpty(overridePath))
                    throw new Exception(errorPrefix + "'path' is missing!");

                HashSet<string> ids = new HashSet<string>();
                foreach (XmlElement anim in source.XML.GetElementsByTagName("Anim"))
                    CheckAnimXML(anim, errorPrefix, ids);
                foreach (XmlElement anim in source.XML.GetElementsByTagName("Loop"))
                    CheckAnimXML(anim, errorPrefix, ids);

                if (source.XML.HasAttr("start") && !ids.Contains(source.XML.Attr("start")))
                    throw new Exception(errorPrefix + "starting animation '" + source.XML.Attr("start") + "' is missing!");

                if (source.XML.HasChild("Justify") && source.XML.HasChild("Origin"))
                    throw new Exception(errorPrefix + "has both Origin and Justify tags!");
            }

            string spritePath = source.XML.Attr("path", "");
            float defaultDelay = source.XML.AttrFloat("delay", 0);

            // add the animations
            foreach (XmlElement anim in source.XML.GetElementsByTagName("Anim"))
            {
                Chooser<string> into = anim.HasAttr("goto") ? Chooser<string>.FromString<string>(anim.Attr("goto")) : null;

                string id = anim.Attr("id");
                string animPath = anim.Attr("path", "");
                int[] frames = Calc.ReadCSVIntWithTricks(anim.Attr("frames", ""));

                if (!string.IsNullOrEmpty(overridePath) && HasFrames(Atlas, overridePath + animPath, frames))
                    animPath = overridePath + animPath;
                else
                    animPath = spritePath + animPath;

                Sprite.Add(id, animPath, anim.AttrFloat("delay", defaultDelay), into, frames);
            }

            // add the loops
            foreach (XmlElement anim in source.XML.GetElementsByTagName("Loop"))
            {
                string id = anim.Attr("id");
                string animPath = anim.Attr("path", "");
                int[] frames = Calc.ReadCSVIntWithTricks(anim.Attr("frames", ""));

                if (!string.IsNullOrEmpty(overridePath) && HasFrames(Atlas, overridePath + animPath, frames))
                    animPath = overridePath + animPath;
                else
                    animPath = spritePath + animPath;

                Sprite.AddLoop(id, animPath, anim.AttrFloat("delay", defaultDelay), frames);
            }

            // origin
            if (source.XML.HasChild("Center"))
            {
                Sprite.CenterOrigin();
                Sprite.Justify = new Vector2(0.5f, 0.5f);
            }
            else if (source.XML.HasChild("Justify"))
            {
                Sprite.JustifyOrigin(source.XML.ChildPosition("Justify"));
                Sprite.Justify = source.XML.ChildPosition("Justify");
            }
            else if (source.XML.HasChild("Origin"))
                Sprite.Origin = source.XML.ChildPosition("Origin");

            if (source.XML.HasChild("Position"))
                Sprite.Position = source.XML.ChildPosition("Position");

            // start animation
            if (source.XML.HasAttr("start"))
                Sprite.Play(source.XML.Attr("start"), false, false);

            Sources.Add(source);
        }

        private bool HasFrames(Atlas atlas, string path, int[] frames = null)
        {
            if (frames == null || frames.Length == 0)
                return atlas.GetAtlasSubtexturesAt(path, 0) != null;

            for (int i = 0; i < frames.Length; i++)
                if (atlas.GetAtlasSubtexturesAt(path, frames[i]) == null)
                    return false;

            return true;
        }

        private void CheckAnimXML(XmlElement xml, string prefix, HashSet<string> ids)
        {
            if (!xml.HasAttr("id"))
                throw new Exception(prefix + "'id' is missing on " + xml.Name + "!");
            if (ids.Contains(xml.Attr("id")))
                throw new Exception(prefix + "multiple animations with id '" + xml.Attr("id") + "'!");
            ids.Add(xml.Attr("id"));
        }

        public Sprite Create()
        {
            return Sprite.CreateClone();
        }

        public Sprite CreateOn(Sprite sprite)
        {
            return Sprite.CloneInto(sprite);
        }
    }
}
