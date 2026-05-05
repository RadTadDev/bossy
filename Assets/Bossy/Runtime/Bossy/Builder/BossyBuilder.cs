namespace Bossy
{
    /// <summary>
    /// Builds a new Bossy runtime.
    /// </summary>
    public class BossyBuilder
    {
        private BossyBuilder() { }

        /// <summary>
        /// Starts the process by finding commands.
        /// </summary>
        /// <returns>The builder to specify how to find commands.</returns>
        public static IBossyRegisterCommandsStep GetCommands()
        {
            return new BossyRegisterCommandsBuilder();
        }
    }
}