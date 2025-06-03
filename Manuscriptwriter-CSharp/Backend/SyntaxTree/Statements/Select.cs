using System.Collections.Generic;

namespace MSW.Scripting
{
    internal class Select : Statement
    {
        public readonly Token choiceStart;
        public List<Choice> choices;

        public Select(Token choiceStart, List<Choice> choices)
        {
            this.choiceStart = choiceStart;
            this.choices = choices;
        }

        public override bool Accept(IMSWStatementVisitor visitor)
        {
            return visitor.VisitSelectStatement(this);
        }
    }
}
