using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.Learning;
using Microsoft.ProgramSynthesis.Rules;
using Microsoft.ProgramSynthesis.Specifications;

namespace ProseTutorial
{
    public class WitnessFunctions : DomainLearningLogic
    {
        public WitnessFunctions(Grammar grammar) : base(grammar)
        {
        }

        // We will use this set of regular expressions in this tutorial 
        public static Regex[] UsefulRegexes =
        {
            new Regex(@"\w+"), // Word
            new Regex(@"\d+"), // Number
            new Regex(@"\s+"), // Space
            new Regex(@".+"), // Anything
            new Regex(@"$") // End of line
        };

        [WitnessFunction(nameof(Semantics.Concat), 2)]
        public DisjunctiveExamplesSpec WitnessConcatDelimiter(GrammarRule rule, ExampleSpec spec)
        {
            var result = new Dictionary<State, IEnumerable<object>>();

            foreach (KeyValuePair<State, object> example in spec.Examples)
            {
                State inputState = example.Key;
                var output = example.Value as string;
                var occurrences = new List<string>();

                for (int startIdx = 0; startIdx < output.Length; startIdx++)
                {
                    for (int endIdx = startIdx + 1; endIdx <= output.Length; endIdx++)
                    {
                        string delimiter = output.Substring(startIdx, endIdx - startIdx);
                        occurrences.Add(delimiter);
                    }
                }

                if (occurrences.Count == 0) return null;
                result[inputState] = occurrences.Cast<object>();
            }

            return DisjunctiveExamplesSpec.From(result);
        }

        [WitnessFunction(nameof(Semantics.Concat), 0, DependsOnParameters = new[] { 2 })]
        public DisjunctiveExamplesSpec WitnessConcatSubstring1(GrammarRule rule, ExampleSpec spec, ExampleSpec delmSpec)
        {
            var result = new Dictionary<State, IEnumerable<object>>();

            foreach (KeyValuePair<State, object> example in spec.Examples)
            {
                State inputState = example.Key;
                var output = example.Value as string;
                var delimiter = delmSpec.Examples[inputState] as string;
                int delmStartIdx = output.IndexOf(delimiter);
                int delmEndIdx = delmStartIdx + delimiter.Length;

                var occurrences = new List<string>();
                for (int startIdx = delmEndIdx; startIdx < output.Length; startIdx++)
                {
                    for (int endIdx = startIdx + 1; endIdx <= output.Length; endIdx++)
                    {
                        string subString1 = output.Substring(startIdx, endIdx - startIdx);
                        occurrences.Add(subString1);
                    }
                }

                if (occurrences.Count == 0) return null;
                result[inputState] = occurrences.Cast<object>();

            }
            return DisjunctiveExamplesSpec.From(result);
        }

        [WitnessFunction(nameof(Semantics.Concat), 1, DependsOnParameters = new[] { 2, 0 })]
        public DisjunctiveExamplesSpec WitnessConcatSubstring2(GrammarRule rule, ExampleSpec spec, ExampleSpec delmSpec, ExampleSpec sub1Spec)
        {
            var result = new Dictionary<State, IEnumerable<object>>();

            foreach (KeyValuePair<State, object> example in spec.Examples)
            {
                State inputState = example.Key;
                var output = example.Value as string;
                var delimiter = delmSpec.Examples[inputState] as string;
                var subString1 = sub1Spec.Examples[inputState] as string;
                int delmStartIdx = output.IndexOf(delimiter);
                int delmEndIdx = delmStartIdx + delimiter.Length;

                var occurrences = new List<string>();
                for (int startIdx = 0; startIdx < delmStartIdx; startIdx++)
                {
                    for (int endIdx = startIdx + 1; endIdx <= delmStartIdx; endIdx++)
                    {
                        string subString2 = output.Substring(startIdx, endIdx - startIdx);
                        //occurrences.Add(subString2);

                        string candidate = subString2 + delimiter + subString1;
                        if (candidate.Length == output.Length)
                        {
                            occurrences.Add(subString2);
                        }

                    }
                }

                if (occurrences.Count == 0) return null;
                result[inputState] = occurrences.Cast<object>();
            }
            return DisjunctiveExamplesSpec.From(result);
        }

        // ================================================================================================================== //

        [WitnessFunction(nameof(Semantics.Substring), 1)]
        public DisjunctiveExamplesSpec WitnessStartPosition(GrammarRule rule, ExampleSpec spec)
        {
            var result = new Dictionary<State, IEnumerable<object>>();

            foreach (KeyValuePair<State, object> example in spec.Examples)
            {
                State inputState = example.Key;
                var input = inputState[rule.Body[0]] as string;
                var output = example.Value as string;
                var occurrences = new List<int>();

                for (int i = input.IndexOf(output); i >= 0; i = input.IndexOf(output, i + 1)) occurrences.Add(i);

                if (occurrences.Count == 0) return null;
                result[inputState] = occurrences.Cast<object>();
            }
            return new DisjunctiveExamplesSpec(result);
        }

        [WitnessFunction(nameof(Semantics.Substring), 2, DependsOnParameters = new[] {1})]
        public ExampleSpec WitnessEndPosition(GrammarRule rule, ExampleSpec spec, ExampleSpec startSpec)
        {
            var result = new Dictionary<State, object>();
            foreach (KeyValuePair<State, object> example in spec.Examples)
            {
                State inputState = example.Key;
                var output = example.Value as string;
                var start = (int) startSpec.Examples[inputState];
                result[inputState] = start + output.Length;
            }
            return new ExampleSpec(result);
        }

        [WitnessFunction(nameof(Semantics.AbsPos), 1)]
        public DisjunctiveExamplesSpec WitnessK(GrammarRule rule, DisjunctiveExamplesSpec spec)
        {
            var kExamples = new Dictionary<State, IEnumerable<object>>();
            foreach (KeyValuePair<State, IEnumerable<object>> example in spec.DisjunctiveExamples)
            {
                State inputState = example.Key;
                var v = inputState[rule.Body[0]] as string;

                var positions = new List<int>();
                foreach (int pos in example.Value)
                {
                    positions.Add(pos + 1);
                    positions.Add(pos - v.Length - 1);
                }
                if (positions.Count == 0) return null;
                kExamples[inputState] = positions.Cast<object>();
            }
            return DisjunctiveExamplesSpec.From(kExamples);
        }

        [WitnessFunction(nameof(Semantics.RelPos), 1)]
        public DisjunctiveExamplesSpec WitnessRegexPair(GrammarRule rule, DisjunctiveExamplesSpec spec)
        {
            var result = new Dictionary<State, IEnumerable<object>>();
            foreach (KeyValuePair<State, IEnumerable<object>> example in spec.DisjunctiveExamples)
            {
                State inputState = example.Key;
                var input = inputState[rule.Body[0]] as string;

                var regexes = new List<Tuple<Regex, Regex>>();
                foreach (int output in example.Value)
                {
                    List<Regex>[] leftMatches, rightMatches;
                    BuildStringMatches(input, out leftMatches, out rightMatches);


                    List<Regex> leftRegex = leftMatches[output];
                    List<Regex> rightRegex = rightMatches[output];
                    if (leftRegex.Count == 0 || rightRegex.Count == 0)
                        return null;
                    regexes.AddRange(from l in leftRegex
                        from r in rightRegex
                        select Tuple.Create(l, r));
                }
                if (regexes.Count == 0) return null;
                result[inputState] = regexes;
            }
            return DisjunctiveExamplesSpec.From(result);
        }

        private static void BuildStringMatches(string inp, out List<Regex>[] leftMatches,
            out List<Regex>[] rightMatches)
        {
            leftMatches = new List<Regex>[inp.Length + 1];
            rightMatches = new List<Regex>[inp.Length + 1];
            for (var p = 0; p <= inp.Length; ++p)
            {
                leftMatches[p] = new List<Regex>();
                rightMatches[p] = new List<Regex>();
            }
            foreach (Regex r in UsefulRegexes)
            foreach (Match m in r.Matches(inp))
            {
                leftMatches[m.Index + m.Length].Add(r);
                rightMatches[m.Index].Add(r);
            }
        }
    }
}