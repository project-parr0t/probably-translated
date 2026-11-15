using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ProbablyStolenKo
{
    [DefaultExecutionOrder(-32000)]
    internal sealed class ManagedPatchLoadingOverlay : MonoBehaviour
    {
        private sealed class Glyph
        {
            internal Glyph(JArray values)
            {
                X = (int)values[0];
                Y = (int)values[1];
                Width = (int)values[2];
                Height = (int)values[3];
                Advance = (int)values[4];
                BearingX = (int)values[5];
                BearingY = (int)values[6];
            }

            internal int X { get; }
            internal int Y { get; }
            internal int Width { get; }
            internal int Height { get; }
            internal int Advance { get; }
            internal int BearingX { get; }
            internal int BearingY { get; }
        }

        private const int PatchBudgetMilliseconds = 32;
        private const string AtlasPath = "loading-ui/galmuri9-atlas.png";
        private const string AtlasMetadataPath = "loading-ui/galmuri9-atlas.json";
        private readonly Dictionary<char, Glyph> _glyphs = new Dictionary<char, Glyph>();
        private ManagedLiteralOverlay _patches;
        private ModLog _log;
        private string _manifestId;
        private Texture2D _logo;
        private Texture2D _fontAtlas;
        private bool _visible = true;
        private bool _completionAllowed;
        private bool _assetsLogged;
        private bool _assetFailureLogged;
        private int _framesBeforePatching = 1;
        private int _idleFrames;
        private EventSystem _blockedEventSystem;
        private bool _blockedEventSystemWasEnabled;
        private bool _blockedNavigationWasEnabled;
        private GameObject _blockedSelection;

        internal static ManagedPatchLoadingOverlay Create(
            ManagedLiteralOverlay patches,
            string manifestId,
            ModLog log)
        {
            if (patches == null) throw new ArgumentNullException(nameof(patches));
            if (string.IsNullOrWhiteSpace(manifestId)) throw new ArgumentException("Manifest ID is required.", nameof(manifestId));
            if (log == null) throw new ArgumentNullException(nameof(log));
            var host = new GameObject("ProbablyTranslated.PatchProgress");
            host.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(host);
            var overlay = host.AddComponent<ManagedPatchLoadingOverlay>();
            overlay._patches = patches;
            overlay._manifestId = manifestId;
            overlay._log = log;
            return overlay;
        }

        internal void AllowCompletion()
        {
            _completionAllowed = true;
        }

        internal void Dispose()
        {
            RestoreInput();
            _patches = null;
            if (_logo != null) Destroy(_logo);
            if (_fontAtlas != null) Destroy(_fontAtlas);
            _logo = null;
            _fontAtlas = null;
            _glyphs.Clear();
            if (gameObject != null) Destroy(gameObject);
        }

        private void Update()
        {
            if (_patches == null)
            {
                RestoreInput();
                return;
            }

            if (!_visible && _patches.HasPendingWork)
            {
                _visible = true;
                _framesBeforePatching = 1;
            }
            if (!_visible) return;

            BlockInput();
            if (!_patches.HasPendingWork)
            {
                if (_completionAllowed && ++_idleFrames >= 2)
                {
                    _visible = false;
                    RestoreInput();
                }
                return;
            }

            _idleFrames = 0;
            if (_framesBeforePatching != 0)
            {
                _framesBeforePatching--;
                return;
            }

            _patches.ProcessPending(PatchBudgetMilliseconds);
        }

        private void OnGUI()
        {
            if (!_visible || _patches == null) return;

            var oldColor = GUI.color;
            var oldDepth = GUI.depth;
            GUI.depth = -10000;
            GUI.color = new Color(0.055f, 0.047f, 0.075f, 1f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);

            if (TryLoadAssets())
            {
                const float panelWidth = 720f;
                const float panelHeight = 356f;
                var left = (Screen.width - panelWidth) * 0.5f;
                var top = (Screen.height - panelHeight) * 0.5f;

                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(left + 265f, top + 18f, 190f, 174f), _logo, ScaleMode.ScaleToFit, true);

                var total = Math.Max(1, _patches.TotalWork);
                var completed = Math.Min(_patches.CompletedWork, total);
                var percentage = completed * 100 / total;
                var track = new Rect(left + 70f, top + 218f, panelWidth - 140f, 34f);
                GUI.color = new Color(0.17f, 0.15f, 0.21f, 1f);
                GUI.DrawTexture(track, Texture2D.whiteTexture);
                GUI.color = new Color(0.42f, 0.78f, 0.76f, 1f);
                GUI.DrawTexture(
                    new Rect(track.x, track.y, track.width * completed / total, track.height),
                    Texture2D.whiteTexture);
                DrawTextCentered(percentage + "%", track.center.x, track.y + 2f, Color.white);

                var modId = _patches.CurrentModId ?? "finishing";
                DrawTextCentered(
                    "<" + modId + "> 패치 진행 (" + _patches.CurrentBindingCompleted + "/" +
                    _patches.CurrentBindingTotal + ")",
                    left + panelWidth * 0.5f,
                    top + 282f,
                    new Color(0.88f, 0.88f, 0.91f, 1f));
            }

            GUI.color = oldColor;
            GUI.depth = oldDepth;
            var currentEvent = Event.current;
            if (currentEvent != null &&
                currentEvent.type != EventType.Layout &&
                currentEvent.type != EventType.Repaint &&
                currentEvent.type != EventType.Used)
                currentEvent.Use();
        }

        private void BlockInput()
        {
            Input.ResetInputAxes();
            var current = EventSystem.current;
            if (current == _blockedEventSystem) return;
            RestoreEventSystem();
            if (current == null) return;
            _blockedEventSystem = current;
            _blockedEventSystemWasEnabled = current.enabled;
            _blockedNavigationWasEnabled = current.sendNavigationEvents;
            _blockedSelection = current.currentSelectedGameObject;
            current.sendNavigationEvents = false;
            current.enabled = false;
        }

        private void RestoreInput()
        {
            RestoreEventSystem();
            Input.ResetInputAxes();
        }

        private void RestoreEventSystem()
        {
            if (_blockedEventSystem == null) return;
            _blockedEventSystem.enabled = _blockedEventSystemWasEnabled;
            _blockedEventSystem.sendNavigationEvents = _blockedNavigationWasEnabled;
            if (_blockedEventSystemWasEnabled && _blockedSelection != null)
                _blockedEventSystem.SetSelectedGameObject(_blockedSelection);
            _blockedEventSystem = null;
            _blockedSelection = null;
        }

        private bool TryLoadAssets()
        {
            if (_logo != null && _fontAtlas != null && _glyphs.Count != 0) return true;
            try
            {
                if (_logo == null)
                {
                    _logo = LoadTexture(ModHelper.ReadModFileBytes(_manifestId, "preview.png"), "ProbablyTranslated.Logo");
                }
                if (_fontAtlas == null)
                {
                    _fontAtlas = LoadTexture(ModHelper.ReadModFileBytes(_manifestId, AtlasPath), "ProbablyTranslated.Galmuri9");
                    var metadata = JObject.Parse(ModHelper.ReadModFileText(_manifestId, AtlasMetadataPath));
                    foreach (var property in ((JObject)metadata["glyphs"]).Properties())
                    {
                        if (property.Name.Length == 1) _glyphs[property.Name[0]] = new Glyph((JArray)property.Value);
                    }
                }
                var ready = _glyphs.Count != 0;
                if (ready && !_assetsLogged)
                {
                    _assetsLogged = true;
                    _log.Log("Patch progress UI displayed with Galmuri9 assets.");
                }
                return ready;
            }
            catch (Exception error)
            {
                if (!_assetFailureLogged)
                {
                    _assetFailureLogged = true;
                    _log.Error("Patch progress UI assets failed to load. " + error.GetType().Name + ": " + error.Message);
                }
                return false;
            }
        }

        private static Texture2D LoadTexture(byte[] bytes, string name)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            if (!texture.LoadImage(bytes, false))
            {
                Destroy(texture);
                throw new InvalidOperationException("Loading UI texture could not be decoded.");
            }
            return texture;
        }

        private void DrawTextCentered(string text, float centerX, float top, Color color)
        {
            var width = MeasureText(text);
            DrawText(text, centerX - width * 0.5f, top, color);
        }

        private float MeasureText(string text)
        {
            var width = 0f;
            foreach (var character in text)
            {
                if (_glyphs.TryGetValue(character, out var glyph) || _glyphs.TryGetValue('?', out glyph))
                    width += glyph.Advance;
            }
            return width;
        }

        private void DrawText(string text, float left, float top, Color color)
        {
            GUI.color = color;
            foreach (var character in text)
            {
                if (!_glyphs.TryGetValue(character, out var glyph) && !_glyphs.TryGetValue('?', out glyph)) continue;
                var destination = new Rect(
                    left + glyph.BearingX,
                    top + glyph.BearingY,
                    glyph.Width,
                    glyph.Height);
                var source = new Rect(
                    (float)glyph.X / _fontAtlas.width,
                    1f - (float)(glyph.Y + glyph.Height) / _fontAtlas.height,
                    (float)glyph.Width / _fontAtlas.width,
                    (float)glyph.Height / _fontAtlas.height);
                GUI.DrawTextureWithTexCoords(destination, _fontAtlas, source, true);
                left += glyph.Advance;
            }
        }
    }
}
