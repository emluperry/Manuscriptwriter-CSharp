namespace MSW.Scripting
{
    internal class While : Statement
    {
        public readonly Expression condition;
        public readonly Statement statement;

        public While(Expression condition, Statement statement)
        {
            this.condition = condition;
            this.statement = statement;
        }

        public override bool Accept(IMSWStatementVisitor visitor)
        {
            return visitor.VisitWhileBlock(this);
        }
    }
}
