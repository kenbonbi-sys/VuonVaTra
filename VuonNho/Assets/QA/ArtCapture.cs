using System;
using System.Collections;
using System.IO;
using UnityEngine;
using VuonNho.Core;
using VuonNho.Views;

namespace VuonNho.ArtQa
{
    // Used only in the separate ArtReview scene/build. No GameSession, save, or PlayerPrefs.
    public sealed class ArtCapture : MonoBehaviour
    {
        public Camera Camera;
        public PlotView[] Plots;
        public MachineView Machine;
        public HelperView Helper;
        public GameObject[] Models;
        public GameObject Calibration;

        IEnumerator Start()
        {
            string output = Argument("-art-output");
            if (string.IsNullOrEmpty(output))
            {
                Debug.LogError("ArtReview requires -art-output <directory>.");
                Application.Quit(1);
                yield break;
            }
            Directory.CreateDirectory(output);
            Application.targetFrameRate = 60;
            var catalog = DefaultContent.Create();
            var state = GameState.CreateNew(catalog, 0);
            var simulation = new FarmSimulation(catalog);
            state.SimulationTimeMs = 6000;
            state.RobotUnlocked = true;
            for (int i = 0; i < state.Plots.Count; i++)
            {
                var plot = state.Plots[i];
                plot.Unlocked = i != 11;
                plot.CurrentCropId = catalog.Crops[i % 3].Id;
                plot.StartAtMs = 0;
                plot.FinishAtMs = i == 3 ? 60000 : 10000;
                plot.Phase = i == 11 ? PlotPhase.Locked : i == 7 ? PlotPhase.Empty
                    : (i <= 2 || i >= 8) ? PlotPhase.Ready : PlotPhase.Growing;
            }
            state.Machine.BatchRunning = true;
            state.Machine.BatchStartAtMs = 0;
            state.Machine.BatchFinishAtMs = 10000;
            foreach (var plot in Plots) plot.Render(state, simulation);
            Machine.Render(state, simulation);
            Helper.Render(state, simulation);
            yield return new WaitForSeconds(0.5f);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "garden-" + Screen.width + "x" + Screen.height + ".png"));
            yield return new WaitForSeconds(0.3f);

            // Demonstrate ready, growing, locked, and waiting visuals without advancing an economy.
            state.RobotUnlocked = false;
            state.Machine.BatchRunning = false;
            Machine.Render(state, simulation);
            Helper.Render(state, simulation);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "garden-waiting-" + Screen.width + "x" + Screen.height + ".png"));
            yield return new WaitForSeconds(0.3f);

            foreach (var root in gameObject.scene.GetRootGameObjects())
                if (root != gameObject && root != Camera.gameObject && root.GetComponent<Light>() == null)
                    root.SetActive(false);

            Camera.backgroundColor = new Color(0.949f, 0.918f, 0.835f, 1f);
            Camera.clearFlags = CameraClearFlags.SolidColor;
            Camera.transform.rotation = Quaternion.Euler(35f, -45f, 0f);
            var rt = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32);
            rt.Create();
            Camera.targetTexture = rt;
            Camera.aspect = 1f;
            Directory.CreateDirectory(Path.Combine(output, "models"));
            Directory.CreateDirectory(Path.Combine(output, "icons"));

            // Moi prefab trong danh sach deu duoc chup ca model lan icon; rieng cube calibration
            // (i == Models.Length) chi chup model. Prefab chua ton tai thi bo qua, khong dung lai.
            for (int i = 0; i <= Models.Length; i++)
            {
                var prefab = i == Models.Length ? Calibration : Models[i];
                if (prefab == null) continue;
                var instance = Instantiate(prefab);
                // Match the facing used by SceneFactory; these model roots are identity in the asset.
                if (prefab.name == "PF_Helper") instance.transform.rotation = Quaternion.Euler(0f, 135f, 0f);
                else if (prefab.name == "PF_TeaStation") instance.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                Frame(instance);
                yield return new WaitForEndOfFrame();
                WriteTexture(rt, Path.Combine(output, "models", prefab.name + ".png"));
                if (i < Models.Length)
                {
                    Camera.targetTexture = null;
                    var iconRt = new RenderTexture(256, 256, 24, RenderTextureFormat.ARGB32);
                    iconRt.Create();
                    Camera.targetTexture = iconRt;
                    yield return new WaitForEndOfFrame();
                    WriteTexture(iconRt, Path.Combine(output, "icons", "ICO_" + prefab.name.Replace("PF_", "") + ".png"));
                    Camera.targetTexture = rt;
                    iconRt.Release();
                    Destroy(iconRt);
                }
                Destroy(instance);
                yield return null;
            }
            Camera.targetTexture = null;
            rt.Release();
            Destroy(rt);
            File.WriteAllText(Path.Combine(output, "capture-complete.txt"),
                "Unity " + Application.unityVersion + "; actual Windows player render; no save/session opened.\n");
            Debug.Log("[ArtQA] Capture complete: " + output);
            Application.Quit();
        }

        void Frame(GameObject model)
        {
            var renderers = model.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            float halfWidth = 0, halfHeight = 0;
            for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                    for (int z = -1; z <= 1; z += 2)
                    {
                        var delta = Vector3.Scale(bounds.extents, new Vector3(x, y, z));
                        halfWidth = Mathf.Max(halfWidth, Mathf.Abs(Vector3.Dot(Camera.transform.right, delta)));
                        halfHeight = Mathf.Max(halfHeight, Mathf.Abs(Vector3.Dot(Camera.transform.up, delta)));
                    }
            Camera.orthographicSize = Mathf.Max(halfWidth, halfHeight) * 1.24f;
            Camera.transform.position = bounds.center - Camera.transform.forward * 12f;
        }

        static void WriteTexture(RenderTexture source, string path)
        {
            var previous = RenderTexture.active;
            RenderTexture.active = source;
            var texture = new Texture2D(source.width, source.height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Destroy(texture);
            RenderTexture.active = previous;
        }

        static string Argument(string key)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++) if (args[i] == key) return args[i + 1];
            return null;
        }
    }
}
