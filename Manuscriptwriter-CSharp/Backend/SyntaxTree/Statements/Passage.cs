namespace MSW.Scripting
{
    internal class Passage : Statement
    {
        public readonly Token id;
        public readonly Statement body;

        public Passage(Token id, Statement body)
        {
            this.id = id;
            this.body = body;
        }

        public override bool Accept(IMSWStatementVisitor visitor)
        {
            return visitor.VisitPassageBlock(this);
        }
    }
}
