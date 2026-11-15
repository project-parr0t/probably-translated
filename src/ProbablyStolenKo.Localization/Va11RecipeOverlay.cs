using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace ProbablyStolenKo
{
    internal sealed class Va11RecipeOverlay
    {
        private const string TargetModId = "va11halla";
        private const string HarmonyId = "probably-stolen-ko-localization.va11-recipes";
        private const string AssetDirectory = "va11-recipes/";
        private static readonly IReadOnlyDictionary<string, string> AssetPaths = CreateAssetPaths();
        private static readonly IReadOnlyDictionary<string, string> SourceSpriteKeys = CreateSourceSpriteKeys();
        private static Va11RecipeOverlay _active;
        private static bool _installing;

        private readonly ModManifest _manifest;
        private readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private readonly List<Texture2D> _textures = new List<Texture2D>();
        private Harmony _harmony;
        private bool _attempted;

        internal Va11RecipeOverlay(ModManifest manifest)
        {
            _manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
        }

        internal void Apply(string targetModId)
        {
            if (!StringComparer.Ordinal.Equals(targetModId, TargetModId) || _harmony != null || _attempted) return;
            _attempted = true;
            var harmony = new Harmony(HarmonyId);
            try
            {
                _active = this;
                _installing = true;
                harmony.CreateClassProcessor(typeof(ImageSpritePatch)).Patch();
                _harmony = harmony;
            }
            catch
            {
                if (ReferenceEquals(_active, this)) _active = null;
                harmony.UnpatchAll(harmony.Id);
                throw;
            }
            finally
            {
                _installing = false;
            }
        }

        private static void ReplaceRecipeSprite(ref Sprite source)
        {
            var overlay = _active;
            if (overlay == null || source == null ||
                !StringComparer.Ordinal.Equals(LocHelper.GetCurrentLocaleCode(), "ko")) return;
            string key;
            if (SourceSpriteKeys.TryGetValue(source.name, out key)) source = overlay.GetSprite(key);
        }

        [HarmonyPatch(typeof(Image), "set_sprite")]
        private static class ImageSpritePatch
        {
            [HarmonyPrepare]
            private static bool Prepare() => _installing;

            [HarmonyPrefix]
            private static void Prefix(ref Sprite __0) => ReplaceRecipeSprite(ref __0);
        }

        private Sprite GetSprite(string key)
        {
            if (_sprites.TryGetValue(key, out var sprite)) return sprite;
            var bytes = ModHelper.ReadModFileBytes(_manifest.ID, AssetPaths[key]);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                name = key + ".ko",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            try
            {
                if (!ImageConversion.LoadImage(texture, bytes, true) || texture.width != 315 || texture.height != 214)
                    throw new InvalidOperationException("Invalid VA-11 recipe image: " + key);
                sprite = Sprite.Create(texture, new Rect(0f, 0f, 315f, 214f), new Vector2(0.5f, 0.5f), 100f);
                sprite.name = key + ".ko";
                _textures.Add(texture);
                _sprites.Add(key, sprite);
                return sprite;
            }
            catch
            {
                UnityEngine.Object.Destroy(texture);
                throw;
            }
        }

        internal void Restore()
        {
            if (_harmony != null)
            {
                _harmony.UnpatchAll(_harmony.Id);
                _harmony = null;
            }
            if (ReferenceEquals(_active, this)) _active = null;
            foreach (var sprite in _sprites.Values) UnityEngine.Object.Destroy(sprite);
            foreach (var texture in _textures) UnityEngine.Object.Destroy(texture);
            _sprites.Clear();
            _textures.Clear();
            _attempted = false;
        }

        private static IReadOnlyDictionary<string, string> CreateAssetPaths()
        {
            var paths = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["va11halla_recipe_intro"] = AssetDirectory + "recipe_intro.png",
                ["va11halla_recipe_hint"] = AssetDirectory + "recipe_hint.png"
            };
            for (var page = 1; page <= 24; page++)
                paths.Add("va11halla_recipe_" + page, AssetDirectory + "recipe_" + page.ToString("00") + ".png");
            return paths;
        }

        private static IReadOnlyDictionary<string, string> CreateSourceSpriteKeys()
        {
            var keys = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Va11HallA.Assets.recipe_intro_cn_0.png"] = "va11halla_recipe_intro",
                ["Va11HallA.Assets.recipe_hint_cn_0.png"] = "va11halla_recipe_hint"
            };
            for (var page = 1; page <= 24; page++)
                keys.Add("Va11HallA.Assets.recipes_cn_" + page + ".png", "va11halla_recipe_" + page);
            return keys;
        }
    }
}
