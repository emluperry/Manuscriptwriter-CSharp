namespace MSW.Scripting
{
    internal class Literal : Expression
    {
        public readonly object literal;
        public Literal(object literal)
        {
            this.literal = literal;
        }

        public override object Accept(IMSWExpressionVisitor visitor)
        {
            return visitor.VisitLiteral(this);
        }
    }
}
