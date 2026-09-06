using System;
using UnityEngine;

namespace VuonNho.Views
{
    /// <summary>
    /// Cong cu QA: chup man hinh tu chinh ban build roi thoat, de kiem tra hinh anh that
    /// tren may chuan thay vi tin vao anh render trong Editor.
    ///
    /// VuonNho.exe -vuonnho-screenshot "D:\anh.png" -vuonnho-screenshot-delay 6
    ///
    /// Khong co tham so thi component nay khong lam gi.
    /// </summary>
    public sealed class QaScreenshot : MonoBehaviour
    {
        public const string PathArgument = "-vuonnho-screenshot";
        public const string DelayArgument = "-vuonnho-screenshot-delay";
        public const string PanelArgument = "-vuonnho-open-panel";

        string _outputPath;
        string _panelName;
        float _delaySeconds = 5f;
        float _captureAt = -1f;
        bool _captured;

        void Awake()
        {
            var arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length; i++)
            {
                if (arguments[i] == PathArgument && i + 1 < arguments.Length)
                    _outputPath = arguments[i + 1];
                else if (arguments[i] == DelayArgument && i + 1 < arguments.Length)
                    float.TryParse(arguments[i + 1], out _delaySeconds);
                else if (arguments[i] == PanelArgument && i + 1 < arguments.Length)
                    _panelName = arguments[i + 1];
            }

            if (string.IsNullOrEmpty(_outputPath))
            {
                enabled = false;
                return;
            }
            _captureAt = Time.realtimeSinceStartup + _delaySeconds;
        }

        void Update()
        {
            if (_captured || _captureAt < 0f || Time.realtimeSinceStartup < _captureAt) return;

            if (!string.IsNullOrEmpty(_panelName))
            {
                var hud = FindAnyObjectByType<GameHud>();
                if (hud != null) hud.OpenPanelByName(_panelName);
                _panelName = null;
                return;   // cho mot frame de panel kip dung xong
            }

            _captured = true;
            ScreenCapture.CaptureScreenshot(_outputPath);
            Debug.Log("[VuonNho] QA screenshot: " + _outputPath);
            // Cho vai frame de file duoc ghi xong roi moi thoat.
            Invoke("QuitAfterCapture", 2f);
        }

        void QuitAfterCapture()
        {
            Application.Quit();
        }
    }
}
