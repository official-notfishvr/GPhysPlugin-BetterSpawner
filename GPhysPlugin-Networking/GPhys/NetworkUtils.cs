using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace GPhys.Addons
{
    // https://github.com/developer9998/GorillaCraft/blob/main/GorillaCraft/Utilities/NetworkUtils.cs
    public class NetworkUtils
    {
        public enum NetworkType
        {
            SpawnObjectCode = 150
        }

        public static void SpawnObject(string prefabName, long packedPosition, long packedRotation)
        {
            object[] content = new object[]
            {
                prefabName,
                packedPosition,
                packedRotation
            };
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions()
            {
                Receivers = ReceiverGroup.Others
            };
            PhotonNetwork.RaiseEvent((int)NetworkType.SpawnObjectCode, content, raiseEventOptions, SendOptions.SendReliable);
        }

        public static long PackQuaternionToLong(Quaternion q)
        {
            long x = (long)(Mathf.Clamp(q.x, -1f, 1f) * 32767) & 0xFFFF;
            long y = (long)(Mathf.Clamp(q.y, -1f, 1f) * 32767) & 0xFFFF;
            long z = (long)(Mathf.Clamp(q.z, -1f, 1f) * 32767) & 0xFFFF;
            long w = (long)(Mathf.Clamp(q.w, -1f, 1f) * 32767) & 0xFFFF;
            return (x << 48) | (y << 32) | (z << 16) | w;
        }

        public static Quaternion UnpackQuaternionFromLong(long packed)
        {
            float x = ((packed >> 48) & 0xFFFF) / 32767f;
            float y = ((packed >> 32) & 0xFFFF) / 32767f;
            float z = ((packed >> 16) & 0xFFFF) / 32767f;
            float w = (packed & 0xFFFF) / 32767f;
            return new Quaternion(x, y, z, w);
        }
    }
}
