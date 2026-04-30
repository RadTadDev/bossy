using System;
using Bossy.Frontend.Parsing;
using Bossy.Settings;

namespace Bossy.Frontend
{
    internal class FrontEndFactory
    {
        private readonly Parser _parser;
        private readonly BossyCliSettings _cliSettings;
        private readonly BossyInputSettings _inputSettings;
        
        public FrontEndFactory(Parser parser, BossyInputSettings inputSettings, BossyCliSettings cliSettings)
        {
            _parser = parser;
            _cliSettings = cliSettings;
            _inputSettings = inputSettings;
        }

        public IContentView Create(FrontendType frontendType)
        {
            switch (frontendType)
            {
                case FrontendType.CommandLine:
                    return new CliContentView(_parser, _cliSettings, _inputSettings);
                case FrontendType.Graphical:
                    return new GuiContentView();
                default:
                    throw new ArgumentOutOfRangeException(nameof(frontendType), frontendType, null);
            }
        }
    }
}