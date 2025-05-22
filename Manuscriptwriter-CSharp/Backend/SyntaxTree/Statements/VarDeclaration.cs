namespace MSW.Scripting
{
    internal class VarDeclaration : Statement
    {
        public readonly Token token;
        public readonly Expression initialiser;

        public VarDeclaration(Token token, Expression initialiser)
        {
            this.token = token;
            this.initialiser = initialiser;
        }

        public override bool Accept(IMSWStatementVisitor visitor)
        {
            return visitor.VisitVar(this);
        }
    }
}