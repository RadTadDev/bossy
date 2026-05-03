using System;
using Bossy.Utils;

namespace Bossy.Command
{
    /// <summary>
    /// Validates that an argument is within a certain range.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class RangeAttribute : ArgumentValidationAttribute
    {
        public readonly float Min;
        public readonly float Max;
        
        public RangeAttribute(float min, float max)
        {
            Min = min;
            Max = max;
        }

        public override ArgumentValidationResult Validate(object value)
        {
            if (!float.TryParse(value?.ToString(), out var num))
            {
                return ArgumentValidationResult.Fail($"Input type '{value?.GetType().GetFriendlyName()}' is not numeric.");
            }

            if (num >= Min && num <= Max)
            {
                return ArgumentValidationResult.Pass();
            }
            
            return ArgumentValidationResult.Fail($"{num} is outside the range [{Min}, {Max}]");
        }
    }
}