namespace MSW.Scripting
{
    internal abstract class Expression
    {
        public abstract object Accept(IMSWExpressionVisitor visitor);
    }
}
