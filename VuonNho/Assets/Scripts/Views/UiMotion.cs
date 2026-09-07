using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VuonNho.Views
{
    /// <summary>
    /// Lop chuyen dong cua giao dien: mo bang, bam nut, so dem, thanh tien do.
    ///
    /// Truoc day moi thay doi deu tuc thi — `SetActive` bat tat, so nhay mot phat tu 0 sang 4.569,
    /// thanh tien do gan thang toi vi tri moi. Mat nguoi doc chuyen dong tot hon doc mot khung
    /// hinh dung yen: mot cai bang truot vao noi cho nguoi choi biet no **den tu dau** va vi the
    /// dong lai thi ve dau, con mot con so chay len noi cho ho biet **vua cong bao nhieu**.
    ///
    /// Ba luat cua ca lop nay:
    ///
    /// 1. **Khong bao gio chan.** Moi thu van bam duoc ngay tu khung hinh dau cua chuyen dong;
    ///    chuyen dong chi la lop son, khong phai mot cai cong phai doi.
    /// 2. **Dung `unscaledDeltaTime`.** Cong cu tua thoi gian cua ban dev doi `Time.timeScale`,
    ///    va giao dien khong duoc cham di theo no.
    /// 3. **Ket thuc dung diem dat.** Moi tween ghi thang gia tri cuoi o buoc cuoi chu khong de
    ///    lai phan le cua phep noi suy — mot thanh tien do dung o 0,997 la mot loi nhin thay duoc.
    /// </summary>
    public static class UiMotion
    {
        // --- thoi luong, giay. Ba bac thoi luong cho ba muc "xa" cua chuyen dong.
        public const float DurationQuick = 0.11f;
        public const float DurationBase = 0.17f;
        public const float DurationPanel = 0.22f;

        /// <summary>So dem chay lau hon: mat can kip doc con so dang chay.</summary>
        public const float DurationCounter = 0.45f;

        /// <summary>Bang truot vao tu ben phai bao nhieu pixel.</summary>
        public const float PanelSlide = 26f;

        /// <summary>Nut bi an xuong con bao nhieu phan.</summary>
        public const float PressScale = 0.94f;

        static Runner _runner;
        static readonly List<Tween> Live = new List<Tween>();

        /// <summary>
        /// Con tween nao dang chay khong.
        ///
        /// Bo QA doc cai nay de doi chuyen dong dung han roi moi do bo cuc: do giua chung thi mot
        /// cai bang dang truot vao se nam sai cho, va phep do "bang co chan click xuong vuon
        /// khong" se bao sai theo. Doi nhu vay cung co nghia bo QA kiem **dung duong chay that**
        /// chu khong kiem mot che do khong chuyen dong rieng.
        /// </summary>
        public static bool Busy
        {
            get { return Live.Count > 0; }
        }

        /// <summary>
        /// Tat chuyen dong. Dat truoc khi dung UI trong mot ban chay khong co nguoi xem.
        /// Moi tween van chay het nhung nhay thang toi diem dat o khung hinh dau.
        /// </summary>
        public static bool Instant;

        // ------------------------------------------------------------------ vong chay

        sealed class Tween
        {
            public GameObject Owner;
            public float Elapsed;
            public float Duration;
            public Action<float> Step;
            public Action Done;
        }

        /// <summary>MonoBehaviour an, tu sinh ra o lan dung dau tien.</summary>
        sealed class Runner : MonoBehaviour
        {
            void Update()
            {
                for (int i = Live.Count - 1; i >= 0; i--)
                {
                    var tween = Live[i];
                    // Chu so huu bi huy giua chung — dong bang chang han. Bo tween di, khong goi
                    // Step nua: Step cham vao Text va Image cua chinh cai vua bi huy.
                    if (tween.Owner == null)
                    {
                        Live.RemoveAt(i);
                        continue;
                    }

                    tween.Elapsed += Time.unscaledDeltaTime;
                    float t = tween.Duration <= 0f ? 1f : Mathf.Clamp01(tween.Elapsed / tween.Duration);
                    tween.Step(t);
                    if (t < 1f) continue;

                    Live.RemoveAt(i);
                    if (tween.Done != null) tween.Done();
                }
            }
        }

        static void Run(GameObject owner, float duration, Action<float> step, Action done = null)
        {
            if (owner == null) return;

            // Mot doi tuong chi co mot tween cua cung mot loai dang chay. Mo roi dong roi mo lai
            // that nhanh se de lai hai tween danh nhau tren cung mot gia tri alpha.
            for (int i = Live.Count - 1; i >= 0; i--)
                if (Live[i].Owner == owner) Live.RemoveAt(i);

            if (Instant || duration <= 0f)
            {
                step(1f);
                if (done != null) done();
                return;
            }

            if (_runner == null)
            {
                var host = new GameObject("UiMotion");
                UnityEngine.Object.DontDestroyOnLoad(host);
                host.hideFlags = HideFlags.HideAndDontSave;
                _runner = host.AddComponent<Runner>();
            }

            step(0f);
            Live.Add(new Tween { Owner = owner, Duration = duration, Step = step, Done = done });
        }

        // ------------------------------------------------------------------ ham noi suy

        /// <summary>Cham o hai dau, nhanh o giua. Nhip mac dinh cua moi chuyen dong o day.</summary>
        public static float EaseInOut(float t)
        {
            return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
        }

        /// <summary>Vot nhanh roi cham dan. Dung cho thu **di vao**: no toi noi som, roi lang lai.</summary>
        public static float EaseOut(float t)
        {
            return 1f - Mathf.Pow(1f - t, 3f);
        }

        /// <summary>Vuot qua diem dat mot chut roi quay lai. Dung cho phan hoi khi bam.</summary>
        public static float EaseBack(float t)
        {
            const float overshoot = 1.7f;
            float u = t - 1f;
            return u * u * ((overshoot + 1f) * u + overshoot) + 1f;
        }

        // ------------------------------------------------------------------ bang

        static CanvasGroup GroupOf(GameObject target)
        {
            var group = target.GetComponent<CanvasGroup>();
            return group != null ? group : target.AddComponent<CanvasGroup>();
        }

        /// <summary>
        /// Mo mot bang: hien ra, truot vao tu ben phai va no nhe tu 98%.
        ///
        /// Bat `SetActive` ngay tu dau chu khong doi het chuyen dong: bang phai bam duoc lien tay,
        /// va moi doan ma khac trong HUD doc `activeSelf` de biet bang nao dang mo.
        /// </summary>
        public static void ShowPanel(GameObject panel)
        {
            if (panel == null) return;
            panel.SetActive(true);
            var rect = panel.transform as RectTransform;
            var group = GroupOf(panel);
            group.blocksRaycasts = true;
            group.interactable = true;

            Vector2 home = rect != null ? rect.anchoredPosition : Vector2.zero;
            Run(panel, DurationPanel, delegate(float t)
            {
                float e = EaseOut(t);
                group.alpha = e;
                if (rect != null)
                    rect.anchoredPosition = home + new Vector2(PanelSlide * (1f - e), 0f);
                panel.transform.localScale = Vector3.one * Mathf.Lerp(0.98f, 1f, e);
            },
            delegate
            {
                group.alpha = 1f;
                if (rect != null) rect.anchoredPosition = home;
                panel.transform.localScale = Vector3.one;
            });
        }

        /// <summary>
        /// Dong mot bang: mo di va truot ra, roi moi tat han.
        ///
        /// Tat `blocksRaycasts` ngay khung hinh dau: mot cai bang dang mo dan van chan click la
        /// mot cu bam bi nuot ma khong ai hieu vi sao.
        /// </summary>
        public static void HidePanel(GameObject panel)
        {
            if (panel == null || !panel.activeSelf) return;
            var rect = panel.transform as RectTransform;
            var group = GroupOf(panel);
            group.blocksRaycasts = false;
            group.interactable = false;

            Vector2 home = rect != null ? rect.anchoredPosition : Vector2.zero;
            Run(panel, DurationBase, delegate(float t)
            {
                float e = EaseInOut(t);
                group.alpha = 1f - e;
                if (rect != null) rect.anchoredPosition = home + new Vector2(PanelSlide * e, 0f);
            },
            delegate
            {
                if (rect != null) rect.anchoredPosition = home;
                group.alpha = 1f;
                panel.transform.localScale = Vector3.one;
                panel.SetActive(false);
            });
        }

        /// <summary>Hien mot the nho: mo dan va no tu 92%. Dung cho toast va the ten.</summary>
        public static void Pop(GameObject target)
        {
            if (target == null) return;
            target.SetActive(true);
            var group = GroupOf(target);
            Run(target, DurationBase, delegate(float t)
            {
                float e = EaseBack(t);
                group.alpha = Mathf.Clamp01(t * 2f);
                target.transform.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, e);
            },
            delegate
            {
                group.alpha = 1f;
                target.transform.localScale = Vector3.one;
            });
        }

        public static void FadeOutAndHide(GameObject target)
        {
            if (target == null || !target.activeSelf) return;
            var group = GroupOf(target);
            Run(target, DurationQuick, delegate(float t) { group.alpha = 1f - t; },
                delegate
                {
                    group.alpha = 1f;
                    target.SetActive(false);
                });
        }

        // ------------------------------------------------------------------ nut

        /// <summary>
        /// Phan hoi khi bam: nut an xuong roi bat lai.
        ///
        /// Unity co san ColorTint doi mau nut, nhung doi mau khong phai la mot cu **cham**. Cai
        /// bao cho tay biet cu bam da an la nut lun xuong duoi ngon tay roi tro lai.
        ///
        /// Gan vao chinh `Button` qua EventTrigger chu khong qua `onClick`: `onClick` chi ban khi
        /// tha chuot **trong** nut, con phan hoi phai co ngay luc nhan xuong.
        /// </summary>
        public static void AttachPress(Button button)
        {
            if (button == null) return;
            var target = button.transform;
            var trigger = button.gameObject.GetComponent<EventTrigger>();
            if (trigger == null) trigger = button.gameObject.AddComponent<EventTrigger>();

            var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            down.callback.AddListener(delegate
            {
                if (!button.IsInteractable()) return;
                target.localScale = Vector3.one * PressScale;
            });
            trigger.triggers.Add(down);

            var up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            up.callback.AddListener(delegate { ReleasePress(button); });
            trigger.triggers.Add(up);

            // Nhan xuong roi keo ra ngoai nut roi tha: PointerUp khong ban, va nut se ket o trang
            // thai bi an xuong mai mai neu khong bat ca truong hop nay.
            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(delegate { ReleasePress(button); });
            trigger.triggers.Add(exit);
        }

        static void ReleasePress(Button button)
        {
            if (button == null) return;
            var target = button.transform;
            float from = target.localScale.x;
            if (Mathf.Approximately(from, 1f)) return;
            Run(button.gameObject, DurationQuick,
                delegate(float t) { target.localScale = Vector3.one * Mathf.Lerp(from, 1f, EaseBack(t)); },
                delegate { target.localScale = Vector3.one; });
        }

        // ------------------------------------------------------------------ so va thanh

        /// <summary>
        /// Cho mot con so chay toi gia tri moi thay vi nhay mot phat.
        ///
        /// Nho gia tri dang hien tren chinh `Text` de goi lai nhieu lan khong lam so giat: HUD
        /// chay `Refresh` moi khung hinh, nen neu moi lan goi deu bat dau lai tu dau thi con so
        /// se dung yen o diem xuat phat.
        /// </summary>
        public static void CountTo(Text label, long value, string format = "N0")
        {
            if (label == null) return;

            long shown;
            if (!Counters.TryGetValue(label, out shown))
            {
                // Lan dau thay con so nay thi khong chay: mo game len ma so xu bo tu 0 len la mot
                // hieu ung khong ai xin, va no lap lai moi lan doi canh.
                Counters[label] = value;
                label.text = value.ToString(format);
                return;
            }
            if (shown == value) return;

            Counters[label] = value;
            long from = shown;
            Run(label.gameObject, DurationCounter,
                delegate(float t)
                {
                    long now = from + (long)Math.Round((value - from) * (double)EaseOut(t));
                    label.text = now.ToString(format);
                },
                delegate { label.text = value.ToString(format); });
        }

        static readonly Dictionary<Text, long> Counters = new Dictionary<Text, long>();

        /// <summary>
        /// Thanh tien do truot toi muc moi.
        ///
        /// Khac cac tween khac o cho no **khong dat thoi luong**: no duoi theo muc dich moi khung
        /// hinh. Thanh mua vu nhich len lien tuc, va mot tween co thoi luong se bi khoi dong lai
        /// moi khung hinh roi khong bao gio di duoc quang nao.
        /// </summary>
        public static void GlideFill(Image fill, float target01)
        {
            if (fill == null) return;
            float goal = Mathf.Clamp01(target01);
            if (Instant)
            {
                UiFactory.SetProgress(fill, goal);
                return;
            }
            // Doc va ghi qua anchorMax.x, dung cho ma UiFactory.SetProgress dung: hai cach do muc
            // day khac nhau tren cung mot thanh se danh nhau moi khung hinh.
            float now = fill.rectTransform.anchorMax.x;
            // Tien mot phan co dinh cua quang con lai moi giay: gan dich thi cham lai, nen khong
            // co cu dung dot nao o cuoi.
            float step = 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime);
            float next = Mathf.Lerp(now, goal, step);
            UiFactory.SetProgress(fill, Mathf.Abs(goal - next) < 0.002f ? goal : next);
        }
    }
}
