using System.Collections.Generic;
using System.Linq;
using Bossy.Utils;
using NUnit.Framework;
using UnityEngine;

namespace Bossy.Tests.FrontEnd.Autocomplete
{
    public class AutocompleteEngineTest
    {
        [Test]
        public void SandboxTest()
        {
            var input = "rep";
            var candi = "repeat";

            var ig = GetTrigrams(input);
            var cg = GetTrigrams(candi);
            
            Assert.That(ig.Count, Is.EqualTo(1));
            Assert.That(cg.Count, Is.EqualTo(4));
            
            ig.ToList().ForEach(Debug.Log);
            Log.Info("AND");
            cg.ToList().ForEach(Debug.Log);
            
            Assert.That(ig.Intersect(cg).Count, Is.EqualTo(1));
        }
        
        private HashSet<string> GetTrigrams(string word)
        {
            var lower = 0;
            var result = new HashSet<string>();
            
            while (lower < word.Length - 2)
            {
                var upper = Mathf.Min(lower + 3, word.Length);

                result.Add(word[lower..upper]);
                
                lower++;
            }

            return result;
        }
        
        
    }
}