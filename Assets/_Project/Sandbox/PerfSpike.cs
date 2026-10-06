using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using StructureViewer.Infrastructure.Quality;
using UnityEngine;
using UnityEngine.Networking;

namespace StructureViewer.Sandbox
{
    // Throwaway B08 spike (delete before release): spawns N timber-like members as separate renderers
    // with one shared material and reports FPS, so a phone can answer "is one GameObject per member OK?".
    // URL parameters: ?count=1600&mode=xray, and ?bench to run a fixed sequence and POST results to /bench.
    public sealed class PerfSpike : MonoBehaviour
    {
        private const float WarmupSeconds = 2f;
        private const float MeasureSeconds = 8f;
        private static readonly int[] Counts = { 400, 800, 1600 };
        private static readonly (int Count, bool XRay)[] BenchSteps = { (800, false), (800, true), (1600, false), (1600, true) };

        [SerializeField] private Material _wood;
        [SerializeField] private Material _xray;
        [SerializeField] private Material _concrete;
        [SerializeField] private Material _sheathing;
        [SerializeField] private Mesh _cube;
        [SerializeField] private Camera _camera;
        [SerializeField] private int _count = 800;

        private readonly List<MeshRenderer> _members = new List<MeshRenderer>();
        private readonly List<float> _frameTimes = new List<float>(4096);
        private readonly List<string> _benchLines = new List<string>();
        private Transform _root;
        private bool _xrayMode;
        private bool _orbit = true;
        private bool _recording;
        private string _benchStatus;
        private float _yaw = -35f;
        private float _smoothedDelta = 1f / 60f;
        private float _worstDelta;
        private float _worstResetTime;
        private GUIStyle _label;
        private GUIStyle _button;

        [Serializable]
        private sealed class BenchReport
        {
            public string url;
            public string quality;
            public bool isMobile;
            public int screenWidth;
            public int screenHeight;
            public string gpu;
            public string os;
            public BenchStep[] steps;
        }

        [Serializable]
        private sealed class BenchStep
        {
            public int members;
            public string mode;
            public int frames;
            public float avgFps;
            public float p50Ms;
            public float p95Ms;
            public float worstMs;
        }

        private void Awake()
        {
            QualitySelector.Apply();
            var query = ParseQuery(UnityEngine.Application.absoluteURL);
            if (query.TryGetValue("count", out var countText) && int.TryParse(countText, out int count) && count > 0)
                _count = count;
            _xrayMode = query.TryGetValue("mode", out var mode) && mode == "xray";
            Spawn(_count);
            if (query.ContainsKey("bench"))
                StartCoroutine(RunBenchmark());
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (_recording)
                _frameTimes.Add(dt);
            _smoothedDelta = Mathf.Lerp(_smoothedDelta, dt, 0.05f);
            if (dt > _worstDelta)
                _worstDelta = dt;
            if (Time.unscaledTime > _worstResetTime)
            {
                _worstResetTime = Time.unscaledTime + 2f;
                _worstDelta = dt;
            }

            if (_orbit)
                _yaw += 12f * dt;
            var pivot = new Vector3(5f, 3f, 4f);
            var rotation = Quaternion.Euler(28f, _yaw, 0f);
            _camera.transform.SetPositionAndRotation(pivot - rotation * Vector3.forward * 17f, rotation);
        }

        private void OnGUI()
        {
            float scale = Mathf.Max(1f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            if (_label == null)
            {
                _label = new GUIStyle(GUI.skin.label) { fontSize = 16 };
                _label.normal.textColor = new Color(0.1f, 0.1f, 0.12f);
                _button = new GUIStyle(GUI.skin.button) { fontSize = 16 };
            }

            var quality = QualitySettings.names[QualitySettings.GetQualityLevel()];
            GUILayout.BeginArea(new Rect(10f, 10f, 380f, 600f));
            GUILayout.Label($"{1f / _smoothedDelta:F0} FPS  ({_smoothedDelta * 1000f:F1} ms, worst {_worstDelta * 1000f:F0} ms)", _label);
            GUILayout.Label($"{_members.Count} members · {(_xrayMode ? "X-ray" : "Opaque")} · {quality}", _label);
            GUILayout.Label($"{Screen.width}×{Screen.height} · mobile={UnityEngine.Application.isMobilePlatform}", _label);

            if (_benchStatus != null)
            {
                GUILayout.Label(_benchStatus, _label);
                foreach (var line in _benchLines)
                    GUILayout.Label(line, _label);
                GUILayout.EndArea();
                return;
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(_xrayMode ? "Opaque" : "X-ray", _button, GUILayout.Height(44f)))
                SetXRay(!_xrayMode);
            if (GUILayout.Button(_orbit ? "Pause" : "Orbit", _button, GUILayout.Height(44f)))
                _orbit = !_orbit;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            foreach (int count in Counts)
            {
                if (GUILayout.Button(count.ToString(), _button, GUILayout.Height(44f)))
                    Spawn(count);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private IEnumerator RunBenchmark()
        {
            var report = new BenchReport
            {
                url = UnityEngine.Application.absoluteURL,
                quality = QualitySettings.names[QualitySettings.GetQualityLevel()],
                isMobile = UnityEngine.Application.isMobilePlatform,
                screenWidth = Screen.width,
                screenHeight = Screen.height,
                gpu = SystemInfo.graphicsDeviceName,
                os = SystemInfo.operatingSystem,
                steps = new BenchStep[BenchSteps.Length]
            };

            for (int i = 0; i < BenchSteps.Length; i++)
            {
                var (count, xray) = BenchSteps[i];
                string mode = xray ? "xray" : "opaque";
                _benchStatus = $"Benchmark {i + 1}/{BenchSteps.Length}: {count} {mode}…";
                _xrayMode = xray;
                Spawn(count);
                yield return new WaitForSecondsRealtime(WarmupSeconds);

                _frameTimes.Clear();
                _recording = true;
                yield return new WaitForSecondsRealtime(MeasureSeconds);
                _recording = false;

                var step = Summarize(count, mode);
                report.steps[i] = step;
                _benchLines.Add($"{count} {mode}: {step.avgFps:F1} FPS, p95 {step.p95Ms:F1} ms, worst {step.worstMs:F0} ms");
            }

            string json = JsonUtility.ToJson(report);
            Debug.Log("BENCH " + json);
            _benchStatus = "Sending results…";

            var url = new Uri(new Uri(UnityEngine.Application.absoluteURL), "bench").ToString();
            using (var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                yield return request.SendWebRequest();
                _benchStatus = request.result == UnityWebRequest.Result.Success
                    ? "Benchmark done, results sent."
                    : $"Benchmark done, sending failed: {request.error}";
            }
        }

        private BenchStep Summarize(int count, string mode)
        {
            var sorted = new List<float>(_frameTimes);
            sorted.Sort();
            float total = 0f;
            foreach (float t in sorted)
                total += t;
            int n = sorted.Count;
            return new BenchStep
            {
                members = count,
                mode = mode,
                frames = n,
                avgFps = n > 0 ? n / total : 0f,
                p50Ms = n > 0 ? sorted[n / 2] * 1000f : 0f,
                p95Ms = n > 0 ? sorted[Mathf.Min(n - 1, (int)(n * 0.95f))] * 1000f : 0f,
                worstMs = n > 0 ? sorted[n - 1] * 1000f : 0f
            };
        }

        private static Dictionary<string, string> ParseQuery(string url)
        {
            var result = new Dictionary<string, string>();
            int start = string.IsNullOrEmpty(url) ? -1 : url.IndexOf('?');
            if (start < 0)
                return result;
            foreach (var part in url.Substring(start + 1).Split('&'))
            {
                if (part.Length == 0)
                    continue;
                int eq = part.IndexOf('=');
                string key = Uri.UnescapeDataString(eq < 0 ? part : part.Substring(0, eq));
                result[key] = eq < 0 ? string.Empty : Uri.UnescapeDataString(part.Substring(eq + 1));
            }
            return result;
        }

        private void Spawn(int count)
        {
            if (_root != null)
                Destroy(_root.gameObject);
            _members.Clear();
            _root = new GameObject("Spike").transform;

            AddBox("Slab", new Vector3(5f, -0.15f, 4f), Quaternion.identity, new Vector3(10f, 0.3f, 8f), _concrete);
            AddBox("Roof-S", new Vector3(5f, 6.2f, 2f), Quaternion.Euler(-22.5f, 0f, 0f), new Vector3(10.4f, 0.012f, 4.4f), _sheathing);
            AddBox("Roof-N", new Vector3(5f, 6.2f, 6f), Quaternion.Euler(22.5f, 0f, 0f), new Vector3(10.4f, 0.012f, 4.4f), _sheathing);

            var segments = HouseSegments(count);
            for (int i = 0; i < count; i++)
            {
                var (start, end, section) = segments[i];
                var dir = end - start;
                var up = Mathf.Abs(Vector3.Dot(dir.normalized, Vector3.up)) > 0.999f ? Vector3.forward : Vector3.up;
                var renderer = AddBox("M", (start + end) * 0.5f, Quaternion.LookRotation(dir, up),
                    new Vector3(section.x, section.y, dir.magnitude), _xrayMode ? _xray : _wood);
                _members.Add(renderer);
            }
        }

        private void SetXRay(bool on)
        {
            _xrayMode = on;
            var material = on ? _xray : _wood;
            foreach (var member in _members)
                member.sharedMaterial = material;
        }

        private MeshRenderer AddBox(string name, Vector3 position, Quaternion rotation, Vector3 scale, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root, false);
            go.transform.SetPositionAndRotation(position, rotation);
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = _cube;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }

        // Rough two-storey framing (studs, noggings, plates, joists, trusses). Spacing shrinks until there are enough members.
        private static List<(Vector3, Vector3, Vector2)> HouseSegments(int count)
        {
            var result = new List<(Vector3, Vector3, Vector2)>();
            for (float spacing = 0.6f; result.Count < count; spacing *= 0.85f)
            {
                result.Clear();
                var stud = new Vector2(0.035f, 0.09f);
                var joist = new Vector2(0.045f, 0.24f);
                var walls = new[]
                {
                    (new Vector3(0f, 0f, 0f), new Vector3(10f, 0f, 0f)),
                    (new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 8f)),
                    (new Vector3(10f, 0f, 8f), new Vector3(0f, 0f, 8f)),
                    (new Vector3(0f, 0f, 8f), new Vector3(0f, 0f, 0f)),
                    (new Vector3(0f, 0f, 4f), new Vector3(10f, 0f, 4f))
                };
                for (int level = 0; level < 2; level++)
                {
                    var lift = Vector3.up * (level * 2.7f);
                    foreach (var (a, b) in walls)
                    {
                        result.Add((a + lift + Vector3.up * 0.02f, b + lift + Vector3.up * 0.02f, stud));
                        result.Add((a + lift + Vector3.up * 2.4f, b + lift + Vector3.up * 2.4f, stud));
                        int studs = Mathf.CeilToInt(Vector3.Distance(a, b) / spacing);
                        for (int s = 0; s <= studs; s++)
                        {
                            var p = Vector3.Lerp(a, b, (float)s / studs) + lift;
                            result.Add((p + Vector3.up * 0.04f, p + Vector3.up * 2.38f, stud));
                            if (s < studs)
                            {
                                var q = Vector3.Lerp(a, b, (float)(s + 1) / studs) + lift;
                                result.Add((p + Vector3.up * 1.2f, q + Vector3.up * 1.2f, stud));
                            }
                        }
                    }
                }
                for (float x = 0f; x <= 10f; x += spacing * 0.75f)
                {
                    result.Add((new Vector3(x, 2.58f, 0f), new Vector3(x, 2.58f, 4f), joist));
                    result.Add((new Vector3(x, 2.58f, 4f), new Vector3(x, 2.58f, 8f), joist));
                }
                for (float x = 0f; x <= 10f; x += spacing)
                {
                    var eaveA = new Vector3(x, 5.4f, 0f);
                    var eaveB = new Vector3(x, 5.4f, 8f);
                    var ridge = new Vector3(x, 7.05f, 4f);
                    result.Add((eaveA, eaveB, stud));
                    result.Add((eaveA, ridge, stud));
                    result.Add((ridge, eaveB, stud));
                    result.Add((new Vector3(x, 5.4f, 2f), Vector3.Lerp(eaveA, ridge, 0.75f), stud));
                    result.Add((new Vector3(x, 5.4f, 6f), Vector3.Lerp(eaveB, ridge, 0.75f), stud));
                    result.Add((new Vector3(x, 5.4f, 4f), ridge, stud));
                }
            }
            return result;
        }
    }
}
