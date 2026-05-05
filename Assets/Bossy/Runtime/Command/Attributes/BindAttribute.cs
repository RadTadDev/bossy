using System;

namespace Bossy.Command
{
    /// <summary>
    /// This item will be bound to an instance registered via the <see cref="IBossyBinder"/>.
    /// Make sure you have added one to your <see cref="BossyBuilder"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class BindAttribute : Attribute { }
}