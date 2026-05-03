using System;
using UnityEngine;
using Bossy.Session;
using System.Collections.Generic;
using System.Linq;

namespace Bossy.Command
{
    /// <summary>
    /// A formatter to assist in standardizing output messages.
    /// </summary>
    public static class Formatter
    {
        /// <summary>
        /// Bossy blue.
        /// </summary>
        public static Color LightBlue = new(0.5f, 0.7f, 0.9f);

        /// <summary>
        /// Bossy green.
        /// </summary>
        public static Color Green = new(0.1f, 0.8f, 0.1f);
        
        /// <summary>
        /// Bossy yellow.
        /// </summary>
        public static Color Yellow = new(0.9f, 0.9f, 0.3f);
        
        /// <summary>
        /// Bossy red.
        /// </summary>
        public static Color Red = new(0.8f, 0.2f, 0.2f);
        
        /// <summary>
        /// Prints an enumerated list by prepending numbers.
        /// </summary>
        /// <param name="enumerable">The list.</param>
        /// <param name="ctx">The writer context.</param>
        /// <param name="zeroIndex">Whether to start at 0 or 1.</param>
        /// <typeparam name="T">The type of the items in the list.</typeparam>
        public static void Enumerate<T>(IEnumerable<T> enumerable, SimpleContext ctx, bool zeroIndex = false)
        {
            var i = zeroIndex ? 0 : 1;
            foreach (var e in enumerable)
            {
                ctx.Write($"[{i++}]: {e}");
            }
        }

        public static void Align<T, TOut>
        (
            IEnumerable<T> enumerable,
            Func<T, TOut> header,
            Func<T, TOut> body,
            SimpleContext ctx,
            Color headerColor = default,
            Color bodyColor = default
        )
        {
            var list = enumerable.ToList();
            var max = list.Aggregate(0, (current, item) => Mathf.Max(current, header(item).ToString().Length));
            
            foreach (var item in list)
            {
                var prefix = header(item).ToString();
                var first = prefix + new string(' ', max - prefix.Length);
                var second = body(item).ToString();

                if (headerColor != default)
                {
                    first = Color(first, headerColor);
                }

                if (bodyColor != default)
                {
                    second = Color(second, bodyColor);
                }
                
                ctx.Write($"{first}: {second}");
            }
        }
        
        /// <summary>
        /// Adds a color to a string.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="color">The color.</param>
        public static string Color(object value, Color color)
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{value}</color>";
        }
        
        /// <summary>
        /// Formats a message as a warning.
        /// </summary>
        /// <param name="value">The value to turn into a warning.</param>
        /// <param name="ctx">The context to use to print.</param>
        /// <param name="indentCount">The number of spaces to indent.</param>
        public static void Warning(object value, SimpleContext ctx, int indentCount = 0)
        {
            ctx.Write($"{new string(' ', indentCount)}{Color("Warning:", Yellow)} {value}");
        }
        
        /// <summary>
        /// Formats a message as an error.
        /// </summary>
        /// <param name="value">The value to turn into an error.</param>
        /// <param name="ctx">The context to use to print.</param>
        /// <param name="indentCount">The number of spaces to indent.</param>
        public static void Error(object value, SimpleContext ctx, int indentCount = 0)
        {
            ctx.Write($"{new string(' ', indentCount)}{Color("Error:", Red)} {value}");
        }

        /// <summary>
        /// Renders an object consistently.
        /// </summary>
        /// <param name="value">The value to render.</param>
        /// <returns>The rendered string.</returns>
        public static string Render(object value)
        {
            return value.ToString().Replace(" ", "\u00A0");
        }
    }
}