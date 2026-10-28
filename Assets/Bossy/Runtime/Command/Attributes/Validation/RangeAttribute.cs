using System;
using System.Globalization;
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

        public override string Describe()
        {
            return $"range [{Bound(Min)}, {Bound(Max)}]";
        }

        private static string Bound(float value)
        {
            return value switch
            {
                float.MaxValue or float.PositiveInfinity => "∞",
                float.MinValue or float.NegativeInfinity => "-∞",
                _                                        => value.ToString(CultureInfo.InvariantCulture)
            };
        }
    }
}