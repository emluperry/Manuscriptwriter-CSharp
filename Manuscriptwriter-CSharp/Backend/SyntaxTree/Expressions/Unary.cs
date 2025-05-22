namespace MSW.Scripting
{
    internal class Unary : Expression
    {
        public readonly Token op;
        public readonly Expression right;
        public Unary(Token op, Expression right)
        {
            this.op = op;
            this.right = right;
        }

        public override object Accept(IMSWExpressionVisitor visitor)
        {
            return visitor.VisitUnary(this);
        }
    }
}
