using System;
using Bossy.Utils;
using UnityEngine;

namespace Bossy.Settings
{
    [Serializable]
    public class BossyCliSettings
    {
        [Setting("The key used to enter responses when pressed together with ctrl.")]
        public readonly KeyCombination CancelCombination = new(KeyCode.C, KeyModifiers.Control);
        
        [Setting("The operator for running a command after another.")]
        public string ThenOperator = ";";
        
        [Setting("The operator for running a command after another if the first succeeds.")]
        public string AndOperator = "&&";
        
        [Setting("The operator for running a command after another if the first fails.")]
        public string OrOperator = "||";

        [Setting("The operator for piping the output from one command to the input of another.")]
        public string PipeOperator = "|";

        [Setting("The operator for running a command job in a window.")]
        public string WindowOperator = "!";
    }
}