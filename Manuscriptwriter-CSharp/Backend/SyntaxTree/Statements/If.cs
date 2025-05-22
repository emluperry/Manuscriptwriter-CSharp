namespace MSW.Scripting
{
    internal class If : Statement
    {
        public readonly Expression condition;
        public readonly Statement thenBranch;
        public readonly Statement elseBranch;

        public If(Expression condition, Statement thenBranch, Statement elseBranch = null)
        {
            this.condition = condition;
            this.thenBranch = thenBranch;
            this.elseBranch = elseBranch;
        }

        public override bool Accept(IMSWStatementVisitor visitor)
        {
            return visitor.VisitIfBlock(this);
        }
    }
}
