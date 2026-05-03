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

        public IUserInterfaceView Create(FrontendType frontendType)
        {
            return frontendType switch
            {
                FrontendType.CommandLine => new CliUserInterfaceView(_parser, _cliSettings, _inputSettings),
                FrontendType.Graphical => new GuiUserInterfaceView(),
                FrontendType.CommandDisplay => new CommandDisplay(_parser, _cliSettings, _inputSettings),
                _ => throw new ArgumentOutOfRangeException(nameof(frontendType), frontendType, null)
            };
        }
    }
}