using System;
using System.IO;
using System.Reflection;
using System.Text;
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
            var hash = difficultyBeatmap.level.levelID.Replace(CustomLevelLoader.kCustomLevelPrefixId, "");
            var difficulty = difficultyBeatmap.difficulty.ToString();
            var characteristic = difficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName;
            return $"{hash}%7C_{difficulty}_Solo{characteristic}";
        }

        public static async Task<T> ParseWebResponse<T>(IHttpResponse webResponse)
        {
            if (webResponse.Successful && (await webResponse.ReadAsByteArrayAsync()).Length > 3)
            {
                using (var streamReader = new StreamReader(await webResponse.ReadAsStreamAsync()))
                {
                    using (var jsonTextReader = new JsonTextReader(streamReader))
                    {
                        var jsonSerializer = new JsonSerializer();
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
            foreach (var info in originalType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic))
            {
                info.SetValue(upgradedDummyComponent, info.GetValue(monoBehaviour));
            }
            UnityEngine.Object.DestroyImmediate(monoBehaviour);
            var goState = gameObject.activeSelf;
            gameObject.SetActive(false);
            var upgradedMonoBehaviour = gameObject.AddComponent(upgradingType);
            foreach (var info in upgradingType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic))
            {
                info.SetValue(upgradedMonoBehaviour, info.GetValue(upgradedDummyComponent));
            }
            gameObject.SetActive(goState);
            return upgradedMonoBehaviour;
        }

        public static bool DoesNotHaveAlphaNumericCharacters(this string str)
        {
            var sb = new StringBuilder();
            foreach (var c in str)
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                {
                    sb.Append(c);
                }
            }
            return sb.Length == 0;
        }

        public static string RemoveSpecialCharacters(this string str)
        {
            var sb = new StringBuilder();
            foreach (var c in str)
            {
                if (c <= 255)
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
    }
}
