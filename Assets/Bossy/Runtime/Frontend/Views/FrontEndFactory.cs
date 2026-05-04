using System;
using Bossy.Frontend.Parsing;
using Bossy.Settings;

namespace Bossy.Frontend
{
    /// <summary>
    /// Creates front ends.
    /// </summary>
    internal class FrontEndFactory
    {
        private readonly Parser _parser;
        private readonly BossyCliSettings _cliSettings;
        private readonly BossyInputSettings _inputSettings;
        
        /// <summary>
        /// Creates a new factory.
        /// </summary>
        /// <param name="parser">The parser.</param>
        /// <param name="inputSettings">The input settings.</param>
        /// <param name="cliSettings">The Cli settings.</param>
        public FrontEndFactory(Parser parser, BossyInputSettings inputSettings, BossyCliSettings cliSettings)
        {
            _parser = parser;
            _cliSettings = cliSettings;
            _inputSettings = inputSettings;
        }

        /// <summary>
        /// Creates a new front end.
        /// </summary>
        /// <param name="frontendType">The type to create.</param>
        /// <returns>The created front end.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Throws when the input type is unrecognized.</exception>
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