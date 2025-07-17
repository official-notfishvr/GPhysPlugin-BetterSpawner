using ExitGames.Client.Photon;
using GPhys.Types;
using GPhys.Types.Abstracts;
using GPhys.Types.Objects;
using GPhys.Types.Objects.SCPs;
using GPhys.Types.Objects.Vehicles;
using Newtonsoft.Json;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using GPhys.Addons;
using HarmonyLib;
using GPhys;
using NetworkUtils = GPhys.Addons.NetworkUtils;

namespace GPhys.Addons
{
    public class Networking : MonoBehaviourPunCallbacks, IGPhysPlugin
    {
        public string PluginName => "Networking";
        public string Version => "2.0.0";
        public Plugin? gphysInstance;
        private Harmony? harmony;

        public void Cleanup()
        {
            if (harmony != null)
            {
                harmony.UnpatchSelf();
                harmony = null;
            }
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

            harmony = new Harmony("com.notfishvr.gphysplugin.networking");
            harmony.PatchAll();
        }
        public void RegisterSpawnables()
        {

        }
        public override async void OnJoinedRoom()
        {
            base.OnJoinedRoom();
            PhotonNetwork.NetworkingClient.EventReceived += OnEvent;
        }
        public override void OnLeftRoom()
        {
            base.OnLeftRoom();
            PhotonNetwork.NetworkingClient.EventReceived -= OnEvent;
        }

        public void OnEvent(EventData data)
        {
            if (data.Code == (int)NetworkUtils.NetworkType.SpawnObjectCode)
            {
                object[] eventData = (object[])data.CustomData;
                string prefabName = (string)eventData[0];
                Vector3 position = Utils.UnpackVector3FromLong((long)eventData[1]);
                Quaternion rotation = NetworkUtils.UnpackQuaternionFromLong((long)eventData[2]);

                if (!PhotonNetwork.LocalPlayer.IsLocal)
                {
                    Debug.Log($"[GPhysNetworking] Received spawn event for '{prefabName}' at {position} rot {rotation}");
                    if (prefabName == Plugin.Instance.ToolGunPrefab?.name)
                        Plugin.Instance.SpawnToolGun(position, rotation);
                    else if (prefabName == Plugin.Instance.AirboatPrefab?.name)
                        Plugin.Instance.SpawnAirboat(position, rotation);
                    else if (prefabName == Plugin.Instance.BarnaclePrefab?.name)
                        Plugin.Instance.SpawnBarnacle(position, rotation, BarnacleType.Default); // Default type
                    else if (prefabName == Plugin.Instance.BombPrefab?.name)
                        Plugin.Instance.SpawnBomb(position, rotation);
                    else if (prefabName == Plugin.Instance.BullsquidPrefab?.name)
                        Plugin.Instance.SpawnBullSquid(position, rotation);
                    else if (prefabName == Plugin.Instance.HeadcrabPrefab?.name)
                        Plugin.Instance.SpawnHeadcrab(position, rotation, HeadcrabType.Normal); // Default type
                    else if (prefabName == Plugin.Instance.HoundeyePrefab?.name)
                        Plugin.Instance.SpawnHoundeye(position, rotation);
                    else if (prefabName == Plugin.Instance.MagnetPrefab?.name)
                        Plugin.Instance.SpawnMagnet(position, rotation);
                    else if (prefabName == Plugin.Instance.petRockPrefab?.name)
                        Plugin.Instance.SpawnPetRock(position, rotation);
                    else if (prefabName == Plugin.Instance.PhysGunPrefab?.name)
                        Plugin.Instance.SpawnPhysGun(position, rotation);
                    else if (prefabName == Plugin.Instance.PistolPrefab?.name)
                        Plugin.Instance.SpawnPistol(position, rotation);
                    else if (prefabName == Plugin.Instance.RocketLauncherPrefab?.name)
                        Plugin.Instance.SpawnRocketLauncher(position, rotation);
                    else if (prefabName == Plugin.Instance.ScoutCarPrefab?.name)
                        Plugin.Instance.SpawnScoutCar(position, rotation);
                    else if (prefabName == Plugin.Instance.SCP096Prefab?.name)
                        Plugin.Instance.SpawnSCP096(position, rotation);
                    else if (prefabName == Plugin.Instance.SCP173Prefab?.name)
                        Plugin.Instance.SpawnSCP173(position, rotation);
                    else if (prefabName == Plugin.Instance.StasisCannonPrefab?.name)
                        Plugin.Instance.SpawnStasisCannon(position, rotation);
                    else if (Plugin.Instance.customSpawnables != null && Plugin.Instance.customSpawnables.ContainsKey(prefabName))
                        Plugin.Instance.SpawnCustomObject(prefabName, position, rotation);
                    else
                    {
                        var allPrefabs = Plugin.Instance.allPrefabs;
                        GameObject prefab = null;
                        if (allPrefabs != null)
                        {
                            foreach (var p in allPrefabs)
                            {
                                if (p != null && p.name == prefabName)
                                {
                                    prefab = p;
                                    break;
                                }
                            }
                        }
                        if (prefab != null)
                        {
                            Plugin.Instance.SpawnPhysicObject(prefab, position, rotation);
                        }
                        else
                        {
                            Debug.LogWarning($"[GPhysNetworking] Could not find spawn method or prefab for '{prefabName}'");
                        }
                    }
                }
                else
                {
                    Debug.Log($"[GPhysNetworking] Local spawn, sending event for '{prefabName}' at {position} rot {rotation}");
                    var allPrefabs = Plugin.Instance.allPrefabs;
                    GameObject prefab = null;
                    if (allPrefabs != null)
                    {
                        foreach (var p in allPrefabs)
                        {
                            if (p != null && p.name == prefabName)
                            {
                                prefab = p;
                                break;
                            }
                        }
                    }
                    if (prefab != null)
                    {
                        NetworkUtils.SpawnObject(
                            prefab.name,
                            Utils.PackVector3ToLong(position),
                            NetworkUtils.PackQuaternionToLong(rotation)
                        );
                    }
                }
                return;
            }
        }
    }
} 

[HarmonyPatch]
public static class GPhysSpawnPatches
{
    static bool IsNetworkActive() => Photon.Pun.PhotonNetwork.LocalPlayer != null && Photon.Pun.PhotonNetwork.InRoom;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Plugin), nameof(Plugin.SpawnPhysicObject))]
    public static void Postfix_SpawnPhysicObject(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (IsNetworkActive() && prefab != null)
        {
            Debug.Log($"[GPhysNetworking] Sending network spawn for '{prefab.name}' at {position} rot {rotation}");
            NetworkUtils.SpawnObject(
                prefab.name,
                Utils.PackVector3ToLong(position),
                NetworkUtils.PackQuaternionToLong(rotation)
            );
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Plugin), nameof(Plugin.SpawnToolGun))]
    public static void Postfix_SpawnToolGun(Vector3 position, Quaternion rotation)
    {
        if (IsNetworkActive() && Plugin.Instance.ToolGunPrefab != null)
        {
            Debug.Log($"[GPhysNetworking] Sending network spawn for '{Plugin.Instance.ToolGunPrefab.name}' at {position} rot {rotation}");
            NetworkUtils.SpawnObject(
                Plugin.Instance.ToolGunPrefab.name,
                Utils.PackVector3ToLong(position),
                NetworkUtils.PackQuaternionToLong(rotation)
            );
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Plugin), nameof(Plugin.SpawnAirboat))]
    public static void Postfix_SpawnAirboat(Vector3 position, Quaternion rotation)
    {
        if (IsNetworkActive() && Plugin.Instance.AirboatPrefab != null)
        {
            Debug.Log($"[GPhysNetworking] Sending network spawn for '{Plugin.Instance.AirboatPrefab.name}' at {position} rot {rotation}");
            NetworkUtils.SpawnObject(
                Plugin.Instance.AirboatPrefab.name,
                Utils.PackVector3ToLong(position),
                NetworkUtils.PackQuaternionToLong(rotation)
            );
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Plugin), nameof(Plugin.SpawnBarnacle))]
    public static void Postfix_SpawnBarnacle(Vector3 position, Quaternion rotation)
    {
        if (IsNetworkActive() && Plugin.Instance.BarnaclePrefab != null)
        {
            Debug.Log($"[GPhysNetworking] Sending network spawn for '{Plugin.Instance.BarnaclePrefab.name}' at {position} rot {rotation}");
            NetworkUtils.SpawnObject(
                Plugin.Instance.BarnaclePrefab.name,
                Utils.PackVector3ToLong(position),
                NetworkUtils.PackQuaternionToLong(rotation)
            );
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Plugin), nameof(Plugin.SpawnBomb))]
    public static void Postfix_SpawnBomb(Vector3 position, Quaternion rotation)
    {
        if (IsNetworkActive() && Plugin.Instance.BombPrefab != null)
        {
            Debug.Log($"[GPhysNetworking] Sending network spawn for '{Plugin.Instance.BombPrefab.name}' at {position} rot {rotation}");
            NetworkUtils.SpawnObject(
                Plugin.Instance.BombPrefab.name,
                Utils.PackVector3ToLong(position),
                NetworkUtils.PackQuaternionToLong(rotation)
            );
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Plugin), nameof(Plugin.SpawnBullSquid))]
    public static void Postfix_SpawnBullSquid(Vector3 position, Quaternion rotation)
    {
        if (IsNetworkActive() && Plugin.Instance.BullsquidPrefab != null)
        {
            Debug.Log($"[GPhysNetworking] Sending network spawn for '{Plugin.Instance.BullsquidPrefab.name}' at {position} rot {rotation}");
            NetworkUtils.SpawnObject(
                Plugin.Instance.BullsquidPrefab.name,
                Utils.PackVector3ToLong(position),
                NetworkUtils.PackQuaternionToLong(rotation)
            );
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Plugin), nameof(Plugin.SpawnCustomObject))]
    public static void Postfix_SpawnCustomObject(string name, Vector3 position, Quaternion rotation)
    {
        if (IsNetworkActive())
        {
            Debug.Log($"[GPhysNetworking] Sending network spawn for '{name}' at {position} rot {rotation}");
            NetworkUtils.SpawnObject(
                name,
                Utils.PackVector3ToLong(position),
                NetworkUtils.PackQuaternionToLong(rotation)
            );
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Plugin), nameof(Plugin.SpawnHeadcrab))]
    public static void Postfix_SpawnHeadcrab(Vector3 position, Quaternion rotation)
    {
        if (IsNetworkActive() && Plugin.Instance.HeadcrabPrefab != null)
        {
            Debug.Log($"[GPhysNetworking] Sending network spawn for '{Plugin.Instance.HeadcrabPrefab.name}' at {position} rot {rotation}");
            NetworkUtils.SpawnObject(
                Plugin.Instance.HeadcrabPrefab.name,
                Utils.PackVector3ToLong(position),
                NetworkUtils.PackQuaternionToLong(rotation)
            );
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Plugin), nameof(Plugin.SpawnHoundeye))]
    public static void Postfix_SpawnHoundeye(Vector3 position, Quaternion rotation)
    {
        if (IsNetworkActive() && Plugin.Instance.HoundeyePrefab != null)
        {
            Debug.Log($"[GPhysNetworking] Sending network spawn for '{Plugin.Instance.HoundeyePrefab.name}' at {position} rot {rotation}");
            NetworkUtils.SpawnObject(
                Plugin.Instance.HoundeyePrefab.name,
                Utils.PackVector3ToLong(position),
                NetworkUtils.PackQuaternionToLong(rotation)
            );
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Plugin), nameof(Plugin.SpawnMagnet))]
    public static void Postfix_SpawnMagnet(Vector3 position, Quaternion rotation)
    {
        if (IsNetworkActive() && Plugin.Instance.MagnetPrefab != null)
        {
            Debug.Log($"[GPhysNetworking] Sending network spawn for '{Plugin.Instance.MagnetPrefab.name}' at {position} rot {rotation}");
            NetworkUtils.SpawnObject(
                Plugin.Instance.MagnetPrefab.name,
                Utils.PackVector3ToLong(position),
                NetworkUtils.PackQuaternionToLong(rotation)
            );
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Plugin), nameof(Plugin.SpawnPetRock))]
    public static void Postfix_SpawnPetRock(Vector3 position, Quaternion rotation)
    {
        if (IsNetworkActive() && Plugin.Instance.petRockPrefab != null)
        {
            Debug.Log($"[GPhysNetworking] Sending network spawn for '{Plugin.Instance.petRockPrefab.name}' at {position} rot {rotation}");
            NetworkUtils.SpawnObject(
                Plugin.Instance.petRockPrefab.name,
                Utils.PackVector3ToLong(position),
                NetworkUtils.PackQuaternionToLong(rotation)
            );
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Plugin), nameof(Plugin.SpawnPhysGun))]
    public static void Postfix_SpawnPhysGun(Vector3 position, Quaternion rotation)
    {
        if (IsNetworkActive() && Plugin.Instance.PhysGunPrefab != null)
        {
            Debug.Log($"[GPhysNetworking] Sending network spawn for '{Plugin.Instance.PhysGunPrefab.name}' at {position} rot {rotation}");
            NetworkUtils.SpawnObject(
                Plugin.Instance.PhysGunPrefab.name,
                Utils.PackVector3ToLong(position),
                NetworkUtils.PackQuaternionToLong(rotation)
            );
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Plugin), nameof(Plugin.SpawnPistol))]
    public static void Postfix_SpawnPistol(Vector3 position, Quaternion rotation)
    {
        if (IsNetworkActive() && Plugin.Instance.PistolPrefab != null)
        {
            Debug.Log($"[GPhysNetworking] Sending network spawn for '{Plugin.Instance.PistolPrefab.name}' at {position} rot {rotation}");
            NetworkUtils.SpawnObject(
                Plugin.Instance.PistolPrefab.name,
                Utils.PackVector3ToLong(position),
                NetworkUtils.PackQuaternionToLong(rotation)
            );
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Plugin), nameof(Plugin.SpawnRocketLauncher))]
    public static void Postfix_SpawnRocketLauncher(Vector3 position, Quaternion rotation)
    {
        if (IsNetworkActive() && Plugin.Instance.RocketLauncherPrefab != null)
        {
            Debug.Log($"[GPhysNetworking] Sending network spawn for '{Plugin.Instance.RocketLauncherPrefab.name}' at {position} rot {rotation}");
            NetworkUtils.SpawnObject(
                Plugin.Instance.RocketLauncherPrefab.name,
                Utils.PackVector3ToLong(position),
                NetworkUtils.PackQuaternionToLong(rotation)
            );
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Plugin), nameof(Plugin.SpawnScoutCar))]
    public static void Postfix_SpawnScoutCar(Vector3 position, Quaternion rotation)
    {
        if (IsNetworkActive() && Plugin.Instance.ScoutCarPrefab != null)
        {
            Debug.Log($"[GPhysNetworking] Sending network spawn for '{Plugin.Instance.ScoutCarPrefab.name}' at {position} rot {rotation}");
            NetworkUtils.SpawnObject(
                Plugin.Instance.ScoutCarPrefab.name,
                Utils.PackVector3ToLong(position),
                NetworkUtils.PackQuaternionToLong(rotation)
            );
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Plugin), nameof(Plugin.SpawnSCP096))]
    public static void Postfix_SpawnSCP096(Vector3 position, Quaternion rotation)
    {
        if (IsNetworkActive() && Plugin.Instance.SCP096Prefab != null)
        {
            Debug.Log($"[GPhysNetworking] Sending network spawn for '{Plugin.Instance.SCP096Prefab.name}' at {position} rot {rotation}");
            NetworkUtils.SpawnObject(
                Plugin.Instance.SCP096Prefab.name,
                Utils.PackVector3ToLong(position),
                NetworkUtils.PackQuaternionToLong(rotation)
            );
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Plugin), nameof(Plugin.SpawnSCP173))]
    public static void Postfix_SpawnSCP173(Vector3 position, Quaternion rotation)
    {
        if (IsNetworkActive() && Plugin.Instance.SCP173Prefab != null)
        {
            Debug.Log($"[GPhysNetworking] Sending network spawn for '{Plugin.Instance.SCP173Prefab.name}' at {position} rot {rotation}");
            NetworkUtils.SpawnObject(
                Plugin.Instance.SCP173Prefab.name,
                Utils.PackVector3ToLong(position),
                NetworkUtils.PackQuaternionToLong(rotation)
            );
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Plugin), nameof(Plugin.SpawnStasisCannon))]
    public static void Postfix_SpawnStasisCannon(Vector3 position, Quaternion rotation)
    {
        if (IsNetworkActive() && Plugin.Instance.StasisCannonPrefab != null)
        {
            Debug.Log($"[GPhysNetworking] Sending network spawn for '{Plugin.Instance.StasisCannonPrefab.name}' at {position} rot {rotation}");
            NetworkUtils.SpawnObject(
                Plugin.Instance.StasisCannonPrefab.name,
                Utils.PackVector3ToLong(position),
                NetworkUtils.PackQuaternionToLong(rotation)
            );
        }
    }
} 