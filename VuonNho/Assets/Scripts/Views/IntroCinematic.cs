using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VuonNho.Core;

namespace VuonNho.Views
{
    /// <summary>
    /// Doan mo man cua muc 6 trong ban mo phong: bon phan canh, moi canh mot khoi chu tren nen
    /// toi, mo dan vao va mo dan ra theo dung nhip cua kich ban.
    ///
    /// Hinh ve 2D hand-drawn va am thanh ASMR chua co, nen ban nay la mot **ban doc kich ban**:
    /// no giu nguyen thu tu, cau phu de va do dai cua tung canh, va no hien ca dong "hinh anh" va
    /// "am thanh" de nguoi lam art doc duoc minh phai ve gi vao day. Khi co art thi thay khoi chu
    /// bang khoi anh, nhip khong doi vi nhip nam o <see cref="IntroScene.DurationMs"/> trong Core.
    ///
    /// Bo qua duoc bat cu luc nao, va chi chay mot lan cho moi van
    /// (<see cref="GameState.IntroSeen"/>).
    /// </summary>
    public sealed class IntroCinematic : MonoBehaviour
    {
        /// <summary>Bao lau de mo dan vao va mo dan ra o hai dau moi canh.</summary>
        const float FadeSeconds = 0.9f;

        /// <summary>Cho nguoi choi bam bo qua ma khong bam trung vao vuon phia sau.</summary>
        const float SkipButtonHeight = 40f;

        GameSession _session;
        Canvas _canvas;
        CanvasGroup _group;
        Text _timeCode, _title, _visual, _audio, _subtitle, _progress;
        List<IntroScene> _scenes;
        int _index = -1;
        float _sceneElapsed;
        bool _running;

        public bool Running { get { return _running; } }

        /// <summary>
        /// Bat doan mo man neu van nay chua xem. Da xem roi thi khong dung gi va tra ve false —
        /// nguoi choi mo lai game lan thu hai khong phai xem lai mot phut phim.
        /// </summary>
        public bool Begin(GameSession session)
        {
            _session = session;
            if (session == null || session.State == null || session.State.IntroSeen) return false;

            _scenes = IntroStoryboard.Scenes();
            if (_scenes.Count == 0) return false;

            BuildUi();
            _index = 0;
            _sceneElapsed = 0f;
            _running = true;
            ShowScene(_scenes[0]);
            return true;
        }

        void BuildUi()
        {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Tren moi thu, ke ca toast: doan mo man la lop tren cung khi no dang chay.
            _canvas.sortingOrder = 500;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1366f, 768f);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();
            _group = gameObject.AddComponent<CanvasGroup>();

            var backdrop = UiFactory.Panel(transform, "Backdrop", new Color(0.07f, 0.08f, 0.06f, 1f));
            UiFactory.Stretch(backdrop.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var column = UiFactory.Node(backdrop.transform, "Column");
            UiFactory.Stretch(UiFactory.Rect(column), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                              new Vector2(-380f, -230f), new Vector2(380f, 230f));
            var list = UiFactory.VerticalList(column, 14f, new RectOffset(0, 0, 0, 0));
            list.childAlignment = TextAnchor.MiddleLeft;

            var cream = new Color(0.94f, 0.91f, 0.82f);
            var muted = new Color(0.68f, 0.68f, 0.60f);

            _timeCode = UiFactory.Label(column.transform, "TimeCode", "", 16,
                                        TextAnchor.MiddleLeft, muted);
            _title = UiFactory.Label(column.transform, "Title", "", 30,
                                     TextAnchor.MiddleLeft, cream, true);
            _visual = UiFactory.Label(column.transform, "Visual", "", 18, TextAnchor.UpperLeft, cream);
            _audio = UiFactory.Label(column.transform, "Audio", "", 16, TextAnchor.UpperLeft, muted);
            _subtitle = UiFactory.Label(column.transform, "Subtitle", "", 21, TextAnchor.UpperLeft,
                                        new Color(0.85f, 0.79f, 0.55f), true);
            _progress = UiFactory.Label(column.transform, "Progress", "", 14, TextAnchor.MiddleLeft, muted);

            var skip = UiFactory.TextButton(backdrop.transform, "Skip", "Bỏ qua đoạn mở màn",
                                            Finish, UiFactory.ButtonStyle.Quiet);
            var skipRect = UiFactory.Rect(skip.gameObject);
            skipRect.anchorMin = skipRect.anchorMax = skipRect.pivot = new Vector2(1f, 0f);
            skipRect.anchoredPosition = new Vector2(-28f, 28f);
            skipRect.sizeDelta = new Vector2(220f, SkipButtonHeight);
        }

        void ShowScene(IntroScene scene)
        {
            _timeCode.text = scene.TimeCode + "  ·  " + scene.Camera;
            _title.text = "Phân cảnh " + scene.Index + " — " + scene.Title;
            _visual.text = scene.Visual;
            _audio.text = "Âm thanh · " + scene.Audio;
            _subtitle.text = "“" + scene.Subtitle + "”";
            _progress.text = scene.Index + "/" + _scenes.Count + "  ·  " +
                             IntroStoryboard.GameTitle + "  ·  " + IntroStoryboard.GameSubtitle;
        }

        void Update()
        {
            if (!_running) return;

            // Bam chuot hay Space thi sang canh ke tiep. Doc kich ban nhanh hon nhip phim la mot
            // nhu cau that, va cho no khac han voi viec bo qua ca doan.
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Space) ||
                Input.GetKeyDown(KeyCode.Return))
            {
                NextScene();
                return;
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Finish();
                return;
            }

            var scene = _scenes[_index];
            _sceneElapsed += Time.unscaledDeltaTime;
            float seconds = scene.DurationMs / 1000f;

            // Mo dan vao o dau canh, mo dan ra o cuoi. Giua thi ro han.
            float alpha = 1f;
            if (_sceneElapsed < FadeSeconds) alpha = _sceneElapsed / FadeSeconds;
            else if (_sceneElapsed > seconds - FadeSeconds)
                alpha = Mathf.Max(0f, (seconds - _sceneElapsed) / FadeSeconds);
            _group.alpha = Mathf.Clamp01(alpha);

            if (_sceneElapsed >= seconds) NextScene();
        }

        void NextScene()
        {
            _index++;
            if (_index >= _scenes.Count)
            {
                Finish();
                return;
            }
            _sceneElapsed = 0f;
            ShowScene(_scenes[_index]);
        }

        /// <summary>
        /// Ket thuc doan mo man: ghi lai la da xem roi go canvas.
        ///
        /// Ghi ngay khi ket thuc chu khong doi autosave: tat game giua doan roi mo lai ma phai
        /// xem lai tu dau la mot cach lam mat thoi gian cua nguoi choi.
        /// </summary>
        void Finish()
        {
            if (!_running) return;
            _running = false;
            if (_session != null) _session.MarkIntroSeen();
            Destroy(gameObject);
        }
    }
}
