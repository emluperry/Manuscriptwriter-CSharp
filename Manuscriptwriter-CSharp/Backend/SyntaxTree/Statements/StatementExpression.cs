namespace MSW.Scripting
{
    internal class StatementExpression : Statement
    {
        public readonly Expression expression;
        public StatementExpression(Expression expression)
        {
            this.expression = expression;
        }

        public override bool Accept(IMSWStatementVisitor visitor)
        {
            return visitor.VisitExpression(this);
        }
    }
}
