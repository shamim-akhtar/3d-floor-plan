using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Makes the render sharper and cleaner:
//  - SMAA anti-aliasing on the camera, plus 4x MSAA and full render scale
//  - Neutral tone mapping so warm lights do not wash out the whites
//  - Sharper shadows and textures
//  - A reflection probe in each room, for cleaner reflections on floors and glass
// Put this on the FloorPlanBuilder object. It applies itself in Play mode,
// or use "Apply Render Quality" from the component's menu.
// Note: the URP asset and Quality settings it changes are project settings,
// so those changes stay after you stop playing.
public class RenderQuality : MonoBehaviour
{
    [Header("Camera")]
    public bool smaa = true;
    [Tooltip("1, 2, 4 or 8")]
    public int msaa = 4;
    [Range(0.5f, 2f)] public float renderScale = 1f;

    [Header("Look")]
    public bool neutralTonemapping = true;
    [Range(-1f, 1f)] public float exposure = 0f;

    [Header("Shadows and textures")]
    public float shadowDistance = 20f;
    public int shadowCascades = 4;
    public int mainLightShadowResolution = 4096;
    public bool sharpTextures = true;

    [Header("Reflections")]
    public bool roomReflectionProbes = true;
    [Tooltip("Only floor zones bigger than this get a probe (square metres)")]
    public float minProbeArea = 2f;
    public int probeResolution = 256;

    IEnumerator Start()
    {
        // Wait so the house, ceilings and walls are built first
        yield return null;
        yield return null;
        Apply();
    }

    [ContextMenu("Apply Render Quality")]
    public void Apply()
    {
        SetupCamera();
        SetupPipeline();
        SetupVolume();
        if (roomReflectionProbes) BuildProbes();
    }

    [ContextMenu("Refresh Reflections")]
    public void RefreshReflections()
    {
        var probes = transform.Find("Reflection Probes");
        if (probes == null) return;
        foreach (var p in probes.GetComponentsInChildren<ReflectionProbe>()) p.RenderProbe();
    }

    void SetupCamera()
    {
        var cam = Camera.main;
        if (cam == null) { Debug.LogWarning("RenderQuality: no camera tagged MainCamera."); return; }
        cam.allowMSAA = msaa > 1;
        cam.allowHDR = true;

        var data = cam.GetComponent<UniversalAdditionalCameraData>();
        if (data == null) data = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
        data.renderPostProcessing = true;
        if (smaa)
        {
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;
        }
    }

    void SetupPipeline()
    {
        var urp = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
        if (urp == null) urp = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
        if (urp != null)
        {
            urp.msaaSampleCount = Mathf.ClosestPowerOfTwo(Mathf.Clamp(msaa, 1, 8));
            urp.renderScale = renderScale;
            urp.supportsHDR = true;
            urp.shadowDistance = shadowDistance;
            urp.shadowCascadeCount = Mathf.Clamp(shadowCascades, 1, 4);
            urp.mainLightShadowmapResolution = mainLightShadowResolution;
        }
        else Debug.LogWarning("RenderQuality: could not find the URP asset.");

        if (sharpTextures)
        {
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            QualitySettings.globalTextureMipmapLimit = 0;
        }
    }

    void SetupVolume()
    {
        const string volName = "Render Quality Volume";
        var existing = transform.Find(volName);
        if (existing != null) DestroyImmediate(existing.gameObject);

        var go = new GameObject(volName);
        go.transform.SetParent(transform, false);
        var vol = go.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = 20;

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        if (neutralTonemapping)
        {
            var tm = profile.Add<Tonemapping>(true);
            tm.mode.Override(TonemappingMode.Neutral);
        }
        if (Mathf.Abs(exposure) > 0.001f)
        {
            var ca = profile.Add<ColorAdjustments>(true);
            ca.postExposure.Override(exposure);
        }
        vol.sharedProfile = profile;
    }

    // One box-projected probe per floor zone, sized to the room and its ceiling height
    void BuildProbes()
    {
        var old = transform.Find("Reflection Probes");
        if (old != null) DestroyImmediate(old.gameObject);

        var floors = GetComponent<FloorBuilder>();
        if (floors == null) { Debug.LogWarning("RenderQuality: add Floor Builder to place reflection probes."); return; }
        var plan = GetComponent<FloorPlanBuilder>();
        float h = plan != null ? plan.wallHeight : 2.6f;

        var root = new GameObject("Reflection Probes").transform;
        root.SetParent(transform, false);

        foreach (var z in floors.zones)
        {
            float x0 = Mathf.Min(z.min.x, z.max.x), x1 = Mathf.Max(z.min.x, z.max.x);
            float z0 = Mathf.Min(z.min.y, z.max.y), z1 = Mathf.Max(z.min.y, z.max.y);
            if ((x1 - x0) * (z1 - z0) < minProbeArea) continue;

            var go = new GameObject("Probe - " + z.name);
            go.transform.SetParent(root, false);
            go.transform.localPosition = new Vector3((x0 + x1) / 2f, 1.5f, (z0 + z1) / 2f);

            var p = go.AddComponent<ReflectionProbe>();
            p.mode = ReflectionProbeMode.Realtime;
            p.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            p.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
            p.boxProjection = true;
            p.resolution = probeResolution;
            p.hdr = true;
            p.size = new Vector3(x1 - x0, h, z1 - z0);
            p.center = new Vector3(0f, h / 2f - 1.5f, 0f);
            p.blendDistance = 0.3f;
            p.RenderProbe();
        }
    }
}
