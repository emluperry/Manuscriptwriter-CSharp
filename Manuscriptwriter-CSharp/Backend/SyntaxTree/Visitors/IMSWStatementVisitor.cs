namespace MSW.Scripting
{
    internal interface IMSWStatementVisitor
    {
        bool VisitExpression(StatementExpression visitor);
        bool VisitPrint(Print visitor);

        bool VisitVar(VarDeclaration visitor);

        bool VisitBlock(Block visitor);

        bool VisitIfBlock(If visitor);

        bool VisitWhileBlock(While visitor);

        bool VisitWhenBlock(When visitor);
    }
}
