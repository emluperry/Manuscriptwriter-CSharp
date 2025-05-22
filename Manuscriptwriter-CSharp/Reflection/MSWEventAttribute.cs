using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MSW.Reflection
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
    public class MSWEventAttribute : Attribute
    {
        private readonly string syntax;
        public MSWEventAttribute(string syntax)
        {
            this.syntax = syntax;
        }

        private Regex GetUnformattedTemplate()
        {
            var temp = Regex.Replace(syntax, @"[\\\^\$\.\|\?\*\+\(\)]", m => "\\" + m.Value);

            string pattern = "^" + Regex.Replace(temp, @"\{[0-9]+\}", "(.*?)") + "$";

            return new Regex(pattern);
        }

        public List<string> GetInputs(string input)
        {
            Regex r = this.GetUnformattedTemplate();
            Match m = r.Match(input);

            List<string> output = new List<string>();

            foreach (Group g in m.Groups)
            {
                if (g.Length >= input.Length)
                {
                    continue;
                }

                output.Add(g.Value);
            }

            return output;
        }

        public bool MatchesSyntax(string input)
        {
            Regex r = this.GetUnformattedTemplate();
            Match m = r.Match(input);

            return m.Success;
        }
    }
}