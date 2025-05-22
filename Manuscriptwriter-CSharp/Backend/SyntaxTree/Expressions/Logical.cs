namespace MSW.Scripting
{
    internal class Logical : Expression
    {
        public readonly Expression left;
        public readonly Token op;
        public readonly Expression right;

        public Logical(Expression left, Token op, Expression right)
        {
            this.left = left;
            this.op = op;
            this.right = right;
        }

        public override object Accept(IMSWExpressionVisitor visitor)
        {
            return visitor.VisitLogical(this);
        }
    }
}
