namespace MSW.Scripting
{
    internal class Choice
    {
        public readonly Expression choice;
        public Statement consequence;

        public Choice(Expression choice, Statement consequence)
        {
            this.choice = choice;
            this.consequence = consequence;
        }
    }
}
