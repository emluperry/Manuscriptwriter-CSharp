namespace MSW.Scripting
{
    internal class Grouping : Expression
    {
        public readonly Expression expression;
        public Grouping(Expression exp)
        {
            this.expression = exp;
        }

        public override object Accept(IMSWExpressionVisitor visitor)
        {
            return visitor.VisitGrouping(this);
        }
    }
}
