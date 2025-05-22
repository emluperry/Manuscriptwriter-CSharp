namespace MSW.Scripting
{
    internal class Variable : Expression
    {
        public readonly Token token;
        public Variable(Token token)
        {
            this.token = token;
        }

        public override object Accept(IMSWExpressionVisitor visitor)
        {
            return visitor.VisitVariable(this);
        }
    }
}
