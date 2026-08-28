using System;
using System.Collections.Generic;
using System.Xml;

namespace Monocle
{
    // Port fiel (celeste_source/Celeste/Monocle/SpriteBank.cs). Era a unica peca da cadeia
    // de sprites que faltava: SpriteData, SpriteDataSource e Sprite.CreateClone/CloneInto
    // ja estavam portados.
    //
    // NOTE: Create/CreateOn viraram virtual (no original nao sao). Motivo: os harnesses
    // headless rodam o Player sem GraphicsDevice, logo sem atlas e sem banco — e o
    // Player.ctor chama GFX.SpriteBank.Create no construtor. A FallbackSpriteBank do jogo
    // sobrescreve os dois p/ devolver sprites tolerantes. No Celeste isso nao acontece
    // porque o jogo nunca constroi um Player sem conteudo carregado.
    public class SpriteBank
    {
        public Atlas Atlas;
        public XmlDocument XML;
        public Dictionary<string, SpriteData> SpriteData;

        public SpriteBank(Atlas atlas, XmlDocument xml)
        {
            Atlas = atlas;
            XML = xml;
            SpriteData = new Dictionary<string, SpriteData>(StringComparer.OrdinalIgnoreCase);

            Dictionary<string, XmlElement> elements = new Dictionary<string, XmlElement>();
            foreach (object obj in XML["Sprites"].ChildNodes)
            {
                XmlElement xmlElement = obj as XmlElement;
                if (xmlElement == null)
                    continue;

                elements.Add(xmlElement.Name, xmlElement);
                if (SpriteData.ContainsKey(xmlElement.Name))
                    throw new Exception("Duplicate sprite name in SpriteData: '" + xmlElement.Name + "'!");

                SpriteData data = SpriteData[xmlElement.Name] = new SpriteData(Atlas);
                if (xmlElement.HasAttr("copy"))
                    data.Add(elements[xmlElement.Attr("copy")], xmlElement.Attr("path"));
                data.Add(xmlElement, null);
            }
        }

        public SpriteBank(Atlas atlas, string xmlPath)
            : this(atlas, Calc.LoadContentXML(xmlPath))
        {
        }

        public bool Has(string id)
        {
            return SpriteData.ContainsKey(id);
        }

        public virtual Sprite Create(string id)
        {
            if (SpriteData.ContainsKey(id))
                return SpriteData[id].Create();
            throw new Exception("Missing animation name in SpriteData: '" + id + "'!");
        }

        public virtual Sprite CreateOn(Sprite sprite, string id)
        {
            if (SpriteData.ContainsKey(id))
                return SpriteData[id].CreateOn(sprite);
            throw new Exception("Missing animation name in SpriteData: '" + id + "'!");
        }
    }
}
