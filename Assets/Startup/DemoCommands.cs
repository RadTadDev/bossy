using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Bossy.Command;
using Bossy.Frontend.Autocomplete;
using UnityEngine;
using UnityEngine.SceneManagement;
using RangeAttribute = Bossy.Command.RangeAttribute;

/*
 * Hand-testing playground for autocomplete. Not part of the package.
 *
 * Things to try:
 *   dmo                         misspelled root: resolves to demo and offers the fix
 *   demo                        subcommands vs. the optional "name" value
 *   demo move <Tab>             scene object names, then Vector3 progress (1/3, 2/3, 3/3)
 *   demo move Cube 1 2 x        invalid multi-token value hint
 *   demo move Cube 1 2 3 -      switches with both names; -r, --duration, --log
 *   demo move Cube 1 2 3 --log <space>  enum values are not suggested yet (PR 5), only a hint
 *   demo move Cube 1 2 3 -d 99  Range validator: parse error on run, not in autocomplete
 *   demo load "Main M           completion inside an open quote, closes it
 *   demo load Lev && demo t     next command after an operator
 *   demo load && x              parser error for the missing positional
 *   demo tag 3 <Tab>            variadic values that need quoting (spaces, &&, -dash, quotes)
 *   demo tag hello              optional int rejects "hello", so it goes to the variadic
 *   demo pick <Tab>             40 values, no cap; try "it5", "i10", "tem"
 *   demo slow <type slowly>     function sleeps 300 ms but runs once per value slot
 *   demo broken <Tab>           a red "Suggestions for thing failed" tab slides up, nothing in the Unity console
 *   demo chain fruit <Tab>      second value's suggestions depend on the first
 *   demo file x.txt             EndsWith validator: parse error on run, not in autocomplete
 *   demo flags -fqv             aggregated bools; -fqn is an error (n takes a value)
 *   demo flags -- -x            after "--", "-x" is a value
 *   demo flags -5               negative number is a value, not a switch
 *   !demo load Level1           leading window operator
 *   demo load Level1 !          trailing window operator
 *   alias dl demo load          then "dl " and "d" to see alias completion
 */
namespace Startup
{
    [Command("demo", "Autocomplete playground. Try the subcommands.")]
    public class DemoCommand : SimpleCommand
    {
        [Optional(0, "A name, competing with the subcommand names.")]
        private string _name;

        protected override CommandStatus Execute(SimpleContext ctx)
        {
            ctx.Write($"demo name='{_name}'");
            return CommandStatus.Ok;
        }
    }

    [Command("move", "Moves a scene object.", typeof(DemoCommand))]
    public class DemoMoveCommand : SimpleCommand
    {
        [Suggest(nameof(SceneObjects))]
        [Positional(0, "The object to move.")]
        private string _target;

        [Positional(1, "Where to move it.")]
        private Vector3 _position;

        [Optional(0, "A tint color.")]
        private Color _color = Color.white;

        [Switch('r', "Move relative to the current position.")]
        private bool _relative;

        [Range(0f, 10f)]
        [Switch('d', "How long the move takes, in seconds.")]
        private float _duration = 1f;

        [Switch('l', "The log level to report at.")]
        private LogType _log = LogType.Log;

        protected override CommandStatus Execute(SimpleContext ctx)
        {
            ctx.Write($"move target='{_target}' position={_position} color={_color} relative={_relative} duration={_duration} log={_log}");
            return CommandStatus.Ok;
        }

        private static IEnumerable<string> SceneObjects()
        {
            var names = new List<string>();
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                {
                    names.AddRange(scene.GetRootGameObjects().Select(g => g.name));
                }
            }

            return names;
        }
    }

    [Command("load", "Loads a pretend scene.", typeof(DemoCommand))]
    public class DemoLoadCommand : SimpleCommand
    {
        [Suggest(nameof(Scenes))]
        [Positional(0, "The scene to load.")]
        private string _scene;

        [Switch('a', "Load additively.")]
        private bool _additive;

        protected override CommandStatus Execute(SimpleContext ctx)
        {
            ctx.Write($"load scene='{_scene}' additive={_additive}");
            return CommandStatus.Ok;
        }

        private static string[] Scenes()
        {
            return new[] { "Main Menu", "Level1", "Level2", "Boss Arena", "Credits" };
        }
    }

    [Command("tag", "Tags things with words.", typeof(DemoCommand))]
    public class DemoTagCommand : SimpleCommand
    {
        [Optional(0, "How many times to tag.")]
        private int _amount = 1;

        [Suggest(nameof(Words))]
        [Variadic("The words to tag with.")]
        private string[] _words;

        protected override CommandStatus Execute(SimpleContext ctx)
        {
            ctx.Write($"tag amount={_amount} words=[{string.Join(", ", (_words ?? Array.Empty<string>()).Select(w => $"'{w}'"))}]");
            return CommandStatus.Ok;
        }

        private static string[] Words()
        {
            return new[] { "plain", "two words", "a&&b", "-dash", "say \"hi\"", "semi;colon" };
        }
    }

    [Command("pick", "Picks one of many items.", typeof(DemoCommand))]
    public class DemoPickCommand : SimpleCommand
    {
        [Suggest(nameof(Items))]
        [Positional(0, "The item to pick.")]
        private string _item;

        protected override CommandStatus Execute(SimpleContext ctx)
        {
            ctx.Write($"pick item='{_item}'");
            return CommandStatus.Ok;
        }

        private static IEnumerable<string> Items()
        {
            return Enumerable.Range(0, 40).Select(i => $"item{i}").Append("ItemSpecial").Append("unity-item-log");
        }
    }

    [Command("slow", "Has a slow suggestion function.", typeof(DemoCommand))]
    public class DemoSlowCommand : SimpleCommand
    {
        private static int _calls;

        [Suggest(nameof(SlowValues))]
        [Positional(0, "A value from a slow source.")]
        private string _value;

        protected override CommandStatus Execute(SimpleContext ctx)
        {
            ctx.Write($"slow value='{_value}' (suggestion function ran {_calls} times so far)");
            return CommandStatus.Ok;
        }

        private static string[] SlowValues()
        {
            _calls++;
            Thread.Sleep(300);
            return new[] { "apple", "apricot", "banana", "blueberry", $"call-{_calls}" };
        }
    }

    [Command("broken", "Has a suggestion function that throws.", typeof(DemoCommand))]
    public class DemoBrokenCommand : SimpleCommand
    {
        [Suggest(nameof(Throws))]
        [Positional(0, "Anything.")]
        private string _thing;

        protected override CommandStatus Execute(SimpleContext ctx)
        {
            ctx.Write($"broken thing='{_thing}'");
            return CommandStatus.Ok;
        }

        private static string[] Throws()
        {
            throw new InvalidOperationException("The demo suggestion function failed on purpose");
        }
    }

    [Command("chain", "Second value depends on the first.", typeof(DemoCommand))]
    public class DemoChainCommand : SimpleCommand
    {
        [Suggest(nameof(Kinds))]
        [Positional(0, "The kind of thing.")]
        private string _kind;

        [Suggest(nameof(ThingsOfKind))]
        [Positional(1, "A thing of that kind.")]
        private string _thing;

        protected override CommandStatus Execute(SimpleContext ctx)
        {
            ctx.Write($"chain kind='{_kind}' thing='{_thing}'");
            return CommandStatus.Ok;
        }

        private static string[] Kinds()
        {
            return new[] { "fruit", "color", "animal" };
        }

        private static string[] ThingsOfKind(SuggestionContext ctx)
        {
            // Tokens so far are [root name, subcommand, kind, ...]
            var kind = ctx.AutocompleteContext.TokensSoFar.ElementAtOrDefault(2);

            return kind switch
            {
                "fruit"  => new[] { "apple", "banana", "cherry" },
                "color"  => new[] { "red", "green", "light blue" },
                "animal" => new[] { "cat", "dog", "red panda" },
                _        => Array.Empty<string>()
            };
        }
    }

    [Command("file", "Validates a file name.", typeof(DemoCommand))]
    public class DemoFileCommand : SimpleCommand
    {
        [EndsWith(".cs")]
        [Positional(0, "A C# file name.")]
        private string _path;

        protected override CommandStatus Execute(SimpleContext ctx)
        {
            ctx.Write($"file path='{_path}'");
            return CommandStatus.Ok;
        }
    }

    [Command("flags", "Bool switches, a value switch, and a number.", typeof(DemoCommand))]
    public class DemoFlagsCommand : SimpleCommand
    {
        [Switch('f', "Fast.")]
        private bool _fast;

        [Switch('q', "Quiet.")]
        private bool _quiet;

        [Switch('v', "Verbose.")]
        private bool _verbose;

        [Switch('n', "A count, which takes a value.")]
        private int _count;

        [Optional(0, "A number or any text.")]
        private string _value;

        protected override CommandStatus Execute(SimpleContext ctx)
        {
            ctx.Write($"flags fast={_fast} quiet={_quiet} verbose={_verbose} count={_count} value='{_value}'");
            return CommandStatus.Ok;
        }
    }
}
