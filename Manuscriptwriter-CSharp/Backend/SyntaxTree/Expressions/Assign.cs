namespace MSW.Scripting
{
    internal class Assign : Expression
    {
        public readonly Token token;
        public readonly Expression value;

        public Assign(Token token, Expression value)
        {
            this.token = token;
            this.value = value;
        }

        public override object Accept(IMSWExpressionVisitor visitor)
        {
            return visitor.VisitAssignment(this);
        }
    }
}
