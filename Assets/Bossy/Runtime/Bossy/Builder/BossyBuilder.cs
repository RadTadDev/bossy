namespace Bossy
{
    public class BossyBuilder
    {
        private BossyBuilder() { }

        public static IBossyRegisterCommandsStep GetCommands()
        {
            return new BossyRegisterCommandsBuilder();
        }
    }
}