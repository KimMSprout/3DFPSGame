using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Lean.Pool;
using Unity.Profiling;
using UnityEngine;

// 최적화 전후 비교용 벤치마크 : 고정 시점들에서 렌더 통계와 프레임 시간을 측정해 JSON으로 저장한다.
// 플레이 모드에서 런타임에 붙여서 실행하며, 씬에는 아무것도 저장하지 않는다.
// 사용 예 : new GameObject("Benchmark").AddComponent<PerformanceBenchmark>().Begin("baseline");
public class PerformanceBenchmark : MonoBehaviour
{
    [Serializable]
    public class Viewpoint
    {
        public string Name;
        public Vector3 Position;
        public Vector3 LookAt;

        public Viewpoint(string name, Vector3 position, Vector3 lookAt)
        {
            Name = name;
            Position = position;
            LookAt = lookAt;
        }
    }

    [Serializable]
    public class Stat
    {
        public double Avg;
        public double Min;
        public double Max;
    }

    [Serializable]
    public class ViewpointResult
    {
        public string Name;
        public int SampledFrames;
        public int RenderedFrames;
        public Stat DrawCalls;
        public Stat Batches;
        public Stat SetPassCalls;
        public Stat Triangles;
        public Stat Vertices;
        public Stat ShadowCasters;
        public Stat VisibleSkinnedMeshes;
        public Stat VisibleMeshRenderers;
        public Stat CpuMainThreadMs;
        public Stat CpuRenderThreadMs;
        public Stat GpuMs;
        public Stat FrameMs;
        public string Screenshot;
    }

    [Serializable]
    public class Report
    {
        public string Label;
        public string TimestampUtc;
        public string UnityVersion;
        public int ScreenWidth;
        public int ScreenHeight;
        public int VSyncCount;
        public int TargetFrameRate;
        public int ZombieCount;
        public int MeshRendererCount;
        public bool OptimizationsDisabled;
        public List<ViewpointResult> Viewpoints = new List<ViewpointResult>();
    }

    // 프레임 단위 값을 누적해서 평균/최소/최대를 낸다.
    private class Accumulator
    {
        private double _sum;
        private double _min = double.MaxValue;
        private double _max = double.MinValue;
        private int _count;

        public void Add(double value)
        {
            _sum += value;
            _min = Math.Min(_min, value);
            _max = Math.Max(_max, value);
            _count++;
        }

        public Stat ToStat()
        {
            if (_count == 0) return new Stat { Avg = -1, Min = -1, Max = -1 };

            return new Stat { Avg = Math.Round(_sum / _count, 3), Min = Math.Round(_min, 3), Max = Math.Round(_max, 3) };
        }
    }

    private const int ZombiesPerSpawnPoint = 3;
    private const int RandomSeed = 1234;
    private const float NanosecondsToMs = 1e-6f;
    private const string OutputFolder = "Docs/Optimization";

    // 플레이어를 좀비 감지 거리 밖(지하)으로 치워 둔다.
    private static readonly Vector3 PlayerParkPosition = new Vector3(24f, -100f, -12f);

    [SerializeField] private float _settleSeconds = 3f;
    [SerializeField] private float _warmupSeconds = 1f;
    [SerializeField] private float _sampleSeconds = 4f;

    [SerializeField]
    private Viewpoint[] _viewpoints =
    {
        new Viewpoint("01_PlayerStart_Spawns", new Vector3(-9f, 2.6f, -2f), new Vector3(-15f, 1f, 20f)),
        new Viewpoint("02_BehindHangar", new Vector3(31f, 1.8f, -13f), new Vector3(31f, 1.8f, 20f)),
        new Viewpoint("03_EastIndustrial", new Vector3(40f, 1.8f, -30f), new Vector3(90f, 2f, -45f)),
        new Viewpoint("04_Overview_High", new Vector3(-55f, 25f, -90f), new Vector3(30f, 0f, 0f)),
        new Viewpoint("05_NorthSpawn", new Vector3(5f, 1.8f, 35f), new Vector3(25f, 1f, -10f)),
    };

    private const string PlayerViewName = "00_PlayerView";

    private string _label;
    private bool _disableOptimizations;
    private bool _keepPlayer;
    private MeshRenderer[] _meshRenderers;
    private int _zombieCount;

    public static bool IsRunning { get; private set; }
    public static string LastReportPath { get; private set; }

    // disableOptimizations가 true면 오클루전 컬링, LOD Group, Mesh LOD를 런타임에서 꺼서
    // 같은 세션 안에서 최적화 전 상태와 A/B 비교를 할 수 있게 한다.
    // keepPlayer가 true면 플레이어와 게임 카메라를 그대로 두고 현재 화면 하나만 측정한다(플레이어 렌더링 비용 포함).
    public void Begin(string label, bool disableOptimizations = false, bool keepPlayer = false)
    {
        _label = label;
        _disableOptimizations = disableOptimizations;
        _keepPlayer = keepPlayer;
        IsRunning = true;

        // 에디터가 포커스를 잃으면 플레이 모드가 스로틀링되어 측정값이 망가지므로 런타임에서만 켠다.
        Application.runInBackground = true;
        LastReportPath = null;
        StartCoroutine(Run_Coroutine());
    }

    private IEnumerator Run_Coroutine()
    {
        while (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
        {
            yield return null;
        }

        Camera mainCamera = Camera.main;
        if (_keepPlayer == false)
        {
            DisableCameraControl(mainCamera);
            DisablePlayer();
        }

        SpawnZombies();

        if (_disableOptimizations)
        {
            DisableOptimizations();
        }

        _meshRenderers = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);

        yield return new WaitForSeconds(_settleSeconds);

        string outputDirectory = Path.Combine(Application.dataPath, "..", OutputFolder);
        string screenshotDirectory = Path.Combine(outputDirectory, "Screenshots");
        Directory.CreateDirectory(screenshotDirectory);

        Report report = new Report
        {
            Label = _label,
            TimestampUtc = DateTime.UtcNow.ToString("o"),
            UnityVersion = Application.unityVersion,
            ScreenWidth = Screen.width,
            ScreenHeight = Screen.height,
            VSyncCount = QualitySettings.vSyncCount,
            TargetFrameRate = Application.targetFrameRate,
            ZombieCount = _zombieCount,
            MeshRendererCount = _meshRenderers.Length,
            OptimizationsDisabled = _disableOptimizations,
        };

        // 플레이어 유지 모드에서는 카메라를 옮기지 않는 시점(null) 하나만 잰다.
        Viewpoint[] viewpoints = _keepPlayer ? new Viewpoint[] { null } : _viewpoints;
        foreach (Viewpoint viewpoint in viewpoints)
        {
            if (viewpoint != null)
            {
                mainCamera.transform.SetPositionAndRotation(viewpoint.Position,
                    Quaternion.LookRotation(viewpoint.LookAt - viewpoint.Position));
            }

            yield return new WaitForSeconds(_warmupSeconds);

            string viewpointName = viewpoint != null ? viewpoint.Name : PlayerViewName;
            ViewpointResult result = new ViewpointResult { Name = viewpointName };
            yield return Sample_Coroutine(result);

            string screenshotName = $"{_label}_{viewpointName}.png";
            ScreenCapture.CaptureScreenshot(Path.Combine(screenshotDirectory, screenshotName));
            result.Screenshot = $"Screenshots/{screenshotName}";
            yield return null;

            report.Viewpoints.Add(result);
        }

        string reportPath = Path.GetFullPath(Path.Combine(outputDirectory, $"{_label}.json"));
        File.WriteAllText(reportPath, JsonUtility.ToJson(report, true));

        LastReportPath = reportPath;
        IsRunning = false;
        Debug.Log($"[PerformanceBenchmark] 측정 완료 : {reportPath}");
    }

    private IEnumerator Sample_Coroutine(ViewpointResult result)
    {
        ProfilerRecorder drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
        ProfilerRecorder batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
        ProfilerRecorder setPass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
        ProfilerRecorder triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
        ProfilerRecorder vertices = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Vertices Count");
        ProfilerRecorder shadowCasters = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Shadow Casters Count");
        ProfilerRecorder skinned = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Visible Skinned Meshes Count");
        ProfilerRecorder cpuMain = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "CPU Main Thread Frame Time");
        ProfilerRecorder cpuRender = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "CPU Render Thread Frame Time");
        ProfilerRecorder gpu = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "GPU Frame Time");

        Accumulator drawCallsAcc = new Accumulator();
        Accumulator batchesAcc = new Accumulator();
        Accumulator setPassAcc = new Accumulator();
        Accumulator trianglesAcc = new Accumulator();
        Accumulator verticesAcc = new Accumulator();
        Accumulator shadowCastersAcc = new Accumulator();
        Accumulator skinnedAcc = new Accumulator();
        Accumulator visibleAcc = new Accumulator();
        Accumulator cpuMainAcc = new Accumulator();
        Accumulator cpuRenderAcc = new Accumulator();
        Accumulator gpuAcc = new Accumulator();
        Accumulator frameAcc = new Accumulator();

        // 레코더는 시작한 다음 프레임부터 값이 들어온다.
        yield return null;

        float elapsed = 0f;
        int frames = 0;
        int renderedFrames = 0;
        while (elapsed < _sampleSeconds)
        {
            yield return null;
            elapsed += Time.unscaledDeltaTime;
            frames++;

            frameAcc.Add(Time.unscaledDeltaTime * 1000f);
            AddIfValid(cpuMainAcc, cpuMain, NanosecondsToMs);
            AddIfValid(cpuRenderAcc, cpuRender, NanosecondsToMs);

            // 에디터에서는 렌더가 기록되지 않은 프레임에 렌더 카운터가 0으로 읽히므로 그 프레임은 뺀다.
            if (drawCalls.Valid && drawCalls.LastValue == 0) continue;

            renderedFrames++;
            AddIfValid(drawCallsAcc, drawCalls, 1f);
            AddIfValid(batchesAcc, batches, 1f);
            AddIfValid(setPassAcc, setPass, 1f);
            AddIfValid(trianglesAcc, triangles, 1f);
            AddIfValid(verticesAcc, vertices, 1f);
            AddIfValid(shadowCastersAcc, shadowCasters, 1f);
            AddIfValid(skinnedAcc, skinned, 1f);
            AddIfValid(gpuAcc, gpu, NanosecondsToMs);
            visibleAcc.Add(CountVisibleMeshRenderers());
        }

        result.SampledFrames = frames;
        result.RenderedFrames = renderedFrames;
        result.DrawCalls = drawCallsAcc.ToStat();
        result.Batches = batchesAcc.ToStat();
        result.SetPassCalls = setPassAcc.ToStat();
        result.Triangles = trianglesAcc.ToStat();
        result.Vertices = verticesAcc.ToStat();
        result.ShadowCasters = shadowCastersAcc.ToStat();
        result.VisibleSkinnedMeshes = skinnedAcc.ToStat();
        result.VisibleMeshRenderers = visibleAcc.ToStat();
        result.CpuMainThreadMs = cpuMainAcc.ToStat();
        result.CpuRenderThreadMs = cpuRenderAcc.ToStat();
        result.GpuMs = gpuAcc.ToStat();
        result.FrameMs = frameAcc.ToStat();

        drawCalls.Dispose();
        batches.Dispose();
        setPass.Dispose();
        triangles.Dispose();
        vertices.Dispose();
        shadowCasters.Dispose();
        skinned.Dispose();
        cpuMain.Dispose();
        cpuRender.Dispose();
        gpu.Dispose();
    }

    private static void AddIfValid(Accumulator accumulator, ProfilerRecorder recorder, float scale)
    {
        if (recorder.Valid == false) return;

        accumulator.Add(recorder.LastValue * scale);
    }

    // isVisible은 그림자 패스를 포함해 어떤 카메라에든 그려지면 true다.
    private int CountVisibleMeshRenderers()
    {
        int count = 0;
        foreach (MeshRenderer meshRenderer in _meshRenderers)
        {
            if (meshRenderer != null && meshRenderer.isVisible)
            {
                count++;
            }
        }

        return count;
    }

    private static void DisableCameraControl(Camera mainCamera)
    {
        SetEnabled<CameraFollow>(mainCamera.gameObject, false);
        SetEnabled<CameraRotate>(mainCamera.gameObject, false);
        SetEnabled<CameraViewSwitcher>(mainCamera.gameObject, false);

        // 미니맵 카메라는 현재 위치(플레이어 시작 지점 위)에 고정한다.
        MinimapCamera minimapCamera = FindFirstObjectByType<MinimapCamera>();
        if (minimapCamera != null)
        {
            minimapCamera.enabled = false;
        }
    }

    // SetActive(false)를 하면 좀비가 Player 태그를 찾지 못해 NullReference가 나므로, 조작만 끄고 멀리 치운다.
    private static void DisablePlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        foreach (MonoBehaviour behaviour in player.GetComponentsInChildren<MonoBehaviour>())
        {
            if (behaviour is Player) continue;

            behaviour.enabled = false;
        }

        CharacterController characterController = player.GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.enabled = false;
        }

        player.transform.position = PlayerParkPosition;
    }

    private void SpawnZombies()
    {
        ZombieSpawner spawner = FindFirstObjectByType<ZombieSpawner>(FindObjectsInactive.Include);
        if (spawner == null)
        {
            Debug.LogWarning("[PerformanceBenchmark] ZombieSpawner를 찾지 못해 좀비 없이 측정합니다.");
            return;
        }

        // 스포너의 private 설정값을 그대로 쓰기 위해 리플렉션으로 읽는다(측정 전용).
        Type spawnerType = typeof(ZombieSpawner);
        const System.Reflection.BindingFlags Flags =
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        GameObject zombiePrefab = (GameObject)spawnerType.GetField("_zombiePrefab", Flags).GetValue(spawner);
        Transform[] spawnPoints = (Transform[])spawnerType.GetField("_spawnPoints", Flags).GetValue(spawner);

        UnityEngine.Random.InitState(RandomSeed);

        foreach (Transform spawnPoint in spawnPoints)
        {
            for (int i = 0; i < ZombiesPerSpawnPoint; i++)
            {
                Vector3 offset = new Vector3(i * 1.5f, 1f, 0f);
                LeanPool.Spawn(zombiePrefab, spawnPoint.position + offset, Quaternion.identity);
                _zombieCount++;
            }
        }
    }

    private static void DisableOptimizations()
    {
        foreach (Camera camera in FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            camera.useOcclusionCulling = false;
        }

        // LOD Group을 끄면 모든 LOD가 동시에 그려지므로, 최적화 전처럼 LOD0만 남긴다.
        foreach (LODGroup lodGroup in FindObjectsByType<LODGroup>(FindObjectsSortMode.None))
        {
            LOD[] lods = lodGroup.GetLODs();
            lodGroup.enabled = false;

            for (int i = 1; i < lods.Length; i++)
            {
                foreach (Renderer lodRenderer in lods[i].renderers)
                {
                    if (lodRenderer != null)
                    {
                        lodRenderer.enabled = false;
                    }
                }
            }
        }

        foreach (MeshRenderer meshRenderer in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            meshRenderer.forceMeshLod = 0;
        }

        foreach (SkinnedMeshRenderer skinnedMeshRenderer in FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None))
        {
            skinnedMeshRenderer.forceMeshLod = 0;
        }
    }

    private static void SetEnabled<T>(GameObject target, bool value) where T : Behaviour
    {
        T component = target.GetComponent<T>();
        if (component != null)
        {
            component.enabled = value;
        }
    }
}
