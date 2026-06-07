using Vintagestory.API.Common;

namespace HotSpringsExpanded.Tests
{
    /// <summary>
    /// A fake Block subclass for testing. Sets the Code property directly
    /// since Block.Code is not virtual and cannot be mocked with Moq.
    /// </summary>
    public class FakeBlock : Block
    {
        public FakeBlock(string path) : this("game", path) { }

        public FakeBlock(string domain, string path)
        {
            Code = new AssetLocation(domain, path);
        }
    }
}
