using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Monocle
{
    public static class VirtualContent
    {
        private static List<VirtualAsset> assets = new List<VirtualAsset>();
        private static bool reloading;

        public static int Count
        {
            get { return assets.Count; }
        }

        public static VirtualTexture CreateTexture(string path)
        {
            VirtualTexture texture = new VirtualTexture(path);
            assets.Add(texture);
            return texture;
        }

        public static VirtualTexture CreateTexture(string name, int width, int height, Color color)
        {
            VirtualTexture texture = new VirtualTexture(name, width, height, color);
            assets.Add(texture);
            return texture;
        }

        public static VirtualRenderTarget CreateRenderTarget(string name, int width, int height, bool depth = false, bool preserve = true, int multiSampleCount = 0)
        {
            VirtualRenderTarget target = new VirtualRenderTarget(name, width, height, multiSampleCount, depth, preserve);
            assets.Add(target);
            return target;
        }

        public static void BySize()
        {
            Dictionary<int, Dictionary<int, int>> bySize = new Dictionary<int, Dictionary<int, int>>();
            foreach (VirtualAsset asset in assets)
            {
                if (!bySize.ContainsKey(asset.Width))
                    bySize.Add(asset.Width, new Dictionary<int, int>());
                if (!bySize[asset.Width].ContainsKey(asset.Height))
                    bySize[asset.Width].Add(asset.Height, 0);

                bySize[asset.Width][asset.Height]++;
            }

            foreach (var widthGroup in bySize)
                foreach (var heightGroup in widthGroup.Value)
                    Console.WriteLine(widthGroup.Key + "x" + heightGroup.Key + ": " + heightGroup.Value);
        }

        public static void ByName()
        {
            foreach (VirtualAsset asset in assets)
                Console.WriteLine(asset.Name + "[" + asset.Width + "x" + asset.Height + "]");
        }

        internal static void Remove(VirtualAsset asset)
        {
            assets.Remove(asset);
        }

        internal static void Reload()
        {
            if (reloading)
                foreach (VirtualAsset asset in assets)
                    asset.Reload();
            reloading = false;
        }

        internal static void Unload()
        {
            foreach (VirtualAsset asset in assets)
                asset.Unload();
            reloading = true;
        }
    }
}
