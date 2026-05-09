using System;

namespace Bossy.Frontend
{
    /// <summary>
    /// Creates front ends.
    /// </summary>
    internal class FrontEndFactory
    {
        private readonly BossyContext _context;
        
        /// <summary>
        /// Creates a new factory.
        /// </summary>
        /// <param name="context">The Bossy context.</param>
        public FrontEndFactory(BossyContext context)
        {
            _context = context;
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
                FrontendType.CommandLine => new CliUserInterfaceView(_context),
                FrontendType.CommandDisplay => new CommandDisplay(_context),
                FrontendType.Graphical => new GuiUserInterfaceView(),
                _ => throw new ArgumentOutOfRangeException(nameof(frontendType), frontendType, null)
            };
        }
    }
}