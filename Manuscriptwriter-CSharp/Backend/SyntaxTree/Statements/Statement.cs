namespace MSW.Scripting
{
    internal abstract class Statement
    {
        public abstract bool Accept(IMSWStatementVisitor visitor);
    }
}
