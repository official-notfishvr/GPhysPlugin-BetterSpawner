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

public class PhysicsModifier: IGPhysPlugin
{
    public string PluginName => "Physics Modifier";
    public string Version => "1.0.0";
    public Plugin? gphysInstance;
    private Harmony? harmony;

    private static float massMultiplier = 1f;
    private static float dragMultiplier = 1f;
    private static float gravityScale = 1f;
    private static Rect physicsWindowRect = new Rect(50, 250, 350, 180);
    private static bool physicsWindowInitialized = false;
    private static List<GameObject> modifiedObjects = new List<GameObject>();

    public void Cleanup()
    {
        if (harmony != null)
        {
            harmony.UnpatchSelf();
            harmony = null;
        }
        
        massMultiplier = 1f;
        dragMultiplier = 1f;
        gravityScale = 1f;
        physicsWindowInitialized = false;
        modifiedObjects.Clear();
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

        harmony = new Harmony("com.notfishvr.gphysplugin.physicsmodifier");
        harmony.PatchAll();
    }

    public void RegisterSpawnables()
    {
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
            if (!physicsWindowInitialized)
            {
                physicsWindowRect.x = (Screen.width - physicsWindowRect.width) / 2f;
                physicsWindowRect.y = 250;
                physicsWindowInitialized = true;
            }
            physicsWindowRect = GUI.Window(2285611, physicsWindowRect, PhysicsWindowContents, "Physics Settings");
            GUILayout.Space(10);
        }

        private static void PhysicsWindowContents(int windowID)
        {
            GUILayout.BeginVertical();
            GUILayout.Space(5);
            
            GUILayout.Label($"Mass Multiplier: {massMultiplier:F2}x", GUILayout.Height(25));
            massMultiplier = GUILayout.HorizontalSlider(massMultiplier, 0.1f, 10f, GUILayout.Width(300));
            
            GUILayout.Space(5);
            GUILayout.Label($"Drag Multiplier: {dragMultiplier:F2}x", GUILayout.Height(25));
            dragMultiplier = GUILayout.HorizontalSlider(dragMultiplier, 0.1f, 5f, GUILayout.Width(300));
            
            GUILayout.Space(5);
            GUILayout.Label($"Gravity Scale: {gravityScale:F2}x", GUILayout.Height(25));
            gravityScale = GUILayout.HorizontalSlider(gravityScale, 0f, 3f, GUILayout.Width(300));
            
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply to All", GUILayout.Width(150), GUILayout.Height(30)))
            {
                var allObjects = GameObject.FindObjectsOfType<GameObject>();
                foreach (var obj in allObjects)
                {
                    var rb = obj.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.mass *= massMultiplier;
                        rb.drag *= dragMultiplier;
                        rb.angularDrag *= dragMultiplier;
                        
                        if (gravityScale != 1f)
                        {
                            rb.useGravity = gravityScale > 0f;
                            if (rb.useGravity)
                            {
                                rb.AddForce(Physics.gravity * (gravityScale - 1f), ForceMode.Acceleration);
                            }
                        }
                        
                        if (!modifiedObjects.Contains(obj))
                        {
                            modifiedObjects.Add(obj);
                        }
                    }
                }
            }
            GUILayout.Space(10);
            if (GUILayout.Button("Reset All", GUILayout.Width(150), GUILayout.Height(30)))
            {
                foreach (var obj in modifiedObjects)
                {
                    if (obj != null)
                    {
                        var rb = obj.GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            rb.mass = 1f;
                            rb.drag = 0.05f;
                            rb.angularDrag = 0.05f;
                            rb.useGravity = true;
                        }
                    }
                }
                modifiedObjects.Clear();
            }
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0, 0, 350, 20));
        }
    }

    [HarmonyPatch]
    internal class SpawnPhysicObjectPatch
    {
        static MethodBase TargetMethod()
        {
            var type = AccessTools.TypeByName("GPhys.Plugin");
            return AccessTools.Method(type, "SpawnPhysicObject", new[] { typeof(GameObject), typeof(Vector3), typeof(Quaternion) });
        }

        static void Postfix(GameObject __result)
        {
            if (__result != null)
            {
                var rb = __result.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.mass *= massMultiplier;
                    rb.drag *= dragMultiplier;
                    rb.angularDrag *= dragMultiplier;
                    
                    if (gravityScale != 1f)
                    {
                        rb.useGravity = gravityScale > 0f;
                        if (rb.useGravity)
                        {
                            rb.AddForce(Physics.gravity * (gravityScale - 1f), ForceMode.Acceleration);
                        }
                    }
                    
                    if (!modifiedObjects.Contains(__result))
                    {
                        modifiedObjects.Add(__result);
                    }
                }
            }
        }
    }
} 