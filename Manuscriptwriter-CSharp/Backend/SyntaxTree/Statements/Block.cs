using System.Collections.Generic;

namespace MSW.Scripting
{
    internal class Block : Statement
    {
        public readonly IEnumerable<Statement> statements;
        public Block(IEnumerable<Statement> statements)
        {
            this.statements = statements;
        }

        public override bool Accept(IMSWStatementVisitor visitor)
        {
            return visitor.VisitBlock(this);
        }
    }
}
