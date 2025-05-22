namespace MSW.Scripting
{
    internal class Binary : Expression
    {
        public readonly Expression left;
        public readonly Token op;
        public readonly Expression right;
        public Binary(Expression left, Token op, Expression right)
        {
            this.left = left;
            this.op = op;
            this.right = right;
        }

        public override object Accept(IMSWExpressionVisitor visitor)
        {
            return visitor.VisitBinary(this);
        }
    }
}
