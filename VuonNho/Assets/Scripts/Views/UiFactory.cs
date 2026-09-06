using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VuonNho.Core;

namespace VuonNho.Views
{
    /// <summary>
    /// Mot kieu panel, mot kieu nut voi cac trang thai, thanh tien do va badge.
    /// Khong thiet ke rieng tung cua so.
    ///
    /// Goc bo duoc sinh bang code (xem <see cref="Rounded"/>) chu khong nhap file anh, dung
    /// rang buoc "UI bang hinh co ban, khong phu thuoc asset pack" cua ke hoach.
    /// </summary>
    public static class UiFactory
    {
        /// <summary>O 1366x768: chu chinh tu 18 px tro len, nut chinh cao tu 44 px tro len.</summary>
        public const int FontSizeMeta = 16;
        public const int FontSizeBody = 18;
        public const int FontSizeRowTitle = 20;
        public const int FontSizeTitle = 24;
        public const float ButtonHeight = 44f;

        /// <summary>Le ngoai chung cho moi be mat HUD.</summary>
        public const float EdgeMargin = 16f;
        /// <summary>Khoang cach giua hai be mat canh nhau.</summary>
        public const float Gutter = 12f;

        // --- ban kinh goc bo dung xuyen suot HUD
        public const int RadiusPanel = 14;
        public const int RadiusControl = 10;
        public const int RadiusTrack = 5;
        public const int RadiusDot = 6;

        /// <summary>Canh o icon: 24 px cho dong danh sach va nut, 32 px cho the may pha.</summary>
        public const float IconSizeRow = 32f;
        public const float IconSizeCard = 48f;
        public const float IconSizePreview = 56f;

        static Font _font;
        static Font _bodyFont;
        static Font _displayFont;
        static Font _symbolFont;
        static readonly Dictionary<int, Sprite> RoundedCache = new Dictionary<int, Sprite>();

        /// <summary>
        /// Font cua bo nhan dien dat tu GardenSkin luc khoi dong. Chua dat thi HUD van chay
        /// bang font he thong, nen thieu file font khong bao gio lam vo giao dien.
        /// </summary>
        public static void SetFonts(Font body, Font display)
        {
            SetFonts(body, display, null);
        }

        public static void SetFonts(Font body, Font display, Font symbol)
        {
            _bodyFont = body;
            _displayFont = display != null ? display : body;
            _symbolFont = symbol;
        }

        /// <summary>Chua nap duoc font icon thi tra ve null va nut giu nguyen chu khong.</summary>
        public static Font SymbolFont { get { return _symbolFont; } }

        /// <summary>
        /// Ma ky tu cua cac icon Material Symbols dang dung. Ten bien lay dung ten icon tren
        /// fonts.google.com/icons de tra nguoc lai duoc.
        /// </summary>
        public static class Symbols
        {
            public const string Inventory2 = "\ue1a1";
            public const string Upgrade = "\uf0fb";
            public const string FormatPaint = "\ue243";
            public const string Settings = "\ue8b8";
            public const string Close = "\ue5cd";
            public const string Sell = "\uf05b";
            public const string ShoppingCart = "\ue8cc";
            public const string Add = "\ue145";
            public const string Delete = "\ue92e";
            public const string PlayArrow = "\ue037";
            public const string Download = "\uf090";
            public const string RestartAlt = "\uf053";
            public const string Undo = "\ue166";
            public const string DeleteForever = "\ue92b";
            public const string FastForward = "\ue01f";
            public const string Logout = "\ue9ba";
            public const string VolumeUp = "\ue050";
            public const string VolumeOff = "\ue04f";
            public const string Check = "\ue668";
            /// <summary>Cung mot ma voi <see cref="DefaultContent.CoinGlyph"/>.</summary>
            public const string LocalAtm = DefaultContent.CoinGlyph;
            public const string Schedule = "\uefd6";
            public const string Factory = "\uebbc";
            public const string GroupAdd = "\ue7f0";
            public const string PersonRemove = "\uef66";
        }

        /// <summary>
        /// So xu kem icon tien. Tra ve chinh cai nhan so, nen cho goi chi viec gan .text
        /// nhu voi mot Label thuong.
        /// </summary>
        public static Text CoinValue(Transform parent, string name, int fontSize, float symbolSize,
                                     TextAnchor anchor)
        {
            var group = Node(parent, name);
            var row = HorizontalList(group, 6f, new RectOffset(0, 0, 0, 0));
            row.childAlignment = anchor;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;

            var symbol = Symbol(group.transform, "CoinSymbol", Symbols.LocalAtm, symbolSize);
            symbol.color = GardenPalette.TextCoin;

            var value = Label(group.transform, "Value", "", fontSize, TextAnchor.MiddleLeft,
                              GardenPalette.TextCoin, true);
            value.horizontalOverflow = HorizontalWrapMode.Overflow;
            return value;
        }

        /// <summary>
        /// Icon dang glyph. Dung Text chu khong dung Image nen net o moi co chu va doi mau
        /// bang chinh mau chu, khong can atlas hay anh rieng cho tung kich thuoc.
        /// </summary>
        public static Text Symbol(Transform parent, string name, string glyph, float size)
        {
            var go = Node(parent, name);
            var label = go.AddComponent<Text>();
            label.font = _symbolFont != null ? _symbolFont : Font;
            // Chieu cao mot dong chu lon hon co chu khoang 17%, nen ve dung co o thi glyph
            // cao hon o chua no. Ve nho lai de icon nam gon trong o va khong day bo cuc.
            label.fontSize = Mathf.RoundToInt(size * 0.8f);
            label.text = glyph;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = GardenPalette.IconTint;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.supportRichText = false;
            label.raycastTarget = false;

            var layout = go.AddComponent<LayoutElement>();
            layout.preferredWidth = size;
            layout.preferredHeight = size;
            layout.minWidth = size;
            return label;
        }

        /// <summary>Chu chinh: nhan, mo ta, so lieu.</summary>
        public static Font Font
        {
            get
            {
                if (_bodyFont != null) return _bodyFont;
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
            }
        }

        /// <summary>Chu nhan manh: tieu de, so xu, nhan nut.</summary>
        public static Font DisplayFont
        {
            get { return _displayFont != null ? _displayFont : Font; }
        }

        /// <summary>
        /// Sprite 9-slice goc bo, sinh trong bo nho va dung lai theo ban kinh.
        /// Anh chi co trang + alpha nen mau that van do Image.color quyet dinh — nho vay
        /// khong dinh gi den color space Linear cua project.
        /// </summary>
        public static Sprite Rounded(int radius)
        {
            if (radius <= 0) return null;

            Sprite cached;
            if (RoundedCache.TryGetValue(radius, out cached) && cached != null) return cached;

            int size = radius * 2 + 2;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, false);
            texture.name = "VuonNho_Rounded_" + radius;
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.anisoLevel = 0;
            texture.hideFlags = HideFlags.HideAndDontSave;

            var pixels = new Color32[size * size];
            float half = size * 0.5f;
            float inner = half - radius;
            for (int y = 0; y < size; y++)
            {
                float qy = Mathf.Abs(y + 0.5f - half) - inner;
                for (int x = 0; x < size; x++)
                {
                    float qx = Mathf.Abs(x + 0.5f - half) - inner;
                    float ox = Mathf.Max(qx, 0f);
                    float oy = Mathf.Max(qy, 0f);
                    // Khoang cach co dau den vien hinh chu nhat bo goc.
                    float distance = Mathf.Min(Mathf.Max(qx, qy), 0f) + Mathf.Sqrt(ox * ox + oy * oy) - radius;
                    // Doc mem trong mot texel de goc khong bi rang cua.
                    byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(0.5f - distance) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            var sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f),
                                       100f, 0, SpriteMeshType.FullRect,
                                       new Vector4(radius, radius, radius, radius));
            sprite.name = texture.name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            RoundedCache[radius] = sprite;
            return sprite;
        }

        public static RectTransform Rect(GameObject target)
        {
            var rect = target.GetComponent<RectTransform>();
            if (rect == null) rect = target.AddComponent<RectTransform>();
            return rect;
        }

        public static GameObject Node(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        public static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
                                   Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        /// <summary>Mot mang mau. radius = 0 cho canh vuong (thanh tren chay het be ngang).</summary>
        public static Image Panel(Transform parent, string name, Color color, int radius = 0)
        {
            var go = Node(parent, name);
            var image = go.AddComponent<Image>();
            image.color = color;

            var sprite = Rounded(radius);
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
                image.fillCenter = true;
                // Canvas dung referencePixelsPerUnit 100 nen he so nay giu vien dung bang radius px.
                image.pixelsPerUnitMultiplier = 1f;
            }
            return image;
        }

        public static Text Label(Transform parent, string name, string text, int size,
                                 TextAnchor anchor, Color color)
        {
            return Label(parent, name, text, size, anchor, color, false);
        }

        public static Text Label(Transform parent, string name, string text, int size,
                                 TextAnchor anchor, Color color, bool display)
        {
            var go = Node(parent, name);
            var label = go.AddComponent<Text>();
            label.font = display ? DisplayFont : Font;
            label.fontSize = size;
            label.text = text;
            label.alignment = anchor;
            label.color = color;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.supportRichText = false;
            // Dau tieng Viet chong hai tang can them cho doc; chu khong bao gio chan click.
            label.lineSpacing = 1.15f;
            label.raycastTarget = false;
            return label;
        }

        // ---------------------------------------------------------------- icon

        /// <summary>
        /// O vuong chua mot icon. Khong co sprite thi o bi tat han — layout group bo qua con da
        /// tat nen bo cuc y het luc HUD chua co anh nao, khong de lai lo trong.
        /// </summary>
        public static Image Icon(Transform parent, string name, Sprite sprite, float size)
        {
            var go = Node(parent, name);
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = GardenPalette.IconTint;
            image.preserveAspect = true;
            image.raycastTarget = false;

            var rect = Rect(go);
            rect.sizeDelta = new Vector2(size, size);

            // Chi dat be ngang/cao cua chinh o anh; khong dong vao dong nao co chu.
            var layout = go.AddComponent<LayoutElement>();
            layout.preferredWidth = size;
            layout.preferredHeight = size;
            layout.flexibleWidth = 0f;
            layout.flexibleHeight = 0f;

            go.SetActive(sprite != null);
            return image;
        }

        public static void SetIcon(Image icon, Sprite sprite)
        {
            SetIcon(icon, sprite, GardenPalette.IconTint);
        }

        /// <summary>Het anh thi o tu tat de bo cuc tro lai dung nhu khi chua co icon.</summary>
        public static void SetIcon(Image icon, Sprite sprite, Color tint)
        {
            if (icon == null) return;
            icon.sprite = sprite;
            icon.color = tint;
            icon.gameObject.SetActive(sprite != null);
        }

        // ---------------------------------------------------------------- nut

        public enum ButtonStyle
        {
            /// <summary>Hanh dong chinh: mua, ban, gieo.</summary>
            Primary,
            /// <summary>Dieu huong va huy: khong duoc gianh su chu y voi hanh dong chinh.</summary>
            Quiet,
        }

        public enum ButtonState
        {
            Normal,
            /// <summary>Dang duoc chon — bam nua khong lam gi, nhung khong phai la bi tat.</summary>
            Selected,
            Disabled,
        }

        /// <summary>
        /// icon la tham so cuoi va co gia tri mac dinh, nen moi cho goi cu van dung nguyen ven.
        /// Khong truyen icon thi nut duoc dung y het truoc day, tung dong mot.
        /// </summary>
        public static Button TextButton(Transform parent, string name, string caption, UnityAction onClick,
                                        ButtonStyle style = ButtonStyle.Primary, Sprite icon = null,
                                        string symbol = null)
        {
            var go = Node(parent, name);
            var baseColor = style == ButtonStyle.Primary ? GardenPalette.ButtonNormal : GardenPalette.ButtonQuiet;
            var baseTextColor = style == ButtonStyle.Primary ? GardenPalette.TextOnPrimary : GardenPalette.TextPrimary;

            var image = go.AddComponent<Image>();
            image.color = baseColor;
            var sprite = Rounded(RadiusControl);
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
                image.fillCenter = true;
                image.pixelsPerUnitMultiplier = 1f;
            }

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;

            // Image.color giu mau that; ColorBlock chi con lam sang/toi khi ro chuot va bam.
            // disabledColor phai la trang, neu khong nut tat bi lam toi hai lan.
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.94f, 0.97f, 0.90f, 1f);
            colors.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = Color.white;
            colors.fadeDuration = 0.14f;
            button.colors = colors;

            // Game chi dung chuot. Khong tat navigation thi phim Space bam lai nut vua click.
            var navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;

            if (onClick != null) button.onClick.AddListener(onClick);

            // Nhan nam trong layout group chu khong stretch cung: chu tieng Viet dai xuong hai
            // dong thi nut cao them thay vi de chu tran ra ngoai nen.
            Text label;
            Image iconImage = null;
            Text symbolLabel = null;
            if (icon == null && !string.IsNullOrEmpty(symbol) && _symbolFont != null)
            {
                // Cung bo cuc nhu nut co anh: icon truoc, chu chay tu trai.
                var row = HorizontalList(go, 8f, new RectOffset(10, 10, 4, 4));
                row.childAlignment = TextAnchor.MiddleLeft;
                row.childForceExpandWidth = false;
                row.childForceExpandHeight = false;
                symbolLabel = Symbol(go.transform, "Symbol", symbol, IconSizeRow);
                label = Label(go.transform, "Label", caption, FontSizeBody, TextAnchor.MiddleLeft,
                              GardenPalette.TextPrimary, true);
                var symbolFlex = label.gameObject.AddComponent<LayoutElement>();
                symbolFlex.flexibleWidth = 1f;
            }
            else if (icon == null)
            {
                var padded = VerticalList(go, 0f, new RectOffset(10, 10, 4, 4));
                padded.childAlignment = TextAnchor.MiddleCenter;
                label = Label(go.transform, "Label", caption, FontSizeBody, TextAnchor.MiddleCenter,
                              GardenPalette.TextPrimary, true);
            }
            else
            {
                // Co icon thi icon dung truoc, chu chay tu trai sang: hai thu thanh mot khoi doc duoc.
                // childForceExpandHeight phai tat, neu khong o anh bi keo cao bang ca nut.
                var row = HorizontalList(go, 8f, new RectOffset(10, 10, 4, 4));
                row.childAlignment = TextAnchor.MiddleLeft;
                row.childForceExpandWidth = false;
                row.childForceExpandHeight = false;
                iconImage = Icon(go.transform, "Icon", icon, IconSizeRow);
                label = Label(go.transform, "Label", caption, FontSizeBody, TextAnchor.MiddleLeft,
                              GardenPalette.TextPrimary, true);
                var labelFlex = label.gameObject.AddComponent<LayoutElement>();
                labelFlex.flexibleWidth = 1f;
            }

            var cache = go.AddComponent<UiButtonStyle>();
            cache.Background = image;
            cache.Caption = label;
            cache.Icon = iconImage;
            cache.Symbol = symbolLabel;
            cache.BaseColor = baseColor;
            cache.BaseTextColor = baseTextColor;
            label.color = baseTextColor;

            // Chi dat san 44 px. Khong dat preferredHeight, neu khong no de len chieu cao
            // that ma layout group vua tinh duoc tu nhan.
            var layout = go.AddComponent<LayoutElement>();
            layout.minHeight = ButtonHeight;
            return button;
        }

        /// <summary>
        /// Nut chi rong bang noi dung cua no.
        ///
        /// Nhan chu ben trong nut duoc dat flexibleWidth = 1 de chu can duoc giua khi nut bi keo
        /// rong. Nhung layout group cua nut lai bao flexibleWidth cua chinh no bang tong cua cac
        /// con, nen con so 1 do noi len thanh flexibleWidth cua ca cai nut — va Unity chia cho
        /// trong cho moi thu co flexibleWidth duong, khong quan tam childForceExpandWidth da tat
        /// hay chua. Ep ve 0 la cach duy nhat de nut dung yen o be rong cua no.
        /// </summary>
        public static void HugContent(Button button)
        {
            if (button == null) return;
            var layout = button.GetComponent<LayoutElement>();
            if (layout != null) layout.flexibleWidth = 0f;
        }

        public static void SetButtonCaption(Button button, string caption)
        {
            if (button == null) return;
            var cache = button.GetComponent<UiButtonStyle>();
            if (cache != null && cache.Caption != null) cache.Caption.text = caption;
        }

        /// <summary>Nut disabled van phai noi duoc ly do — nen chu cung phai mo di, khong chi nen.</summary>
        public static void SetButtonState(Button button, ButtonState state)
        {
            if (button == null) return;
            var cache = button.GetComponent<UiButtonStyle>();

            Color fill;
            Color text;
            Color iconTint;
            switch (state)
            {
                case ButtonState.Selected:
                    fill = GardenPalette.ButtonActive;
                    text = GardenPalette.TextPrimary;
                    iconTint = GardenPalette.IconTint;
                    break;
                case ButtonState.Disabled:
                    fill = GardenPalette.ButtonDisabled;
                    text = GardenPalette.TextDisabled;
                    iconTint = GardenPalette.IconMuted;
                    break;
                default:
                    fill = cache != null ? cache.BaseColor : GardenPalette.ButtonNormal;
                    text = cache != null ? cache.BaseTextColor : GardenPalette.TextOnPrimary;
                    iconTint = GardenPalette.IconTint;
                    break;
            }

            button.interactable = state == ButtonState.Normal;
            if (cache == null) return;
            if (cache.Background != null) cache.Background.color = fill;
            if (cache.Caption != null) cache.Caption.color = text;
            if (cache.Icon != null) cache.Icon.color = iconTint;
            if (cache.Symbol != null) cache.Symbol.color = iconTint;
        }

        /// <summary>
        /// An hoac hien glyph dan dau cua nut.
        ///
        /// Mot nut co gia tien mang icon dong xu, nhung cung cai nut do co luc khong con gia nao
        /// de noi ("Dat con tot") — luc do de icon lai la noi doi. Nut phai duoc dung voi symbol
        /// tu dau thi moi co glyph de an.
        /// </summary>
        public static void SetButtonSymbolVisible(Button button, bool visible)
        {
            if (button == null) return;
            var cache = button.GetComponent<UiButtonStyle>();
            if (cache == null || cache.Symbol == null) return;
            cache.Symbol.gameObject.SetActive(visible);
        }

        public static void SetInteractable(Button button, bool interactable)
        {
            SetButtonState(button, interactable ? ButtonState.Normal : ButtonState.Disabled);
        }

        public static VerticalLayoutGroup VerticalList(GameObject target, float spacing, RectOffset padding)
        {
            var layout = target.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = padding;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            return layout;
        }

        /// <summary>
        /// Luoi o co dinh so cot. Dung cho tui do: mat hang nao cung mot co o, va vi tri cua o
        /// on dinh giua hai lan mo nen nguoi choi nho duoc cho ma khong phai doc lai tung dong.
        /// </summary>
        public static GridLayoutGroup Grid(GameObject target, Vector2 cellSize, float spacing, int columns)
        {
            var layout = target.AddComponent<GridLayoutGroup>();
            layout.cellSize = cellSize;
            layout.spacing = new Vector2(spacing, spacing);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = columns;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.padding = new RectOffset(0, 0, 0, 0);
            return layout;
        }

        public static HorizontalLayoutGroup HorizontalList(GameObject target, float spacing, RectOffset padding)
        {
            var layout = target.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = padding;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;
            return layout;
        }

        /// <summary>Thanh tien do don gian: ranh toi bo goc, phan da chay to mau.</summary>
        public static Image ProgressBar(Transform parent, string name, Color fillColor, out Image fill)
        {
            var background = Panel(parent, name, GardenPalette.TrackEmpty, RadiusTrack);

            var fillImage = Panel(background.transform, "Fill", fillColor, RadiusTrack);
            var rect = Rect(fillImage.gameObject);
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            fill = fillImage;
            return background;
        }

        public static void SetProgress(Image fill, float value01)
        {
            if (fill == null) return;
            fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(value01), 1f);
        }
    }
}
