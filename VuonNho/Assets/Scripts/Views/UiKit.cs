using UnityEngine;
using UnityEngine.UI;

namespace VuonNho.Views
{
    /// <summary>
    /// Bo component dung chung cua cac bang thong tin.
    ///
    /// Truoc day nam bang cua ban mo phong khoi nghiep tra deu dung mot khoi chu nhieu dong cho
    /// moi thu: bang sinh hoa bon mua, bang phan hang thu hai, khung tien phat, tinh trang khoan
    /// vay. Doc duoc, nhung **doc nhu mot trang tai lieu** — mat khong bat duoc con so nao truoc,
    /// khong biet cai nao dang tot cai nao dang hong, va phai doc het ca doan moi tim ra dong
    /// minh can.
    ///
    /// Sau component o day thay cho khoi chu do. Moi cai lam mot viec:
    ///
    /// - <see cref="Section"/> — chia bang thanh tung phan doc duoc rieng.
    /// - <see cref="Card"/> — gom nhung dong thuoc ve nhau vao mot khoi.
    /// - <see cref="StatRow"/> — nhan ben trai, con so ben phai, mat quet doc mot cot so.
    /// - <see cref="Badge"/> — mot chu ve trang thai, mang mau cua trang thai do.
    /// - <see cref="Meter"/> — mot con so tren thang cua no, kem khoang chuan.
    /// - <see cref="Callout"/> — cau giai thich, tach khoi so lieu bang mot vach mau.
    ///
    /// Ca sau deu nhan <see cref="UiTone"/> chu khong nhan mau: cho goi noi "cai nay dang sai",
    /// con mau la thu <see cref="UiTokens"/> suy ra.
    /// </summary>
    public static class UiKit
    {
        // ------------------------------------------------------------------ khung

        /// <summary>Tieu de mot phan, kem duong ke mo ben duoi.</summary>
        public static Text Section(Transform parent, string title)
        {
            var holder = UiFactory.Node(parent, "Section_" + title);
            var list = UiFactory.VerticalList(holder, UiTokens.Space1, UiTokens.NoPadding);
            list.childForceExpandHeight = false;

            var label = UiFactory.Label(holder.transform, "Title", title, UiTokens.TextTitle,
                                        TextAnchor.MiddleLeft, GardenPalette.TextPrimary, true);

            var rule = UiFactory.Panel(holder.transform, "Rule", GardenPalette.PanelDivider);
            rule.raycastTarget = false;
            var ruleSize = rule.gameObject.AddComponent<LayoutElement>();
            ruleSize.minHeight = 1f;
            ruleSize.preferredHeight = 1f;
            return label;
        }

        /// <summary>Mot khoi gom nhung dong thuoc ve nhau.</summary>
        public static Transform Card(Transform parent, string name, UiTone tone = UiTone.Neutral)
        {
            var card = UiFactory.Panel(parent, name, UiTokens.FillOf(tone), UiTokens.RadiusCard);
            var list = UiFactory.VerticalList(card.gameObject, UiTokens.Space2, UiTokens.CardPadding);
            list.childForceExpandHeight = false;
            return card.transform;
        }

        /// <summary>Chu giai thich. Nho hon, nhat hon, khong bao gio mang con so quan trong.</summary>
        public static Text Note(Transform parent, string name, string text)
        {
            return UiFactory.Label(parent, name, text, UiTokens.TextMeta,
                                   TextAnchor.UpperLeft, GardenPalette.TextMuted);
        }

        // ------------------------------------------------------------------ hang so lieu

        /// <summary>Mot hang so lieu da dung xong. Giu lai hai o chu de cap nhat sau.</summary>
        public sealed class Row
        {
            public GameObject Node;
            public Text Label;
            public Text Value;

            public void Set(string value, UiTone tone = UiTone.Neutral)
            {
                if (Value == null) return;
                Value.text = value;
                Value.color = UiTokens.TextOf(tone);
            }

            public void SetVisible(bool visible)
            {
                if (Node != null && Node.activeSelf != visible) Node.SetActive(visible);
            }
        }

        /// <summary>
        /// Nhan ben trai, con so ben phai.
        ///
        /// Con so canh phai chu khong canh trai: xep chong len nhau thi ca cot so thang hang o
        /// dau don vi, va mat quet mot cot nhu vay nhanh hon doc tung dong.
        /// </summary>
        public static Row StatRow(Transform parent, string name, string label, string value,
                                  UiTone tone)
        {
            var row = StatRow(parent, name, label);
            row.Set(value, tone);
            return row;
        }

        public static Row StatRow(Transform parent, string name, string label, string value = "")
        {
            var node = UiFactory.Node(parent, "Row_" + name);
            var list = UiFactory.HorizontalList(node, UiTokens.Space2, UiTokens.NoPadding);
            list.childForceExpandWidth = false;
            list.childAlignment = TextAnchor.MiddleLeft;
            var size = node.AddComponent<LayoutElement>();
            size.minHeight = UiTokens.RowHeight;

            var labelText = UiFactory.Label(node.transform, "Label", label, UiTokens.TextMeta,
                                            TextAnchor.MiddleLeft, GardenPalette.TextMuted);
            var labelFlex = labelText.gameObject.AddComponent<LayoutElement>();
            labelFlex.flexibleWidth = 1f;

            var valueText = UiFactory.Label(node.transform, "Value", value, UiTokens.TextBody,
                                            TextAnchor.MiddleRight, GardenPalette.TextPrimary, true);
            var valueFlex = valueText.gameObject.AddComponent<LayoutElement>();
            valueFlex.flexibleWidth = 0f;
            // Khong go cung be ngang: "2.200 xu" va "6,5%–8,5%/năm" dai khac nhau han, ma o nao
            // cung phai vua chu cua no. Chi chan khong cho no gian ra an cho cua nhan.
            valueText.horizontalOverflow = HorizontalWrapMode.Wrap;

            return new Row { Node = node, Label = labelText, Value = valueText };
        }

        // ------------------------------------------------------------------ nhan trang thai

        /// <summary>Mot the chu nho mang mau cua trang thai. Be ngang do theo chinh chu.</summary>
        public static Text Badge(Transform parent, string name, string text, UiTone tone)
        {
            var chip = UiFactory.Panel(parent, "Badge_" + name, UiTokens.FillOf(tone), UiTokens.RadiusBadge);
            var list = UiFactory.HorizontalList(chip.gameObject, 0,
                new RectOffset((int)UiTokens.Space2, (int)UiTokens.Space2, 0, 0));
            list.childForceExpandHeight = true;
            var fitter = chip.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var size = chip.gameObject.AddComponent<LayoutElement>();
            size.minHeight = UiTokens.BadgeHeight;
            size.preferredHeight = UiTokens.BadgeHeight;
            size.flexibleWidth = 0f;

            var label = UiFactory.Label(chip.transform, "Text", text, UiTokens.TextBadge,
                                        TextAnchor.MiddleCenter, UiTokens.TextOf(tone), true);
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            return label;
        }

        /// <summary>Doi chu va mau cua mot the da dung. Truyen chinh Text ma Badge tra ve.</summary>
        public static void SetBadge(Text badgeLabel, string text, UiTone tone)
        {
            if (badgeLabel == null) return;
            badgeLabel.text = text;
            badgeLabel.color = UiTokens.TextOf(tone);
            var chip = badgeLabel.transform.parent != null
                ? badgeLabel.transform.parent.GetComponent<Image>() : null;
            if (chip != null) chip.color = UiTokens.FillOf(tone);
        }

        // ------------------------------------------------------------------ thanh do

        /// <summary>Mot thanh do da dung xong.</summary>
        public sealed class Gauge
        {
            public GameObject Node;
            public Text Caption;
            public Text Value;
            public Image Fill;
            public RectTransform Band;

            /// <summary>Dat muc day, 0..1. Thanh truot toi cho moi chu khong nhay.</summary>
            public void Set(float fraction01, UiTone tone)
            {
                if (Fill == null) return;
                Fill.color = tone == UiTone.Neutral ? GardenPalette.ButtonNormal : UiTokens.TextOf(tone);
                UiMotion.GlideFill(Fill, fraction01);
            }

            /// <summary>
            /// Ve khoang chuan len nen thanh, tinh theo cung thang voi muc day.
            ///
            /// Day la thu bien mot con so thanh mot cau tra loi: "255°C" khong noi gi ca, con
            /// "255°C, va vach dang nam trong vung sang" thi noi ngay la dang dung.
            /// </summary>
            public void SetBand(float low01, float high01)
            {
                if (Band == null) return;
                float lo = Mathf.Clamp01(Mathf.Min(low01, high01));
                float hi = Mathf.Clamp01(Mathf.Max(low01, high01));
                Band.anchorMin = new Vector2(lo, 0f);
                Band.anchorMax = new Vector2(hi, 1f);
                Band.offsetMin = Vector2.zero;
                Band.offsetMax = Vector2.zero;
            }
        }

        /// <summary>
        /// Mot con so tren thang cua no: nhan, gia tri, roi mot thanh co danh dau khoang chuan.
        /// </summary>
        public static Gauge Meter(Transform parent, string name, string caption)
        {
            var node = UiFactory.Node(parent, "Meter_" + name);
            var list = UiFactory.VerticalList(node, UiTokens.Space1, UiTokens.NoPadding);
            list.childForceExpandHeight = false;

            var head = UiFactory.Node(node.transform, "Head");
            var headList = UiFactory.HorizontalList(head, UiTokens.Space2, UiTokens.NoPadding);
            headList.childForceExpandWidth = false;
            headList.childAlignment = TextAnchor.MiddleLeft;
            head.AddComponent<LayoutElement>().minHeight = 24f;

            var captionText = UiFactory.Label(head.transform, "Caption", caption, UiTokens.TextMeta,
                                              TextAnchor.MiddleLeft, GardenPalette.TextMuted);
            captionText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            var valueText = UiFactory.Label(head.transform, "Value", "", UiTokens.TextBody,
                                            TextAnchor.MiddleRight, GardenPalette.TextPrimary, true);
            // Gia tri cua thanh do luon ngan — "255°C", "17 phút", "+1.240 xu" — nen no khong bao
            // gio duoc gay dong. Khong dat minWidth thi layout co the bop no con vai chuc pixel va
            // "255°C" xuong thanh "255°" roi "C" o dong duoi.
            var valueSize = valueText.gameObject.AddComponent<LayoutElement>();
            valueSize.flexibleWidth = 0f;
            valueSize.minWidth = 96f;
            valueText.horizontalOverflow = HorizontalWrapMode.Overflow;

            var track = UiFactory.Panel(node.transform, "Track", GardenPalette.TrackEmpty,
                                        UiTokens.RadiusTrack);
            track.raycastTarget = false;
            var trackSize = track.gameObject.AddComponent<LayoutElement>();
            trackSize.minHeight = UiTokens.TrackHeight;
            trackSize.preferredHeight = UiTokens.TrackHeight;

            // Khoang chuan nam **duoi** vach muc day: no la nen de doc vach, khong phai thu de len.
            var band = UiFactory.Panel(track.transform, "Band", GardenPalette.ButtonActive,
                                       UiTokens.RadiusTrack);
            band.raycastTarget = false;
            var bandRect = band.rectTransform;
            bandRect.anchorMin = new Vector2(0f, 0f);
            bandRect.anchorMax = new Vector2(0f, 1f);
            bandRect.offsetMin = Vector2.zero;
            bandRect.offsetMax = Vector2.zero;

            var fill = UiFactory.Panel(track.transform, "Fill", GardenPalette.ButtonNormal,
                                       UiTokens.RadiusTrack);
            fill.raycastTarget = false;
            var fillRect = fill.rectTransform;
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            return new Gauge
            {
                Node = node, Caption = captionText, Value = valueText,
                Fill = fill, Band = bandRect
            };
        }

        // ------------------------------------------------------------------ ghi chu co vach

        /// <summary>
        /// Mot doan giai thich, tach khoi so lieu bang mot vach mau ben trai.
        ///
        /// Vach mau la thu noi cho mat biet "day khong phai so lieu, day la loi giai thich" ma
        /// khong ton mot dong chu nao de noi dieu do.
        /// </summary>
        public static Text Callout(Transform parent, string name, string text,
                                   UiTone tone = UiTone.Neutral)
        {
            var node = UiFactory.Node(parent, "Callout_" + name);
            var list = UiFactory.HorizontalList(node, UiTokens.Space3, UiTokens.NoPadding);
            list.childForceExpandWidth = false;
            list.childAlignment = TextAnchor.UpperLeft;

            var bar = UiFactory.Panel(node.transform, "Bar", UiTokens.TextOf(tone), 2);
            bar.raycastTarget = false;
            var barSize = bar.gameObject.AddComponent<LayoutElement>();
            barSize.minWidth = UiTokens.AccentBarWidth;
            barSize.preferredWidth = UiTokens.AccentBarWidth;
            barSize.flexibleWidth = 0f;

            var label = UiFactory.Label(node.transform, "Text", text, UiTokens.TextMeta,
                                        TextAnchor.UpperLeft, GardenPalette.TextMuted);
            label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            return label;
        }
    }
}
