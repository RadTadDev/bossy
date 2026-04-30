using Bossy.Settings;

namespace Bossy.Tests.Utils
{
    /// <summary>
    /// A mock settings source.
    /// </summary>
    public class MockSettingsSource : ISettingsSource
    {
        private string _json;
        public MockSettingsSource(string json) => _json = json;
        public string LoadJson() => _json;
        public void SaveJson(string json) => _json = json;
    }
}