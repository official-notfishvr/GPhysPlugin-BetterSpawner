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
using BepInEx;

public class MenuToggle : IGPhysPlugin
{
    public string PluginName => "Menu Toggle";
    public string Version => "1.0.0";
    public Plugin? gphysInstance;

    public void Cleanup()
    {
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

        var harmony = new Harmony("com.notfishvr.gphysplugin.menutoggle");
        harmony.PatchAll();
    }
    public void RegisterSpawnables()
    {

    }

    [HarmonyPatch]
    internal class UpdatePatch
    {
        static MethodBase TargetMethod()
        {
            var type = AccessTools.TypeByName("GPhys.Plugin");
            return AccessTools.Method(type, "Update");
        }
        static bool Prefix()
        {
            if (UnityInput.Current.GetKeyDown(GPhys.Plugin.Instance.menuKey))
            {
                GPhys.Plugin.Instance.menuEnabled = !GPhys.Plugin.Instance.menuEnabled;
            }
            foreach (var plugin in new List<IGPhysPlugin>())
            {
                plugin.Update();
            }
            return false;
        }
    }
}