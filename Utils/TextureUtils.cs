using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DynamicMaps.Utils
{
    public static class TextureUtils
    {
        private static Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

        public static Texture2D LoadTexture2DFromPath(string absolutePath)
        {
            if (!File.Exists(absolutePath))
            {
                return null;
            }

            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var fileData = File.ReadAllBytes(absolutePath);

            // 使用 ImageConversion.LoadImage 替代 Texture2D.LoadImage
            bool loadSuccess = false;

#if UNITY_2017_1_OR_NEWER
            // Unity 2017.1+ 使用 ImageConversion
            try
            {
                loadSuccess = ImageConversion.LoadImage(tex, fileData);
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogError($"Failed to load image with ImageConversion: {e.Message}");
                loadSuccess = false;
            }
#else
            // 旧版本 Unity 使用 Texture2D.LoadImage
            try
            {
                loadSuccess = tex.LoadImage(fileData);
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogError($"Failed to load image with Texture2D.LoadImage: {e.Message}");
                loadSuccess = false;
            }
#endif

            if (!loadSuccess)
            {
                Plugin.Log.LogError($"Failed to load texture from: {absolutePath}");
                GameObject.Destroy(tex);
                return null;
            }

            return tex;
        }

        public static Sprite GetOrLoadCachedSprite(string path)
        {
            if (_spriteCache.ContainsKey(path))
            {
                return _spriteCache[path];
            }

            var absolutePath = Path.Combine(Plugin.Path, path);
            var texture = LoadTexture2DFromPath(absolutePath);

            if (texture == null)
            {
                Plugin.Log.LogError($"Failed to load texture from path: {absolutePath}");
                return null;
            }

            _spriteCache[path] = Sprite.Create(texture,
                                               new Rect(0f, 0f, texture.width, texture.height),
                                               new Vector2(texture.width / 2, texture.height / 2));

            return _spriteCache[path];
        }
    }
}