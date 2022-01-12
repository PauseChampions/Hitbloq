using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SiraUtil.Web;
using UnityEngine;

namespace Hitbloq.Utilities
{
    internal static class Utils
    {
        public static string DifficultyBeatmapToString(IDifficultyBeatmap difficultyBeatmap)
        {
            string hash = difficultyBeatmap.level.levelID.Replace(CustomLevelLoader.kCustomLevelPrefixId, "");
            string difficulty = difficultyBeatmap.difficulty.ToString();
            string characteristic = difficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName;
            return $"{hash}%7C_{difficulty}_Solo{characteristic}";
        }

        public static async Task<T> ParseWebResponse<T>(IHttpResponse webResponse)
        {
            if (webResponse.Successful && (await webResponse.ReadAsByteArrayAsync()).Length > 3)
            {
                using (StreamReader streamReader = new StreamReader(await webResponse.ReadAsStreamAsync()))
                {
                    using (JsonTextReader jsonTextReader = new JsonTextReader(streamReader))
                    {
                        JsonSerializer jsonSerializer = new JsonSerializer();
                        return jsonSerializer.Deserialize<T>(jsonTextReader);
                    }
                }
            }
            return default(T);
        }

        // Yoinked from SiraUtil
        public static U Upgrade<T, U>(this T monoBehaviour) where U : T where T : MonoBehaviour
        {
            return (U)Upgrade(monoBehaviour, typeof(U));
        }

        public static Component Upgrade(this Component monoBehaviour, Type upgradingType)
        {
            var originalType = monoBehaviour.GetType();

            var gameObject = monoBehaviour.gameObject;
            var upgradedDummyComponent = Activator.CreateInstance(upgradingType);
            foreach (FieldInfo info in originalType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic))
            {
                info.SetValue(upgradedDummyComponent, info.GetValue(monoBehaviour));
            }
            UnityEngine.Object.DestroyImmediate(monoBehaviour);
            bool goState = gameObject.activeSelf;
            gameObject.SetActive(false);
            var upgradedMonoBehaviour = gameObject.AddComponent(upgradingType);
            foreach (FieldInfo info in upgradingType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic))
            {
                info.SetValue(upgradedMonoBehaviour, info.GetValue(upgradedDummyComponent));
            }
            gameObject.SetActive(goState);
            return upgradedMonoBehaviour;
        }

        public static bool HasNonASCIIChars(this string str)
        {
            return (System.Text.Encoding.UTF8.GetByteCount(str) != str.Length);
        }
    }
}
