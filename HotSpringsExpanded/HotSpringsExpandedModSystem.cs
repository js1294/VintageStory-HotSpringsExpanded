using HotSpringsExpanded.Processing;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace HotSpringsExpanded
{
    /// <summary>
    /// Main mod system for HotSpringsExpanded.
    /// Acts as the entry point discovered by the game's mod loader and delegates to sub-systems.
    /// </summary>
    public class HotSpringsExpandedModSystem : ModSystem
    {
        private BlockProcessor? blockProcessor;

        /// <summary>
        /// Called on both server and client during mod loading.
        /// Useful for registering block/entity classes on both sides.
        /// </summary>
        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            blockProcessor = new();
            blockProcessor.Initialize(api);
        }

        /// <summary>
        /// Starts server-side initialization.
        /// </summary>
        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            blockProcessor?.InitializeServer(api);
        }
    }
}
