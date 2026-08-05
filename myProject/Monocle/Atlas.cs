using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Microsoft.Xna.Framework;

namespace Monocle
{
    public class Atlas
    {
        public enum AtlasDataFormat
        {
            TexturePacker_Sparrow,
            CrunchXml,
            CrunchBinary,
            CrunchXmlOrBinary,
            CrunchBinaryNoAtlas,
            Packer,
            PackerNoAtlas
        }

        public List<VirtualTexture> Sources;

        private Dictionary<string, MTexture> textures = new Dictionary<string, MTexture>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, List<MTexture>> orderedTexturesCache = new Dictionary<string, List<MTexture>>();
        private Dictionary<string, string> links = new Dictionary<string, string>();

        #region Loading

        public static Atlas FromAtlas(string path, AtlasDataFormat format)
        {
            Atlas atlas = new Atlas();
            atlas.Sources = new List<VirtualTexture>();
            ReadAtlasData(atlas, path, format);
            return atlas;
        }

        private static void ReadAtlasData(Atlas atlas, string path, AtlasDataFormat format)
        {
            switch (format)
            {
                case AtlasDataFormat.TexturePacker_Sparrow:
                    {
                        XmlElement xml = Calc.LoadContentXML(path)["TextureAtlas"];
                        string imagePath = xml.Attr("imagePath", "");
                        VirtualTexture texture = VirtualContent.CreateTexture(Path.Combine(Path.GetDirectoryName(path), imagePath));
                        MTexture parent = new MTexture(texture);
                        atlas.Sources.Add(texture);

                        foreach (XmlElement sub in xml.GetElementsByTagName("SubTexture"))
                        {
                            string name = sub.Attr("name");
                            Rectangle clip = sub.Rect();
                            if (sub.HasAttr("frameX"))
                                atlas.textures[name] = new MTexture(parent, name, clip,
                                    new Vector2(-sub.AttrInt("frameX"), -sub.AttrInt("frameY")),
                                    sub.AttrInt("frameWidth"), sub.AttrInt("frameHeight"));
                            else
                                atlas.textures[name] = new MTexture(parent, name, clip);
                        }
                    }
                    break;

                case AtlasDataFormat.CrunchXml:
                    {
                        foreach (XmlElement page in Calc.LoadContentXML(path)["atlas"])
                        {
                            string pageName = page.Attr("n", "");
                            VirtualTexture texture = VirtualContent.CreateTexture(Path.Combine(Path.GetDirectoryName(path), pageName + ".png"));
                            MTexture parent = new MTexture(texture);
                            atlas.Sources.Add(texture);

                            foreach (XmlElement sub in page)
                            {
                                string name = sub.Attr("n");
                                Rectangle clip = new Rectangle(sub.AttrInt("x"), sub.AttrInt("y"), sub.AttrInt("w"), sub.AttrInt("h"));
                                if (sub.HasAttr("fx"))
                                    atlas.textures[name] = new MTexture(parent, name, clip,
                                        new Vector2(-sub.AttrInt("fx"), -sub.AttrInt("fy")),
                                        sub.AttrInt("fw"), sub.AttrInt("fh"));
                                else
                                    atlas.textures[name] = new MTexture(parent, name, clip);
                            }
                        }
                    }
                    break;

                case AtlasDataFormat.CrunchBinary:
                    {
                        using (FileStream stream = File.OpenRead(Path.Combine(Engine.ContentDirectory, path)))
                        {
                            BinaryReader reader = new BinaryReader(stream);
                            short pages = reader.ReadInt16();
                            for (int p = 0; p < pages; p++)
                            {
                                string pageName = reader.ReadNullTerminatedString();
                                VirtualTexture texture = VirtualContent.CreateTexture(Path.Combine(Path.GetDirectoryName(path), pageName + ".png"));
                                atlas.Sources.Add(texture);
                                MTexture parent = new MTexture(texture);

                                short subs = reader.ReadInt16();
                                for (int s = 0; s < subs; s++)
                                {
                                    string name = reader.ReadNullTerminatedString();
                                    short x = reader.ReadInt16();
                                    short y = reader.ReadInt16();
                                    short width = reader.ReadInt16();
                                    short height = reader.ReadInt16();
                                    short frameX = reader.ReadInt16();
                                    short frameY = reader.ReadInt16();
                                    short frameWidth = reader.ReadInt16();
                                    short frameHeight = reader.ReadInt16();
                                    atlas.textures[name] = new MTexture(parent, name,
                                        new Rectangle(x, y, width, height),
                                        new Vector2(-frameX, -frameY),
                                        frameWidth, frameHeight);
                                }
                            }
                        }
                    }
                    break;

                case AtlasDataFormat.CrunchBinaryNoAtlas:
                    {
                        using (FileStream stream = File.OpenRead(Path.Combine(Engine.ContentDirectory, path + ".bin")))
                        {
                            BinaryReader reader = new BinaryReader(stream);
                            short folders = reader.ReadInt16();
                            for (int f = 0; f < folders; f++)
                            {
                                string folderName = reader.ReadNullTerminatedString();
                                string folderPath = Path.Combine(Path.GetDirectoryName(path), folderName);

                                short subs = reader.ReadInt16();
                                for (int s = 0; s < subs; s++)
                                {
                                    string name = reader.ReadNullTerminatedString();
                                    reader.ReadInt16();
                                    reader.ReadInt16();
                                    reader.ReadInt16();
                                    reader.ReadInt16();
                                    short frameX = reader.ReadInt16();
                                    short frameY = reader.ReadInt16();
                                    short frameWidth = reader.ReadInt16();
                                    short frameHeight = reader.ReadInt16();
                                    VirtualTexture texture = VirtualContent.CreateTexture(Path.Combine(folderPath, name + ".png"));
                                    atlas.Sources.Add(texture);
                                    atlas.textures[name] = new MTexture(texture, new Vector2(-frameX, -frameY), frameWidth, frameHeight);
                                }
                            }
                        }
                    }
                    break;

                case AtlasDataFormat.Packer:
                    {
                        using (FileStream stream = File.OpenRead(Path.Combine(Engine.ContentDirectory, path + ".meta")))
                        {
                            BinaryReader reader = new BinaryReader(stream);
                            reader.ReadInt32();
                            reader.ReadString();
                            reader.ReadInt32();

                            short pages = reader.ReadInt16();
                            for (int p = 0; p < pages; p++)
                            {
                                string pageName = reader.ReadString();
                                VirtualTexture texture = VirtualContent.CreateTexture(Path.Combine(Path.GetDirectoryName(path), pageName + ".data"));
                                atlas.Sources.Add(texture);
                                MTexture parent = new MTexture(texture);

                                short subs = reader.ReadInt16();
                                for (int s = 0; s < subs; s++)
                                {
                                    string name = reader.ReadString().Replace('\\', '/');
                                    short x = reader.ReadInt16();
                                    short y = reader.ReadInt16();
                                    short width = reader.ReadInt16();
                                    short height = reader.ReadInt16();
                                    short frameX = reader.ReadInt16();
                                    short frameY = reader.ReadInt16();
                                    short frameWidth = reader.ReadInt16();
                                    short frameHeight = reader.ReadInt16();
                                    atlas.textures[name] = new MTexture(parent, name,
                                        new Rectangle(x, y, width, height),
                                        new Vector2(-frameX, -frameY),
                                        frameWidth, frameHeight);
                                }
                            }

                            if (stream.Position < stream.Length && reader.ReadString() == "LINKS")
                            {
                                short count = reader.ReadInt16();
                                for (int i = 0; i < count; i++)
                                {
                                    string key = reader.ReadString();
                                    string value = reader.ReadString();
                                    atlas.links.Add(key, value);
                                }
                            }
                        }
                    }
                    break;

                case AtlasDataFormat.PackerNoAtlas:
                    {
                        using (FileStream stream = File.OpenRead(Path.Combine(Engine.ContentDirectory, path + ".meta")))
                        {
                            BinaryReader reader = new BinaryReader(stream);
                            reader.ReadInt32();
                            reader.ReadString();
                            reader.ReadInt32();

                            short folders = reader.ReadInt16();
                            for (int f = 0; f < folders; f++)
                            {
                                string folderName = reader.ReadString();
                                string folderPath = Path.Combine(Path.GetDirectoryName(path), folderName);

                                short subs = reader.ReadInt16();
                                for (int s = 0; s < subs; s++)
                                {
                                    string name = reader.ReadString().Replace('\\', '/');
                                    reader.ReadInt16();
                                    reader.ReadInt16();
                                    reader.ReadInt16();
                                    reader.ReadInt16();
                                    short frameX = reader.ReadInt16();
                                    short frameY = reader.ReadInt16();
                                    short frameWidth = reader.ReadInt16();
                                    short frameHeight = reader.ReadInt16();
                                    VirtualTexture texture = VirtualContent.CreateTexture(Path.Combine(folderPath, name + ".data"));
                                    atlas.Sources.Add(texture);
                                    atlas.textures[name] = new MTexture(texture, new Vector2(-frameX, -frameY), frameWidth, frameHeight);
                                    atlas.textures[name].AtlasPath = name;
                                }
                            }

                            if (stream.Position < stream.Length && reader.ReadString() == "LINKS")
                            {
                                short count = reader.ReadInt16();
                                for (int i = 0; i < count; i++)
                                {
                                    string key = reader.ReadString();
                                    string value = reader.ReadString();
                                    atlas.links.Add(key, value);
                                }
                            }
                        }
                    }
                    break;

                case AtlasDataFormat.CrunchXmlOrBinary:
                    {
                        if (File.Exists(Path.Combine(Engine.ContentDirectory, path + ".bin")))
                            ReadAtlasData(atlas, path + ".bin", AtlasDataFormat.CrunchBinary);
                        else
                            ReadAtlasData(atlas, path + ".xml", AtlasDataFormat.CrunchXml);
                    }
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        public static Atlas FromMultiAtlas(string rootPath, string[] dataPath, AtlasDataFormat format)
        {
            Atlas atlas = new Atlas();
            atlas.Sources = new List<VirtualTexture>();
            for (int i = 0; i < dataPath.Length; i++)
                ReadAtlasData(atlas, Path.Combine(rootPath, dataPath[i]), format);
            return atlas;
        }

        public static Atlas FromMultiAtlas(string rootPath, string filename, AtlasDataFormat format)
        {
            Atlas atlas = new Atlas();
            atlas.Sources = new List<VirtualTexture>();

            int index = 0;
            while (true)
            {
                string dataPath = Path.Combine(rootPath, filename + index.ToString() + ".xml");
                if (!File.Exists(Path.Combine(Engine.ContentDirectory, dataPath)))
                    break;

                ReadAtlasData(atlas, dataPath, format);
                index++;
            }

            return atlas;
        }

        public static Atlas FromDirectory(string path)
        {
            Atlas atlas = new Atlas();
            atlas.Sources = new List<VirtualTexture>();

            string contentDirectory = Engine.ContentDirectory;
            int contentLength = contentDirectory.Length;
            string fullPath = Path.Combine(contentDirectory, path);
            int fullLength = fullPath.Length;

            foreach (string file in Directory.GetFiles(fullPath, "*", SearchOption.AllDirectories))
            {
                string extension = Path.GetExtension(file);
                if (extension != ".png" && extension != ".xnb")
                    continue;

                VirtualTexture texture = VirtualContent.CreateTexture(file.Substring(contentLength + 1));
                atlas.Sources.Add(texture);

                string name = file.Substring(fullLength + 1);
                name = name.Substring(0, name.Length - 4);
                name = name.Replace('\\', '/');
                atlas.textures.Add(name, new MTexture(texture));
            }

            return atlas;
        }

        #endregion

        public MTexture this[string id]
        {
            get { return textures[id]; }
            set { textures[id] = value; }
        }

        public bool Has(string id)
        {
            return textures.ContainsKey(id);
        }

        public MTexture GetOrDefault(string id, MTexture defaultTexture)
        {
            if (string.IsNullOrEmpty(id) || !Has(id))
                return defaultTexture;
            return textures[id];
        }

        public List<MTexture> GetAtlasSubtextures(string key)
        {
            List<MTexture> list;
            if (!orderedTexturesCache.TryGetValue(key, out list))
            {
                list = new List<MTexture>();

                int index = 0;
                while (true)
                {
                    MTexture sub = GetAtlasSubtextureFromAtlasAt(key, index);
                    if (sub == null)
                        break;

                    list.Add(sub);
                    index++;
                }

                orderedTexturesCache.Add(key, list);
            }

            return list;
        }

        private MTexture GetAtlasSubtextureFromCacheAt(string key, int index)
        {
            return orderedTexturesCache[key][index];
        }

        private MTexture GetAtlasSubtextureFromAtlasAt(string key, int index)
        {
            if (index == 0 && textures.ContainsKey(key))
                return textures[key];

            string indexString = index.ToString();
            int startLength = indexString.Length;
            while (indexString.Length < startLength + 6)
            {
                MTexture result;
                if (textures.TryGetValue(key + indexString, out result))
                    return result;
                indexString = "0" + indexString;
            }

            return null;
        }

        public MTexture GetAtlasSubtexturesAt(string key, int index)
        {
            List<MTexture> list;
            if (orderedTexturesCache.TryGetValue(key, out list))
                return list[index];
            return GetAtlasSubtextureFromAtlasAt(key, index);
        }

        public MTexture GetLinkedTexture(string key)
        {
            string linked;
            MTexture result;
            if (key != null && links.TryGetValue(key, out linked) && textures.TryGetValue(linked, out result))
                return result;
            return null;
        }

        public void Dispose()
        {
            foreach (VirtualTexture texture in Sources)
                texture.Dispose();
            Sources.Clear();
            textures.Clear();
        }
    }
}
