using System;
using System.Collections.Generic;
using System.Linq;
using GPhys.Types;
using GPhys.Types.Abstracts;
using GPhys.Types.Objects;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using GPhys.Types.Objects.SCPs;
using GPhys.Types.Objects.Vehicles;

namespace GPhys.Addons
{
    public class BombNetworkingAddon : IGPhysPlugin
    {
        public string PluginName => "BombNetworkingAddon";
        public string Version => "0.1";
        private HashSet<int> knownServerObjects = new HashSet<int>();
        private HashSet<int> knownLocalObjects = new HashSet<int>();
        private bool polling = false;
        private string serverUrlMain = "https://gphysplugin-networking.vercel.app"; // change to your server url if you want to have your own
        private string serverUrl = "https://gphysplugin-networking.vercel.app/objects";
        private Plugin mainPlugin;

        public void Initialize(Plugin mainPlugin)
        {
            Debug.Log("[BombNetworkingAddon] Initialize called - hello");
            this.mainPlugin = mainPlugin;
            if (!polling)
            {
                polling = true;
                Debug.Log("[BombNetworkingAddon] Starting polling coroutine");
                mainPlugin.StartCoroutine(ClearOldObjects());
                mainPlugin.StartCoroutine(PollServerObjects());
            }
        }
        public void RegisterSpawnables() { }
        public void Update() 
        {
            if (mainPlugin != null && mainPlugin.createdObjects != null)
            {
                foreach (var obj in mainPlugin.createdObjects)
                {
                    if (obj != null)
                    {
                        int objId = obj.GetInstanceID();
                        if (!knownLocalObjects.Contains(objId))
                        {
                            knownLocalObjects.Add(objId);
                            mainPlugin.StartCoroutine(SendObjectToServer(obj));
                        }
                    }
                }
            }
        }
        public void Cleanup() { }
        public void OnGUI() { }
        private System.Collections.IEnumerator SendObjectToServer(GameObject obj)
        {
            string objectType = DetermineObjectType(obj);
            if (string.IsNullOrEmpty(objectType))
            {
                Debug.LogWarning("[BombNetworkingAddon] Could not determine object type for: " + obj.name);
                yield break;
            }

            var data = new
            {
                object_type = objectType,
                position = new
                {
                    x = obj.transform.position.x,
                    y = obj.transform.position.y,
                    z = obj.transform.position.z
                },
                rotation = new
                {
                    x = obj.transform.rotation.x,
                    y = obj.transform.rotation.y,
                    z = obj.transform.rotation.z,
                    w = obj.transform.rotation.w
                },
                owner_id = "local_player"
            };

            string json = JsonConvert.SerializeObject(data);
            Debug.Log("[BombNetworkingAddon] Sending to server: " + json);

            using (UnityWebRequest req = UnityWebRequest.Post($"{serverUrlMain}/spawn_object", ""))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");

                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("[BombNetworkingAddon] Successfully sent object to server: " + req.downloadHandler.text);
                }
                else
                {
                    Debug.LogError("[BombNetworkingAddon] Failed to send object to server: " + req.error);
                }
            }
        }
        private string DetermineObjectType(GameObject obj) // W chatgpt idk if works lmao it sends to server soo yeah
        {
            if (obj.GetComponent<ExplosiveObject>() != null)
                return "Bomb";
            if (obj.GetComponent<PhysGun>() != null)
                return "PhysGun";
            if (obj.GetComponent<ToolGun>() != null)
                return "ToolGun";
            if (obj.GetComponent<Pistol>() != null)
                return "Pistol";
            if (obj.GetComponent<RocketLauncher>() != null)
                return "RocketLauncher";
            if (obj.GetComponent<StasisCannon>() != null)
                return "StasisCannon";
            if (obj.GetComponent<Magnet>() != null)
                return "Magnet";
            if (obj.GetComponent<HeadcrabAI>() != null)
                return "Headcrab";
            if (obj.GetComponent<Houndeye>() != null)
                return "Houndeye";
            if (obj.GetComponent<BullSquid>() != null)
                return "BullSquid";
            if (obj.GetComponent<PetRock>() != null)
                return "PetRock";
            if (obj.GetComponent<AirboatController>() != null)
                return "Airboat";
            if (obj.GetComponent<ScoutCar>() != null)
                return "ScoutCar";
            if (obj.GetComponent<SCP173>() != null)
                return "SCP173";
            if (obj.GetComponent<SCP096>() != null)
                return "SCP096";
            if (obj.GetComponent<Barnacle>() != null)
                return "Barnacle";
            if (obj.GetComponent<PhysicObject>() != null)
                return "PhysicObject";

            string name = obj.name.ToLower();
            if (name.Contains("bomb"))
                return "Bomb";
            if (name.Contains("physgun") || name.Contains("phys_gun"))
                return "PhysGun";
            if (name.Contains("toolgun") || name.Contains("tool_gun"))
                return "ToolGun";
            if (name.Contains("pistol"))
                return "Pistol";
            if (name.Contains("rocket") || name.Contains("launcher"))
                return "RocketLauncher";
            if (name.Contains("stasis") || name.Contains("cannon"))
                return "StasisCannon";
            if (name.Contains("magnet"))
                return "Magnet";
            if (name.Contains("headcrab"))
                return "Headcrab";
            if (name.Contains("houndeye"))
                return "Houndeye";
            if (name.Contains("bullsquid") || name.Contains("bull_squid"))
                return "BullSquid";
            if (name.Contains("petrock") || name.Contains("pet_rock"))
                return "PetRock";
            if (name.Contains("airboat"))
                return "Airboat";
            if (name.Contains("scoutcar") || name.Contains("scout_car"))
                return "ScoutCar";
            if (name.Contains("scp173"))
                return "SCP173";
            if (name.Contains("scp096"))
                return "SCP096";
            if (name.Contains("barnacle"))
                return "Barnacle";

            return null;
        }
        private System.Collections.IEnumerator PollServerObjects()
        {
            Debug.Log("[BombNetworkingAddon] PollServerObjects coroutine started");
            while (true)
            {
                Debug.Log("[BombNetworkingAddon] Polling server...");
                using (UnityWebRequest req = UnityWebRequest.Get(serverUrl))
                {
                    yield return req.SendWebRequest();
                    if (req.result == UnityWebRequest.Result.Success)
                    {
                        Debug.Log("[BombNetworkingAddon] Server response: " + req.downloadHandler.text);
                        try
                        {
                            var json = req.downloadHandler.text;
                            Debug.Log("[BombNetworkingAddon] Attempting to parse JSON...");
                            var objectList = JsonConvert.DeserializeObject<ObjectListWrapper>(json);
                            Debug.Log("[BombNetworkingAddon] JsonConvert result - objectList: " + (objectList != null ? "not null" : "null"));
                            if (objectList != null)
                            {
                                Debug.Log("[BombNetworkingAddon] objects array: " + (objectList.objects != null ? "not null, length: " + objectList.objects.Length : "null"));
                            }
                            
                            if (objectList != null && objectList.objects != null)
                            {
                                Debug.Log("[BombNetworkingAddon] Found " + objectList.objects.Length + " objects on server");
                                foreach (var obj in objectList.objects)
                                {
                                    if (!knownServerObjects.Contains(obj.id))
                                    {
                                        knownServerObjects.Add(obj.id);
                                        Debug.Log("[BombNetworkingAddon] hello - New object ID: " + obj.id + " Type: " + obj.object_type);
                                        
                                        Vector3 spawnPosition = new Vector3(obj.position.x, obj.position.y, obj.position.z);
                                        Quaternion spawnRotation = new Quaternion(obj.rotation.x, obj.rotation.y, obj.rotation.z, obj.rotation.w);
                                        
                                        SpawnObjectByType(obj.object_type, spawnPosition, spawnRotation);
                                        Debug.Log("[BombNetworkingAddon] Spawned " + obj.object_type + " at position: " + spawnPosition);
                                    }
                                }
                            }
                            else
                            {
                                Debug.Log("[BombNetworkingAddon] No objects found or null response");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError("[BombNetworkingAddon] JSON parse error: " + ex.Message);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[BombNetworkingAddon] Server poll failed: " + req.error);
                    }
                }
                yield return new WaitForSeconds(1f);
            }
        }
        private void SpawnObjectByType(string objectType, Vector3 position, Quaternion rotation)
        {
            switch (objectType.ToLower())
            {
                case "bomb":
                    mainPlugin.SpawnBomb(position, rotation);
                    break;
                case "physgun":
                    mainPlugin.SpawnPhysGun(position, rotation);
                    break;
                case "toolgun":
                    mainPlugin.SpawnToolGun(position, rotation);
                    break;
                case "pistol":
                    mainPlugin.SpawnPistol(position, rotation);
                    break;
                case "rocketlauncher":
                    mainPlugin.SpawnRocketLauncher(position, rotation);
                    break;
                case "stasiscannon":
                    mainPlugin.SpawnStasisCannon(position, rotation);
                    break;
                case "magnet":
                    mainPlugin.SpawnMagnet(position, rotation);
                    break;
                case "headcrab":
                    mainPlugin.SpawnHeadcrab(position, rotation);
                    break;
                case "houndeye":
                    mainPlugin.SpawnHoundeye(position, rotation);
                    break;
                case "bullsquid":
                    mainPlugin.SpawnBullSquid(position, rotation);
                    break;
                case "petrock":
                    mainPlugin.SpawnPetRock(position, rotation);
                    break;
                case "airboat":
                    mainPlugin.SpawnAirboat(position, rotation);
                    break;
                case "scoutcar":
                    mainPlugin.SpawnScoutCar(position, rotation);
                    break;
                case "scp173":
                    mainPlugin.SpawnSCP173(position, rotation);
                    break;
                case "scp096":
                    mainPlugin.SpawnSCP096(position, rotation);
                    break;
                case "barnacle":
                    mainPlugin.SpawnBarnacle(position, rotation, BarnacleType.Default);
                    break;
                default:
                    Debug.LogWarning("[BombNetworkingAddon] Unknown object type: " + objectType);
                    break;
            }
        }
        private System.Collections.IEnumerator ClearOldObjects()
        {
            Debug.Log("[BombNetworkingAddon] Clearing old objects from server...");
            using (UnityWebRequest req = UnityWebRequest.Post($"{serverUrlMain}/clear_all_objects", ""))
            {
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("[BombNetworkingAddon] Server cleared old objects. Response: " + req.downloadHandler.text);
                }
                else
                {
                    Debug.LogWarning("[BombNetworkingAddon] Failed to clear old objects from server: " + req.error);
                }
            }
        }

        [Serializable]
        private class ObjectListWrapper
        {
            public ObjectData[] objects;
            public int count;
            public bool success;
        }
        [Serializable]
        private class ObjectData
        {
            public int id;
            public string object_type;
            public PositionData position;
            public RotationData rotation;
        }
        
        [Serializable]
        private class PositionData
        {
            public float x;
            public float y;
            public float z;
        }
        
        [Serializable]
        private class RotationData
        {
            public float x;
            public float y;
            public float z;
            public float w;
        }
    }
} 