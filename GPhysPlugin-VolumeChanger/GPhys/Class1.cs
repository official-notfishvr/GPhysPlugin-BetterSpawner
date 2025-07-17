using GPhys;
using GPhys.Types.Abstracts;
using GPhys.Types.Objects;
using System;
using System.Reflection;
using UnityEngine;
using HarmonyLib;

public class VolumeChanger : IGPhysPlugin
{
    public string PluginName => "Volume Changer";
    public string Version => "1.0.0";
    public Plugin? gphysInstance;
    private Harmony? harmony;

    public static float customVolumeMultiplier = 100f;
    private static Rect volumeWindowRect = new Rect(50, 250, 400, 140);
    private static bool volumeWindowInitialized = false;

    public void Cleanup()
    {
        if (harmony != null)
        {
            harmony.UnpatchSelf();
            harmony = null;
        }
        customVolumeMultiplier = 100f;
        volumeWindowInitialized = false;
    }
    public void Update() { }
    public void OnGUI() { }
    public void Initialize(Plugin instance)
    {
        gphysInstance = instance;
        harmony = new Harmony("com.notfishvr.gphysplugin.volumechanger");
        harmony.PatchAll();
    }
    public void RegisterSpawnables() { }

    [HarmonyPatch]
    internal class DrawSpawnableButtonsPatch
    {
        static MethodBase TargetMethod()
        {
            var type = AccessTools.TypeByName("GPhys.Plugin");
            return AccessTools.Method(type, "DrawSpawnableButtons");
        }

        static void Prefix()
        {
            if (!volumeWindowInitialized)
            {
                volumeWindowRect.x = (Screen.width - volumeWindowRect.width) / 2f;
                volumeWindowRect.y = 250;
                volumeWindowInitialized = true;
            }
            volumeWindowRect = GUI.Window(2285614, volumeWindowRect, VolumeWindowContents, "Volume Changer");
            GUILayout.Space(10);
        }

        private static void VolumeWindowContents(int windowID)
        {
            GUILayout.BeginVertical();
            GUILayout.Label("This will change the sound of everything.");
            GUILayout.Label($"Volume: {(int)customVolumeMultiplier}");
            customVolumeMultiplier = GUILayout.HorizontalSlider(customVolumeMultiplier, 0f, 100f, GUILayout.Width(320));
            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0, 0, 400, 30));
        }
    }

    [HarmonyPatch]
    internal class PhysicObject_OnCollisionEnter_Patch
    {
        static MethodBase TargetMethod()
        {
            var type = AccessTools.TypeByName("GPhys.Types.Objects.PhysicObject");
            return AccessTools.Method(type, "OnCollisionEnter", new[] { typeof(Collision) });
        }

        static void Postfix(object __instance)
        {
            var type = __instance.GetType();
            var sourceField = type.GetField("source");
            var rbField = type.GetField("rb");
            if (sourceField == null || rbField == null) return;
            var source = sourceField.GetValue(__instance) as AudioSource;
            var rb = rbField.GetValue(__instance) as Rigidbody;
            if (source == null || rb == null) return;
            float baseVolume = rb.velocity.magnitude / 10f;
            source.volume = Mathf.Clamp01(baseVolume * (VolumeChanger.customVolumeMultiplier / 100f));
        }
    }

    [HarmonyPatch]
    internal class ExplosiveObject_Explode_Patch
    {
        static MethodBase TargetMethod()
        {
            var type = AccessTools.TypeByName("GPhys.Types.Objects.ExplosiveObject");
            return AccessTools.Method(type, "Explode");
        }

        static void Prefix(object __instance)
        {
            var type = __instance.GetType();
            var sourceField = type.GetField("source");
            if (sourceField == null) return;
            var source = sourceField.GetValue(__instance) as AudioSource;
            if (source == null) return;
            source.volume = Mathf.Clamp01(VolumeChanger.customVolumeMultiplier / 100f);
        }
    }

    [HarmonyPatch(typeof(AudioSource), "PlayOneShot", new[] { typeof(AudioClip), typeof(float) })]
    internal class AudioSource_PlayOneShot_Patch
    {
        static void Prefix(AudioSource __instance, AudioClip clip, ref float volumeScale)
        {
            if (clip == null) return;
            if (clip.name != null && clip.name.ToLower().Contains("music")) return;
            volumeScale *= (VolumeChanger.customVolumeMultiplier / 100f);
        }
    }

    [HarmonyPatch(typeof(AudioSource), "Play", new Type[0])]
    internal class AudioSource_Play_Patch
    {
        static float? lastOriginalVolume;
        static void Prefix(AudioSource __instance)
        {
            if (__instance.clip == null) return;
            if (__instance.clip.name != null && __instance.clip.name.ToLower().Contains("music")) return;
            lastOriginalVolume = __instance.volume;
            __instance.volume = Mathf.Clamp01(__instance.volume * (VolumeChanger.customVolumeMultiplier / 100f));
        }
        static void Postfix(AudioSource __instance)
        {
            if (lastOriginalVolume.HasValue)
            {
                __instance.volume = lastOriginalVolume.Value;
                lastOriginalVolume = null;
            }
        }
    }

    [HarmonyPatch]
    internal class Plugin_PlaySFX_Patch
    {
        static MethodBase TargetMethod()
        {
            var type = AccessTools.TypeByName("GPhys.Plugin");
            return AccessTools.Method(type, "SpawnBomb");
        }

        static void Prefix(object __instance, GameObject prefab, Vector3 position, Quaternion rotation)
        {
            var bombPrefab = prefab;
            if (bombPrefab == null) return;
            var audioSource = bombPrefab.GetComponentInChildren<AudioSource>();
            if (audioSource != null)
            {
                audioSource.volume = Mathf.Clamp01(VolumeChanger.customVolumeMultiplier / 100f);
            }
        }
    }
} 