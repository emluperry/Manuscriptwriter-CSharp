using System.Collections.Generic;

namespace MSW.Scripting
{
    public class Manuscript
    {
        internal IEnumerable<Statement> statements;

        internal Manuscript(IEnumerable<Statement> statements)
        {
            this.statements = statements;
        }
    }
}