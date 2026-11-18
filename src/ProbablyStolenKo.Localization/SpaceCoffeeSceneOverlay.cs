using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace ProbablyStolenKo
{
    internal sealed class SpaceCoffeeSceneOverlay
    {
        private const string TargetModId = "space_coffee";
        private const string HarmonyId = "probably-stolen-ko-localization.space-coffee-scene";
        private const string AssetPath = "space-coffee/coffee_shop_scene_ko.png";
        private static SpaceCoffeeSceneOverlay _active;
        private static bool _installing;

        private readonly ModManifest _manifest;
        private Harmony _harmony;
        private Sprite _sprite;
        private Texture2D _texture;
        private bool _attempted;

        internal SpaceCoffeeSceneOverlay(ModManifest manifest)
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

        private static void ReplaceSceneSprite(ref Sprite source)
        {
            var overlay = _active;
            if (overlay == null || source == null ||
                !StringComparer.Ordinal.Equals(LocHelper.GetCurrentLocaleCode(), "ko") ||
                !StringComparer.Ordinal.Equals(source.name, "coffee_shop_scene")) return;
            source = overlay.GetSprite(source);
        }

        [HarmonyPatch(typeof(Image), "set_sprite")]
        private static class ImageSpritePatch
        {
            [HarmonyPrepare]
            private static bool Prepare() => _installing;

            [HarmonyPrefix]
            private static void Prefix(ref Sprite __0) => ReplaceSceneSprite(ref __0);
        }

        private Sprite GetSprite(Sprite source)
        {
            if (_sprite != null) return _sprite;
            var bytes = ModHelper.ReadModFileBytes(_manifest.ID, AssetPath);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                name = "coffee_shop_scene.ko",
                filterMode = source.texture.filterMode,
                wrapMode = TextureWrapMode.Clamp
            };
            try
            {
                if (!ImageConversion.LoadImage(texture, bytes, true) || texture.width != 960 || texture.height != 540)
                    throw new InvalidOperationException("Invalid Space Coffee scene image.");
                var sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    source.pivot,
                    source.pixelsPerUnit);
                sprite.name = source.name;
                _texture = texture;
                _sprite = sprite;
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
            if (_sprite != null) UnityEngine.Object.Destroy(_sprite);
            if (_texture != null) UnityEngine.Object.Destroy(_texture);
            _sprite = null;
            _texture = null;
            _attempted = false;
        }
    }
}
