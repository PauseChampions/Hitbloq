using SiraUtil;

namespace Hitbloq.Utilities
{
    internal class Utils
    {
        public static string DifficultyBeatmapToString(IDifficultyBeatmap difficultyBeatmap)
        {
            string hash = difficultyBeatmap.level.levelID.Replace(CustomLevelLoader.kCustomLevelPrefixId, "");
            string difficulty = difficultyBeatmap.difficulty.ToString();
            string characteristic = difficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName;
            return $"{hash}%7C_{difficulty}_Solo{characteristic}";
        }

        public static T ParseWebResponse<T>(WebResponse webResponse)
        {
            if (webResponse.IsSuccessStatusCode && webResponse.ContentToBytes().Length > 3)
            {
                return webResponse.ContentToJson<T>();
            }
            return default(T);
        }
    }
}
