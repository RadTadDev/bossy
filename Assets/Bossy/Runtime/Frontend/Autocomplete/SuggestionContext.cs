using Bossy.Schema;

namespace Bossy.Frontend.Autocomplete
{
    public class SuggestionContext
    {
        /// <summary>
        /// The Bossy context.
        /// </summary>
        public readonly BossyContext BossyContext;
        
        /// <summary>
        /// The currently identified schema.
        /// </summary>
        public readonly CommandSchema Schema;

        /// <summary>
        /// Creates a new suggestion context.
        /// </summary>
        /// <param name="bossyContext">The Bossy context.</param>
        /// <param name="schema">The currently identified schema.</param>
        public SuggestionContext(BossyContext bossyContext, CommandSchema schema)
        {
            BossyContext = bossyContext;
            Schema = schema;
        }
    }
}