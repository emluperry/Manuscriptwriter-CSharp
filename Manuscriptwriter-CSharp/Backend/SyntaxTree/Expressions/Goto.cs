namespace MSW.Scripting
{
    internal class Goto : Expression
    {
        public readonly Token token;
        public readonly Expression passageID;

        public Goto(Token token, Expression passageID)
        {
            this.token = token;
            this.passageID = passageID;
        }

        public override object Accept(IMSWExpressionVisitor visitor)
        {
            return visitor.VisitGoto(this);
        }
    }
}