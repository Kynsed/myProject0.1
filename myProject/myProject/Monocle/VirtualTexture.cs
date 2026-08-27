using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Monocle
{
    public class VirtualTexture : VirtualAsset
    {
        private const int ByteArraySize = 524288;
        private const int ByteArrayCheckSize = 524256;

        internal static readonly byte[] buffer = new byte[67108864];
        internal static readonly byte[] bytes = new byte[524288];

        public Texture2D Texture;

        private Color color;

        public string Path { get; private set; }

        public bool IsDisposed
        {
            get { return Texture == null || Texture.IsDisposed || Texture.GraphicsDevice.IsDisposed; }
        }

        internal VirtualTexture(string path)
        {
            Path = path;
            Name = path;
            Reload();
        }

        internal VirtualTexture(string name, int width, int height, Color color)
        {
            Name = name;
            Width = width;
            Height = height;
            this.color = color;
            Reload();
        }

        internal override void Unload()
        {
            if (Texture != null && !Texture.IsDisposed)
                Texture.Dispose();
            Texture = null;
        }

        internal unsafe override void Reload()
        {
            Unload();

            // solid color texture
            if (string.IsNullOrEmpty(Path))
            {
                Texture = new Texture2D(Engine.Instance.GraphicsDevice, Width, Height);
                Color[] data = new Color[Width * Height];
                fixed (Color* ptr = data)
                {
                    for (int i = 0; i < data.Length; i++)
                        ptr[i] = color;
                }
                Texture.SetData(data);
                return;
            }

            string extension = System.IO.Path.GetExtension(Path);

            // Celeste's proprietary RLE-compressed format
            if (extension == ".data")
            {
                using (FileStream stream = File.OpenRead(System.IO.Path.Combine(Engine.ContentDirectory, Path)))
                {
                    // Leitura parcial é intencional: buffer fixo de 512KB com refill.
                    // Arquivo menor que o buffer lê tudo de uma vez; maior, refila abaixo.
                    // ReadExactly NÃO serve aqui (lançaria em arquivo < 512KB).
#pragma warning disable CA2022
                    stream.Read(bytes, 0, ByteArraySize);
#pragma warning restore CA2022

                    int position = 0;
                    int width = BitConverter.ToInt32(bytes, position);
                    int height = BitConverter.ToInt32(bytes, position + 4);
                    bool hasAlpha = bytes[position + 8] == 1;
                    position += 9;

                    int totalBytes = width * height * 4;
                    int writePosition = 0;

                    fixed (byte* src = bytes)
                    fixed (byte* dest = buffer)
                    {
                        while (writePosition < totalBytes)
                        {
                            int run = src[position] * 4;

                            if (hasAlpha)
                            {
                                byte alpha = src[position + 1];
                                if (alpha > 0)
                                {
                                    dest[writePosition] = src[position + 4];
                                    dest[writePosition + 1] = src[position + 3];
                                    dest[writePosition + 2] = src[position + 2];
                                    dest[writePosition + 3] = alpha;
                                    position += 5;
                                }
                                else
                                {
                                    dest[writePosition] = 0;
                                    dest[writePosition + 1] = 0;
                                    dest[writePosition + 2] = 0;
                                    dest[writePosition + 3] = 0;
                                    position += 2;
                                }
                            }
                            else
                            {
                                dest[writePosition] = src[position + 3];
                                dest[writePosition + 1] = src[position + 2];
                                dest[writePosition + 2] = src[position + 1];
                                dest[writePosition + 3] = byte.MaxValue;
                                position += 4;
                            }

                            // repeat the color for the rest of the run
                            if (run > 4)
                            {
                                int to = writePosition + run;
                                for (int k = writePosition + 4; k < to; k += 4)
                                {
                                    dest[k] = dest[writePosition];
                                    dest[k + 1] = dest[writePosition + 1];
                                    dest[k + 2] = dest[writePosition + 2];
                                    dest[k + 3] = dest[writePosition + 3];
                                }
                            }

                            writePosition += run;

                            // refill the read buffer when running low
                            if (position > ByteArrayCheckSize)
                            {
                                int leftover = ByteArraySize - position;
                                for (int l = 0; l < leftover; l++)
                                    src[l] = src[position + l];
#pragma warning disable CA2022
                                stream.Read(bytes, leftover, ByteArraySize - leftover);
#pragma warning restore CA2022
                                position = 0;
                            }
                        }
                    }

                    Texture = new Texture2D(Engine.Graphics.GraphicsDevice, width, height);
                    Texture.SetData(buffer, 0, totalBytes);
                }
            }
            // premultiply alpha on load
            else if (extension == ".png")
            {
                using (FileStream stream = File.OpenRead(System.IO.Path.Combine(Engine.ContentDirectory, Path)))
                    Texture = Texture2D.FromStream(Engine.Graphics.GraphicsDevice, stream);

                int count = Texture.Width * Texture.Height;
                Color[] data = new Color[count];
                Texture.GetData(data, 0, count);

                fixed (Color* ptr = data)
                {
                    for (int i = 0; i < count; i++)
                    {
                        ptr[i].R = (byte)(ptr[i].R * (ptr[i].A / 255f));
                        ptr[i].G = (byte)(ptr[i].G * (ptr[i].A / 255f));
                        ptr[i].B = (byte)(ptr[i].B * (ptr[i].A / 255f));
                    }
                }

                Texture.SetData(data, 0, count);
            }
            else if (extension == ".xnb")
            {
                string assetName = Path.Replace(".xnb", "");
                Texture = Engine.Instance.Content.Load<Texture2D>(assetName);
            }
            else
            {
                using (FileStream stream = File.OpenRead(System.IO.Path.Combine(Engine.ContentDirectory, Path)))
                    Texture = Texture2D.FromStream(Engine.Graphics.GraphicsDevice, stream);
            }

            Width = Texture.Width;
            Height = Texture.Height;
        }

        public override void Dispose()
        {
            Unload();
            Texture = null;
            VirtualContent.Remove(this);
        }
    }
}
