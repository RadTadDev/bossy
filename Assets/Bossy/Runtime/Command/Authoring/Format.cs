using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Bossy.Command
{
    /// <summary>
    /// A formatter to assist in standardizing output messages.
    /// </summary>
    public static class Format
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
        /// Bossy gray.
        /// </summary>
        public static Color Gray = new(0.7f, 0.7f, 0.7f);
        
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

        /// <summary>
        /// Aligns a list of items and prints them.
        /// </summary>
        /// <param name="enumerable">The list.</param>
        /// <param name="header">A function to generate the header for each list item.</param>
        /// <param name="body">A function to generate the body for each list item.</param>
        /// <param name="ctx">The command context to print to.</param>
        /// <param name="headerColor">The optional header color.</param>
        /// <param name="bodyColor">The optional body color.</param>
        /// <typeparam name="T">The type of each element.</typeparam>
        /// <typeparam name="TOut">The type being outputted.</typeparam>
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
            ctx.Write(Align(enumerable, header, body, headerColor, bodyColor));
        }
        
        /// <summary>
        /// Aligns a list of items.
        /// </summary>
        /// <param name="enumerable">The list.</param>
        /// <param name="header">A function to generate the header for each list item.</param>
        /// <param name="body">A function to generate the body for each list item.</param>
        /// <param name="headerColor">The optional header color.</param>
        /// <param name="bodyColor">The optional body color.</param>
        /// <typeparam name="T">The type of each element.</typeparam>
        /// <typeparam name="TOut">The type being outputted.</typeparam>
        public static string Align<T, TOut>
        (
            IEnumerable<T> enumerable,
            Func<T, TOut> header,
            Func<T, TOut> body,
            Color headerColor = default,
            Color bodyColor = default
        )
        {
            var builder = new StringBuilder();
            var list = enumerable.ToList();
            var max = list.Aggregate(0, (current, item) => Mathf.Max(current, StripMarkup(header(item).ToString()).Length));
            max++;
            
            foreach (var item in list)
            {
                var prefix = header(item).ToString();
                var first = prefix + new string(' ', max - StripMarkup(prefix).Length);
                var second = body(item).ToString();

                if (headerColor != default)
                {
                    first = Color(first, headerColor);
                }

                if (bodyColor != default)
                {
                    second = Color(second, bodyColor);
                }
                
                builder.AppendLine($"{first}: {second}");
            }
            
            return builder.ToString();
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
        /// Formats a message as a warning.
        /// </summary>
        /// <param name="value">The value to turn into a warning.</param>
        /// <param name="indentCount">The number of spaces to indent.</param>
        public static string Warning(object value, int indentCount = 0) => $"{new string(' ', indentCount)}{Color("Warning:", Yellow)} {value}";
        
        /// <summary>
        /// Formats a message as an error.
        /// </summary>
        /// <param name="value">The value to turn into an error.</param>
        /// <param name="ctx">The context to use to print.</param>
        /// <param name="indentCount">The number of spaces to indent.</param>
        public static void Error(object value, SimpleContext ctx, int indentCount = 0)
        {
            ctx.Write(Error(value, indentCount));
        }

        /// <summary>
        /// Formats a message as an error.
        /// </summary>
        /// <param name="value">The value to turn into an error.</param>
        /// <param name="indentCount">The number of spaces to indent.</param>
        public static string Error(object value, int indentCount = 0) => $"{new string(' ', indentCount)}{Color("Error:", Red)} {value}";
        
        /// <summary>
        /// Adds bolding to a string.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The emboldened string.</returns>
        public static string Bold(object value) => $"<b>{value}</b>";

        /// <summary>
        /// Adds italics to a string.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The italicized string.</returns>
        public static string Italics(object value) => $"<i>{value}</i>";
        
        /// <summary>
        /// Adds a color to a string.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="color">The color.</param>
        public static string Color(object value, Color color) => $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{value}</color>";
        
        /// <summary>
        /// Renders an object consistently.
        /// </summary>
        /// <param name="value">The value to render.</param>
        /// <returns>The rendered string.</returns>
        public static string Render(object value) => value.ToString().Replace(" ", "\u00A0");

        private static string StripMarkup(string text)
        {
            var pattern = new Regex(@"<(\w+)[^>]*>.*?</\1>", RegexOptions.Compiled | RegexOptions.Singleline);
            
            return pattern.Replace(text, m =>
            {
                // Recurse to handle nested tags e.g. <b><color=#ff0000>text</color></b>
                var start = m.Value.IndexOf('>') + 1;
                var end = m.Value.LastIndexOf('<');
                var inner = m.Value.Substring(start, end - start);
                return StripMarkup(inner);
            });
        }
    }
}