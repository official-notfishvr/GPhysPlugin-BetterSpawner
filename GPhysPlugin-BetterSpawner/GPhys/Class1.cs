using GPhys;
using GPhys.Types.Abstracts;
using GPhys.Types.Objects;
using System.IO;
using System.Reflection;
using System;
using UnityEngine;
using UnityEngine.UIElements;
using HarmonyLib;
using System.Collections.Generic;

public class BetterSpawner: IGPhysPlugin
{
    public string PluginName => "Better Spawner";
    public string Version => "1.0.0";
    public Plugin? gphysInstance;
    private Harmony? harmony;

    private static List<string> capturedButtonNames = new List<string>();
    private static float customSliderValue = 1f; 
    private static Rect sliderWindowRect = new Rect(50, 50, 260, 80);
    private static bool sliderWindowInitialized = false;

    public void Cleanup()
    {
        if (harmony != null)
        {
            harmony.UnpatchSelf();
            harmony = null;
        }
        
        capturedButtonNames.Clear();
        customSliderValue = 1f;
        sliderWindowInitialized = false;
    }
    public void Update()
    {
    }
    public void OnGUI()
    {
    }
    public void Initialize(Plugin instance)
    {
        gphysInstance = instance;

        harmony = new Harmony("com.notfishvr.gphysplugin.betterspawner");
        harmony.PatchAll();
    }
    public void RegisterSpawnables()
    {

    }

    [HarmonyPatch]
    internal class DrawImageButtonPatch
    {
        static MethodBase TargetMethod()
        {
            var type = AccessTools.TypeByName("GPhys.Plugin");
            return AccessTools.Method(type, "DrawImageButton", new[] { typeof(string), typeof(int), typeof(int), typeof(Action) });
        }

        static void Prefix(ref string name, ref int index, ref int row, ref Action action)
        {
            if (!capturedButtonNames.Contains(name))
            {
                capturedButtonNames.Add(name);
                Debug.Log($"[BetterSpawner] Saved button name: {name}");
            }
            if (action != null)
            {
                Action originalAction = action;
                action = () => {
                    int count = Mathf.RoundToInt(Mathf.Clamp(customSliderValue, 1f, 100f));
                    for (int i = 0; i < count; i++)
                    {
                        originalAction();
                    }
                };
            }
        }
    }

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
            if (!sliderWindowInitialized)
            {
                sliderWindowRect.x = (Screen.width - sliderWindowRect.width) / 2f;
                sliderWindowRect.y = 50;
                sliderWindowInitialized = true;
            }
            sliderWindowRect = GUI.Window(987654, sliderWindowRect, SliderWindowContents, "Spawn Count");
            GUILayout.Space(10);
        }

        private static void SliderWindowContents(int windowID)
        {
            GUILayout.BeginVertical();
            GUILayout.Label($"Spawn Count: {Mathf.RoundToInt(customSliderValue)}");
            customSliderValue = GUILayout.HorizontalSlider(customSliderValue, 1f, 100f, GUILayout.Width(200));
            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0, 0, 260, 20));
        }
    }
}