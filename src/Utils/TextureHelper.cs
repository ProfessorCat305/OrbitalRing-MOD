using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using static ProjectOrbitalRing.ProjectOrbitalRing;

namespace ProjectOrbitalRing.Utils
{
    internal static class TextureHelper
    {
        private static readonly Assembly Assembly = Assembly.GetExecutingAssembly();

        private static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();

        private static float SrgbToLinear(float val)
        {
            if (val <= 0.04045f)
                return val / 12.92f;
            return Mathf.Pow((val + 0.055f) / 1.055f, 2.4f);
        }

        internal static Texture2D GetTexture(string name, string type = "texture")
        {
            if (Cache.TryGetValue(name, out var cached)) return cached;

            using (var stream = Assembly.GetManifestResourceStream($"ProjectOrbitalRing.assets.{type}.{name}.png")) {
                if (stream == null) {
                    LogError($"Could not find texture for {name}");
                    return null;
                }

                using (var memoryStream = new MemoryStream()) {
                    stream.CopyTo(memoryStream);
                    var bytes = memoryStream.ToArray();

                    var texture = new Texture2D(2, 2);
                    if (type == "sprite") {
                        texture = new Texture2D(2, 2);
                        if (!texture.LoadImage(bytes)) return null;

                    } else if (type == "texture") {
                        texture = new Texture2D(2, 2, TextureFormat.ARGB32, true, false); // 加了这句后换贴图生效了，不过=======括住的部分不知道有没有一起生效，先留着了
                        //=====================
                        if (!texture.LoadImage(bytes, markNonReadable: false)) {
                            LogError($"LoadImage failed for {name}");
                            UnityEngine.Object.DestroyImmediate(texture);
                            return null;
                        }
                        
                        texture.filterMode = FilterMode.Bilinear;
                        texture.wrapMode = TextureWrapMode.Repeat;

                        Color[] pixels = texture.GetPixels();
                        for (int i = 0; i < pixels.Length; i++) {
                            Color c = pixels[i];

                            // 1. sRGB -> Linear 伽马转换（关键，解决整体变灰）
                            //c.r = SrgbToLinear(c.r);
                            //c.g = SrgbToLinear(c.g);
                            //c.b = SrgbToLinear(c.b);

                            // 2. Straight‑Alpha → Premultiplied‑Alpha，适配DSP‑a clip贴图
                            //c.r *= c.a;
                            //c.g *= c.a;
                            //c.b *= c.a;

                            pixels[i] = c;
                        }

                        texture.SetPixels(pixels);
                        texture.Apply(true); // true = 更新mipmaps
                    }
                    //=====================

                    texture.name = name;
                    Cache[name] = texture;
                    return texture;
                }
            }
        }

        internal static Sprite GetSprite(string name, int? width = null, int? height = null)
        {
            if (!Cache.TryGetValue(name, out var texture)) texture = GetTexture(name, "sprite");

            return Sprite.Create(texture, new Rect(0, 0, width ?? texture.width, height ?? texture.height), new Vector2(0.5f, 0.5f));
        }
    }
}
