using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OfficeFlipOut.UI
{
    public static class UIFactory
    {
        // Board and paper
        public static readonly Color32 BoardBrown = new Color32(162, 118, 74, 255);
        public static readonly Color32 BoardEdge = new Color32(130, 92, 55, 255);
        public static readonly Color32 PaperCream = new Color32(248, 244, 235, 255);
        public static readonly Color32 PaperRuledLine = new Color32(180, 200, 220, 42);
        public static readonly Color32 PaperMarginLine = new Color32(210, 140, 140, 35);

        // Text hierarchy
        public static readonly Color32 TextDark = new Color32(38, 28, 18, 255);
        public static readonly Color32 TextMedium = new Color32(68, 54, 40, 255);
        public static readonly Color32 TextSubtle = new Color32(110, 95, 75, 255);
        public static readonly Color32 TextMuted = new Color32(148, 132, 112, 255);
        public static readonly Color32 TextHandwritten = new Color32(30, 40, 90, 255);

        // Cards
        public static readonly Color32 CardWhite = new Color32(252, 249, 240, 255);
        public static readonly Color32 CardShadow = new Color32(0, 0, 0, 30);

        // Sticky-note tab colors
        public static readonly Color32 TabYellow = new Color32(255, 242, 130, 255);
        public static readonly Color32 TabBlue = new Color32(160, 200, 240, 255);
        public static readonly Color32 TabPink = new Color32(250, 180, 180, 255);
        public static readonly Color32 TabGreen = new Color32(170, 225, 170, 255);
        public static readonly Color32 TabShadow = new Color32(0, 0, 0, 35);
        public static readonly Color32 TabTextDark = new Color32(40, 32, 22, 255);

        // Footer
        public static readonly Color32 FooterBg = new Color32(152, 96, 50, 255);
        public static readonly Color32 FooterText = new Color32(252, 240, 222, 255);

        // Buttons
        public static readonly Color32 ButtonBrown = new Color32(112, 86, 56, 255);
        public static readonly Color32 ButtonBrownText = new Color32(252, 244, 230, 255);

        // (Clipboard clip is now a sprite: clipboard_clip.png)

        // Lock & status
        public static readonly Color32 LockBadge = new Color32(155, 55, 40, 240);
        public static readonly Color32 LockBadgeText = new Color32(255, 242, 235, 255);
        public static readonly Color32 StatusActive = new Color32(68, 135, 72, 255);
        public static readonly Color32 StatusFlipped = new Color32(178, 52, 38, 255);

        // Decorations
        public static readonly Color32 PortraitPlaceholder = new Color32(208, 202, 192, 255);
        public static readonly Color32 Dimmer = new Color(0.02f, 0.03f, 0.05f, 0.65f);
        public static readonly Color32 Shadow = new Color(0f, 0f, 0f, 0.30f);
        public static readonly Color32 Separator = new Color32(180, 166, 146, 80);
        public static readonly Color32 PostItYellow = new Color32(255, 245, 155, 255);
        public static readonly Color32 PostItText = new Color32(58, 48, 35, 255);
        public static readonly Color32 RedPen = new Color32(200, 45, 35, 180);

        // --- Sprite cache (loaded once from Resources/UI/Clipboard/) ---

        private static bool _spritesLoaded;
        private static Sprite _tapeStrip;
        private static Sprite _pushPin;
        private static Sprite _paperClip;
        private static Sprite _coffeeRing;
        private static Sprite _clipboardClip;

        public static Sprite TapeStripSprite { get { EnsureSprites(); return _tapeStrip; } }
        public static Sprite PushPinSprite { get { EnsureSprites(); return _pushPin; } }
        public static Sprite PaperClipSprite { get { EnsureSprites(); return _paperClip; } }
        public static Sprite CoffeeRingSprite { get { EnsureSprites(); return _coffeeRing; } }
        public static Sprite ClipboardClipSprite { get { EnsureSprites(); return _clipboardClip; } }

        private static void EnsureSprites()
        {
            if (_spritesLoaded) return;
            _spritesLoaded = true;
            _tapeStrip = Resources.Load<Sprite>("UI/Clipboard/tape_strip");
            _pushPin = Resources.Load<Sprite>("UI/Clipboard/push_pin");
            _paperClip = Resources.Load<Sprite>("UI/Clipboard/paper_clip");
            _coffeeRing = Resources.Load<Sprite>("UI/Clipboard/coffee_ring");
            _clipboardClip = Resources.Load<Sprite>("UI/Clipboard/clipboard_clip");
        }

        // Employee color identity
        public static Color32 GetIdentityColor(string colorIdentity)
        {
            if (string.IsNullOrWhiteSpace(colorIdentity))
                return new Color32(140, 140, 140, 255);
            string key = colorIdentity.Trim().ToLowerInvariant();
            if (key.Contains("purple")) return new Color32(128, 70, 160, 255);
            if (key.Contains("red")) return new Color32(195, 60, 50, 255);
            if (key.Contains("green") || key.Contains("blue")) return new Color32(55, 140, 130, 255);
            if (key.Contains("black") || key.Contains("executive")) return new Color32(42, 42, 48, 255);
            if (key.Contains("orange")) return new Color32(210, 130, 50, 255);
            return new Color32(140, 140, 140, 255);
        }

        public static string GetDifficultyLabel(Data.EmployeeDifficultyTier tier)
        {
            switch (tier)
            {
                case Data.EmployeeDifficultyTier.Intro: return "EASY";
                case Data.EmployeeDifficultyTier.Mid: return "MEDIUM";
                case Data.EmployeeDifficultyTier.Advanced: return "HARD";
                case Data.EmployeeDifficultyTier.Final: return "BOSS";
                default: return "";
            }
        }

        public static Color32 GetDifficultyColor(Data.EmployeeDifficultyTier tier)
        {
            switch (tier)
            {
                case Data.EmployeeDifficultyTier.Intro: return new Color32(60, 130, 65, 255);
                case Data.EmployeeDifficultyTier.Mid: return new Color32(180, 155, 40, 255);
                case Data.EmployeeDifficultyTier.Advanced: return new Color32(200, 100, 35, 255);
                case Data.EmployeeDifficultyTier.Final: return new Color32(165, 35, 35, 255);
                default: return TextMuted;
            }
        }

        private static Font _font;
        public static Font DefaultFont
        {
            get
            {
                if (_font == null)
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
            }
        }

        // --- Core builders ---

        public static GameObject Rect(string name, Transform parent,
            Vector2? anchorMin = null, Vector2? anchorMax = null,
            Vector2? offsetMin = null, Vector2? offsetMax = null)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin ?? new Vector2(0f, 1f);
            rt.anchorMax = anchorMax ?? new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            if (offsetMin.HasValue) rt.offsetMin = offsetMin.Value;
            if (offsetMax.HasValue) rt.offsetMax = offsetMax.Value;
            if (!anchorMin.HasValue && !anchorMax.HasValue)
                rt.sizeDelta = Vector2.zero;
            return go;
        }

        public static Text Label(string name, Transform parent, string text,
            int fontSize, FontStyle style, Color color,
            TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Text label = go.AddComponent<Text>();
            label.font = DefaultFont;
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.color = color;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.alignment = alignment;
            return label;
        }

        public static Button Button(string name, Transform parent, string labelText,
            Color bgColor, Color textColor, int fontSize = 16, bool addHover = true)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            Image bg = go.AddComponent<Image>();
            bg.color = bgColor;

            UnityEngine.UI.Button button = go.AddComponent<UnityEngine.UI.Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.06f, 1.04f, 1.01f, 1f);
            colors.pressedColor = new Color(0.93f, 0.91f, 0.89f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.7f, 0.7f, 0.7f, 0.5f);
            button.colors = colors;

            Text label = Label("Label", go.transform, labelText, fontSize, FontStyle.Bold,
                textColor, TextAnchor.MiddleCenter);
            RectTransform labelRt = label.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            LayoutElement le = go.AddComponent<LayoutElement>();
            le.minHeight = 38;

            if (addHover) go.AddComponent<ButtonHoverPunch>();
            return button;
        }

        public static Image FilledImage(string name, Transform parent, Color color,
            Vector2? anchorMin = null, Vector2? anchorMax = null,
            Vector2? offsetMin = null, Vector2? offsetMax = null)
        {
            GameObject go = Rect(name, parent, anchorMin, anchorMax, offsetMin, offsetMax);
            Image img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        /// <summary>Places a sprite at an anchor point with a fixed pixel size. Excluded from layout.</summary>
        public static Image SpriteDecoration(string name, Transform parent, Sprite sprite,
            Vector2 anchorPos, Vector2 size, float rotation = 0f, float alpha = 1f)
        {
            GameObject go = Rect(name, parent,
                anchorMin: anchorPos, anchorMax: anchorPos);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            if (rotation != 0f)
                rt.localRotation = Quaternion.Euler(0f, 0f, rotation);

            LayoutElement le = go.AddComponent<LayoutElement>();
            le.ignoreLayout = true;

            Image img = go.AddComponent<Image>();
            img.raycastTarget = false;
            if (sprite != null)
            {
                img.sprite = sprite;
                img.preserveAspect = true;
                img.color = new Color(1f, 1f, 1f, alpha);
            }
            else
            {
                img.color = new Color(0f, 0f, 0f, 0f);
            }
            return img;
        }

        // --- Themed builders ---

        public static Image ColorStrip(string name, Transform parent, Color color, float height = 5f)
        {
            GameObject go = Rect(name, parent);
            Image img = go.AddComponent<Image>();
            img.color = color;
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
            return img;
        }

        public static Image DashedLine(string name, Transform parent, float height = 1f)
        {
            GameObject go = Rect(name, parent);
            Image img = go.AddComponent<Image>();
            img.color = Separator;
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
            return img;
        }

        public static Text PageCounter(string name, Transform parent)
        {
            return Label(name, parent, "", 14, FontStyle.Italic,
                TextMuted, TextAnchor.MiddleCenter);
        }

        public static Button StickyTab(string name, Transform parent, string labelText,
            Color32 tabColor, float rotation, out Image bg, out GameObject tabRoot)
        {
            tabRoot = Rect(name, parent);
            LayoutElement le = tabRoot.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.minHeight = 48;

            FilledImage("TabShadow", tabRoot.transform, TabShadow,
                anchorMin: Vector2.zero, anchorMax: Vector2.one,
                offsetMin: new Vector2(1, -3), offsetMax: new Vector2(3, 0));

            bg = tabRoot.AddComponent<Image>();
            bg.color = tabColor;
            tabRoot.GetComponent<RectTransform>().localRotation =
                Quaternion.Euler(0f, 0f, rotation);

            Text label = Label("Label", tabRoot.transform, labelText, 15, FontStyle.Bold,
                TabTextDark, TextAnchor.MiddleCenter);
            label.raycastTarget = false;
            RectTransform labelRt = label.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(4, 0);
            labelRt.offsetMax = new Vector2(-4, 0);

            UnityEngine.UI.Button button = tabRoot.AddComponent<UnityEngine.UI.Button>();
            ColorBlock cb = button.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.04f, 1.03f, 1.01f, 1f);
            cb.pressedColor = new Color(0.95f, 0.94f, 0.92f, 1f);
            cb.selectedColor = cb.highlightedColor;
            button.colors = cb;

            tabRoot.AddComponent<ButtonHoverPunch>();
            return button;
        }

        /// <summary>Tape strip using the real sprite asset.</summary>
        public static void TapeStrip(Transform parent, Vector2 anchorPos, float width, float height, float rotation)
        {
            SpriteDecoration("Tape", parent, TapeStripSprite, anchorPos,
                new Vector2(width, height), rotation, 0.75f);
        }

        /// <summary>Coffee ring stain using the real sprite asset.</summary>
        public static void CoffeeRingStain(Transform parent, Vector2 anchorPos, float size, float rotation)
        {
            SpriteDecoration("CoffeeRing", parent, CoffeeRingSprite, anchorPos,
                new Vector2(size, size), rotation, 0.22f);
        }

        /// <summary>Push pin using the real sprite asset.</summary>
        public static void PushPin(Transform parent, Vector2 anchorPos, float size = 36f, float rotation = 0f)
        {
            SpriteDecoration("PushPin", parent, PushPinSprite, anchorPos,
                new Vector2(size, size), rotation);
        }

        /// <summary>Paper clip using the real sprite asset (source 355x142, ~2.5:1).</summary>
        public static void PaperClip(Transform parent, Vector2 anchorPos, float rotation = 0f, float scale = 1f)
        {
            SpriteDecoration("PaperClip", parent, PaperClipSprite, anchorPos,
                new Vector2(75 * scale, 30 * scale), rotation);
        }

        /// <summary>Faint ruled lines on the paper background. Excluded from layout.</summary>
        public static void RuledLines(Transform parent, int lineCount)
        {
            for (int i = 0; i < lineCount; i++)
            {
                float yNorm = 1f - ((float)(i + 1) / (lineCount + 1));
                Image line = FilledImage("Rule" + i, parent, PaperRuledLine,
                    anchorMin: new Vector2(0.06f, yNorm),
                    anchorMax: new Vector2(0.94f, yNorm),
                    offsetMin: new Vector2(0, -0.5f),
                    offsetMax: new Vector2(0, 0.5f));
                LayoutElement le = line.gameObject.AddComponent<LayoutElement>();
                le.ignoreLayout = true;
            }

            Image margin = FilledImage("MarginLine", parent, PaperMarginLine,
                anchorMin: new Vector2(0.058f, 0.05f),
                anchorMax: new Vector2(0.062f, 0.95f));
            LayoutElement mle = margin.gameObject.AddComponent<LayoutElement>();
            mle.ignoreLayout = true;
        }

        public static GameObject PostItNote(string name, Transform parent, string message,
            float rotation = 2.5f)
        {
            GameObject go = Rect(name, parent);

            FilledImage("PostItShadow", go.transform, new Color(0, 0, 0, 0.08f),
                anchorMin: Vector2.zero, anchorMax: Vector2.one,
                offsetMin: new Vector2(2, -3), offsetMax: new Vector2(4, -1));

            Image bg = go.AddComponent<Image>();
            bg.color = PostItYellow;
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 75;
            le.preferredWidth = 190;
            le.flexibleWidth = 0;
            go.GetComponent<RectTransform>().localRotation = Quaternion.Euler(0f, 0f, rotation);

            // Pin the post-it note to the board
            PushPin(go.transform, new Vector2(0.5f, 0.92f), 28f, 8f);

            Text text = Label("PostItText", go.transform, message, 13, FontStyle.Normal,
                PostItText, TextAnchor.UpperLeft);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            RectTransform textRt = text.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(8, 6);
            textRt.offsetMax = new Vector2(-8, -6);
            return go;
        }

        // --- Layout helpers ---

        public static VerticalLayoutGroup VerticalGroup(GameObject go,
            TextAnchor alignment = TextAnchor.UpperLeft, float spacing = 8,
            RectOffset padding = null, bool expandWidth = true, bool expandHeight = false)
        {
            VerticalLayoutGroup vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = alignment;
            vlg.spacing = spacing;
            vlg.padding = padding ?? new RectOffset(0, 0, 0, 0);
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = expandWidth;
            vlg.childForceExpandHeight = expandHeight;
            return vlg;
        }

        public static HorizontalLayoutGroup HorizontalGroup(GameObject go,
            TextAnchor alignment = TextAnchor.MiddleCenter, float spacing = 10,
            bool expandWidth = true, bool expandHeight = false)
        {
            HorizontalLayoutGroup hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = alignment;
            hlg.spacing = spacing;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = expandWidth;
            hlg.childForceExpandHeight = expandHeight;
            return hlg;
        }
    }

    public class ButtonHoverPunch : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler
    {
        private Vector3 baseScale = Vector3.one;
        private Coroutine scaleRoutine;

        private void Awake() { baseScale = transform.localScale; }

        public void OnPointerEnter(PointerEventData e) { AnimateTo(baseScale * 1.05f, 0.09f); }
        public void OnPointerExit(PointerEventData e) { AnimateTo(baseScale, 0.09f); }
        public void OnPointerDown(PointerEventData e) { AnimateTo(baseScale * 0.95f, 0.04f); }
        public void OnPointerUp(PointerEventData e) { AnimateTo(baseScale * 1.05f, 0.07f); }

        private void AnimateTo(Vector3 target, float duration)
        {
            if (scaleRoutine != null) StopCoroutine(scaleRoutine);
            scaleRoutine = StartCoroutine(ScaleTo(target, duration));
        }

        private IEnumerator ScaleTo(Vector3 target, float duration)
        {
            Vector3 start = transform.localScale;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                transform.localScale = Vector3.Lerp(start, target,
                    Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }
            transform.localScale = target;
        }
    }
}
