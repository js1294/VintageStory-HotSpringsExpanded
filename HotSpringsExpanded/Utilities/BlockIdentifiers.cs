using System.Text.RegularExpressions;
using Vintagestory.API.Common;

namespace HotSpringsExpanded.Utilities
{
    /// <summary>
    /// Static utility class for identifying hot spring bacteria, snow, and ice blocks.
    /// All methods are stateless and safe to call from any thread.
    /// </summary>
    public static class BlockIdentifiers
    {
        /// <summary>
        /// Contains all known block code path constants for snow and ice identification.
        /// Access via <c>BlockIdentifiers.Blocks.SnowBlock</c>, etc.
        /// </summary>
        public static class Blocks
        {
            /// <summary>
            /// Block code path for a full snow block (level 8).
            /// In VS 1.22+ this is "snowblock" (previously "snow" in older versions).
            /// </summary>
            public const string SnowBlock = "snowblock";

            /// <summary>
            /// Block code path prefix for partial snow layers (levels 1-7).
            /// The level number follows immediately after the prefix (e.g., "snowlayer-3").
            /// </summary>
            public const string SnowLayerPrefix = "snowlayer-";

            /// <summary>
            /// Block code path for a standard ice block.
            /// </summary>
            public const string IceBlock = "ice";

            /// <summary>
            /// Block code path for lake ice (forms on lake surfaces).
            /// </summary>
            public const string LakeIceBlock = "lakeice";

            /// <summary>
            /// Block code path for glacier ice (found in glacial formations).
            /// </summary>
            public const string GlacierIceBlock = "glacierice";
        }

        /// <summary>
        /// Compiled regex for extracting the temperature number from bacteria block codes.
        /// Matches the pattern "-NNdeg" at the end of the code (e.g., "-87deg").
        /// </summary>
        private static readonly Regex TemperatureRegex = new(@"-(\d+)deg$", RegexOptions.Compiled);

        /// <summary>
        /// Checks if the given block is a hot spring bacteria block.
        /// </summary>
        /// <param name="block">The block to check.</param>
        /// <returns>True if the block is a hot bacteria block.</returns>
        public static bool IsHotBacteria(Block block) => GetTemperature(block) != -1;

        /// <summary>
        /// Checks if the given block code string contains a hot spring bacteria identifier.
        /// </summary>
        /// <param name="blockCode">The full block code string to check.</param>
        /// <returns>True if the block code contains "hotspringbacteria".</returns>
        public static bool IsHotBacteria(string blockCode) => blockCode.Contains("hotspringbacteria");

        /// <summary>
        /// Checks if the given block is meltable (any type of snow or ice).
        /// </summary>
        /// <param name="block">The block to check.</param>
        /// <returns>True if the block is a snow layer, full snow block, or ice block.</returns>
        public static bool IsMeltableBlock(Block? block)
        {
            if (block == null)
                return false;

            string path = block.Code.Path;

            if (path == Blocks.SnowBlock || path.StartsWith(Blocks.SnowLayerPrefix))
                return true;

            // Ice blocks may have variant suffixes (e.g., "lakeice-3"), so use StartsWith.
            if (path == Blocks.IceBlock || path.StartsWith(Blocks.LakeIceBlock) || path.StartsWith(Blocks.GlacierIceBlock))
                return true;

            return false;
        }

        /// <summary>
        /// Checks if the given block is specifically an ice block (not snow).
        /// </summary>
        /// <param name="block">The block to check.</param>
        /// <returns>True if the block is ice, lake ice, or glacier ice.</returns>
        public static bool IsIceBlock(Block? block)
        {
            if (block == null)
                return false;

            string path = block.Code.Path;
            return path == Blocks.IceBlock || path.StartsWith(Blocks.LakeIceBlock) || path.StartsWith(Blocks.GlacierIceBlock);
        }

        /// <summary>
        /// Extracts the temperature from a hot spring bacteria block's code.
        /// Uses regex to parse the "-NNdeg" suffix from the full block code string.
        /// </summary>
        /// <param name="block">The block to check.</param>
        /// <returns>The temperature (55, 65, 74, or 87), or -1 if not a hot bacteria block.</returns>
        public static int GetTemperature(Block? block)
        {
            if (block == null)
                return -1;

            // Use the full code (domain:path) for regex matching since bacteria codes
            // include the temperature suffix in the path portion.
            string blockCode = block.Code.ToString();

            if (!IsHotBacteria(blockCode))
                return -1;

            Match match = TemperatureRegex.Match(blockCode);

            if (!match.Success)
                return -1;

            if (!int.TryParse(match.Groups[1].Value, out int temperature))
                return -1;

            return temperature;
        }
    }
}
