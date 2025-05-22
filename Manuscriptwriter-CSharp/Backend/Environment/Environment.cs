using System.Collections.Generic;
using System.Linq;

namespace MSW.Scripting
{
    internal class Environment
    {
        private readonly Dictionary<string, object> variables = new Dictionary<string, object>();
        private readonly Environment enclosing;

        private Queue<Statement> statementQueue;

        public Statement Peek() => statementQueue?.Peek();
        public Statement Dequeue() => statementQueue?.Dequeue();

        public Environment(IEnumerable<Statement> statements)
        {
            enclosing = null;
            statementQueue = new Queue<Statement>(statements);
        }

        public Environment(IEnumerable<Statement> statements, Environment enclosing)
        {
            this.enclosing = enclosing;
            statementQueue = new Queue<Statement>(statements);
        }

        public bool IsEmpty()
        {
            return !this.statementQueue.Any();
        }

        public void Dispose()
        {
            this.statementQueue?.Clear();
            this.statementQueue = null;
        }

        #region VARIABLES
        public void Define(string name, object value)
        {
            variables[name] = value;
        }

        public object Get(Token token)
        {
            if (variables.ContainsKey(token.lexeme))
            {
                return variables[token.lexeme];
            }

            if (this.enclosing != null)
            {
                return enclosing.Get(token);
            }

            throw new MSWRuntimeException(token, $"[ManuScriptwriter] Tried to get a variable that hasn't been defined! See: {token.lexeme}.");
        }

        public void Assign(Token token, object value)
        {
            if (variables.ContainsKey(token.lexeme))
            {
                variables[token.lexeme] = value;
                return;
            }

            if (enclosing != null)
            {
                enclosing.Assign(token, value);
                return;
            }

            throw new MSWRuntimeException(token, $"[ManuScriptwriter] Tried to change a variable that hasn't been defined! See: {token.lexeme}.");
        }
        #endregion
    }
}
