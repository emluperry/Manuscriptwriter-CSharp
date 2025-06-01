using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MSW.Events;
using MSW.Reflection;
using MSW.Scripting;

namespace MSW.Compiler
{
    internal class Parser
    {
        private struct CustomFunction
        {
            public MSWFunctionAttribute attribute;
            public MethodInfo method;
            public object instance;

            public CustomFunction(MSWFunctionAttribute attribute, MethodInfo method, object instance)
            {
                this.attribute = attribute;
                this.method = method;
                this.instance = instance;
            }
        }

        private struct CustomEvent
        {
            public MSWEventAttribute attribute;
            public IRunnerEvent runnerEvent;

            public CustomEvent(MSWEventAttribute attribute, IRunnerEvent runnerEvent)
            {
                this.attribute = attribute;
                this.runnerEvent = runnerEvent;
            }
        }

        public Action<Token, string> ReportTokenError;

        private List<Token> tokens;
        private readonly List<CustomFunction> functions;
        private readonly List<CustomEvent> events;

        private int currentIndex = 0;

        public Parser(List<Token> tokens, IEnumerable<object> functionLibrary = null)
        {
            this.tokens = tokens;
            this.functions = new List<CustomFunction>();
            this.events = new List<CustomEvent>();

            if (functionLibrary != null)
            {
                this.SetupLibrary(functionLibrary);
            }
        }

        #region LIBRARIES
        private void SetupLibrary(IEnumerable<object> functionLibrary)
        {
            foreach (var lib in functionLibrary)
            {
                var libraryType = lib.GetType();
                var usableMethods = libraryType.GetMethods()
                    .Where(method => method.GetCustomAttributes<MSWFunctionAttribute>().Any());

                foreach (MethodInfo method in usableMethods)
                {
                    var attributes = method.GetCustomAttributes<MSWFunctionAttribute>();
                    foreach (var attr in attributes)
                    {
                        this.functions.Add(new CustomFunction(attr, method, lib));
                    }
                }

                var usableEvents = libraryType.GetFields()
                    .Where(method => method.GetCustomAttributes<MSWEventAttribute>().Any());

                foreach (FieldInfo method in usableEvents)
                {
                    var attributes = method.GetCustomAttributes<MSWEventAttribute>();
                    var field = method.GetValue(lib);

                    if (!(field is IRunnerEvent runnerEvent))
                    {
                        continue;
                    }

                    foreach (var attr in attributes)
                    {
                        this.events.Add(new CustomEvent(attr, runnerEvent));
                    }
                }
            }
        }

        private Func<object, object[], object> GetFunctionFromLine(string line, out List<string> inputs, out object target)
        {
            foreach (var function in this.functions)
            {
                if (!function.attribute.MatchesSyntax(line))
                {
                    continue;
                }

                inputs = function.attribute.GetInputs(line);
                target = function.instance;
                return function.method.Invoke;
            }

            target = null;
            inputs = null;
            return null;
        }

        private IRunnerEvent GetEventFromLine(string line, out List<string> inputs)
        {
            foreach (var runnerEvent in this.events)
            {
                if (!runnerEvent.attribute.MatchesSyntax(line))
                {
                    continue;
                }

                inputs = runnerEvent.attribute.GetInputs(line);
                return runnerEvent.runnerEvent;
            }

            inputs = null;
            return null;
        }
        #endregion

        public IEnumerable<Statement> Parse()
        {
            List<Statement> statements = new List<Statement>();
            while (!this.IsEndOfQueue())
            {
                if (this.CurrentMatchesType(TokenType.EOF))
                {
                    break;
                }

                if (this.TryConsumeToken(TokenType.EOL, out Token eol))
                {
                    continue;
                }

                Statement s = this.Declaration();

                if (s != null)
                {
                    statements.Add(s);
                }
            }

            return statements;
        }

        private Token TryDequeueToken()
        {
            if (this.IsEndOfQueue())
            {
                return null;
            }

            var token = this.tokens[this.currentIndex];
            currentIndex++;
            return token;
        }

        private Token TryPeekToken()
        {
            if (this.IsEndOfQueue())
            {
                return null;
            }

            return this.tokens[this.currentIndex];
        }

        private bool CurrentMatchesOneOfTypes(IEnumerable<TokenType> tokenTypes)
        {
            foreach (TokenType tokenType in tokenTypes)
            {
                if (this.CurrentMatchesType(tokenType))
                {
                    return true;
                }
            }

            return false;
        }

        private bool CurrentMatchesType(TokenType tokenType)
        {
            return TryPeekToken().type == tokenType;
        }

        private bool TryConsumeToken(TokenType type, out Token token)
        {
            if (this.CurrentMatchesType(type))
            {
                token = this.tokens[this.currentIndex];
                currentIndex++;
                return true;
            }

            token = null;
            return false;
        }

        private bool TryConsumeOneOfTokens(IEnumerable<TokenType> tokenTypes, out Token token)
        {
            if (this.CurrentMatchesOneOfTypes(tokenTypes))
            {
                token = this.TryDequeueToken();
                return true;
            }

            token = null;
            return false;
        }

        private Token ConsumeToken(TokenType type, string message)
        {
            if (this.CurrentMatchesType(type))
            {
                return this.TryDequeueToken();
            }

            throw this.ParseError(this.TryPeekToken(), message);
        }

        private Token ConsumeOneOfTokens(List<TokenType> types, string message)
        {
            foreach (TokenType tokenType in types)
            {
                if (this.CurrentMatchesType(tokenType))
                {
                    return this.TryDequeueToken();
                }
            }

            throw this.ParseError(this.TryPeekToken(), message);
        }

        private bool IsEndOfQueue()
        {
            return this.currentIndex >= this.tokens.Count;
        }

        #region ERROR HANDLING
        private MSWParseException ParseError(Token token, string message)
        {
            ReportTokenError?.Invoke(token, message);

            return new MSWParseException();
        }

        private void Synchronise()
        {
            this.TryDequeueToken();

            while (!this.IsEndOfQueue())
            {
                switch (this.TryPeekToken().type)
                {
                    case TokenType.EOL:
                        this.TryDequeueToken();
                        return;
                    case TokenType.EOF:
                        return;
                }

                this.TryDequeueToken();
            }
        }
        #endregion

        #region STATEMENTS

        private Statement Declaration()
        {
            try
            {
                if (this.TryConsumeToken(TokenType.PASSAGE, out Token passageToken))
                {
                    return this.PassageStatement();
                }
                if (this.TryConsumeToken(TokenType.VAR, out Token var))
                {
                    return this.VarDeclaration();
                }

                return this.Statement();
            }
            catch (MSWParseException)
            {
                Synchronise();
                return null;
            }
        }

        private Statement VarDeclaration()
        {
            Token token = this.ConsumeToken(TokenType.IDENTIFIER, "[ManuScriptwriter] After the 'var' keyword, a name for the new variable was expected.");

            Expression initialiser = null;
            if (this.TryConsumeToken(TokenType.EQUAL, out Token equals))
            {
                initialiser = this.Expression();
            }

            this.ConsumeOneOfTokens(new List<TokenType> { TokenType.COMMA, TokenType.EOL, TokenType.EOF }, "[ManuScriptwriter] After creating a new variable, move to a new line.");
            return new VarDeclaration(token, initialiser);
        }

        private Statement Statement()
        {
            if (this.TryConsumeToken(TokenType.WHEN, out Token whenToken))
            {
                return this.WhenStatement();
            }

            if (this.TryConsumeToken(TokenType.IF, out Token ifToken))
            {
                return this.IfStatement();
            }

            if (this.TryConsumeToken(TokenType.WHILE, out Token whileToken))
            {
                return this.WhileStatement();
            }

            if (this.TryConsumeToken(TokenType.FOR, out Token forToken))
            {
                return this.ForStatement();
            }

            if (this.TryConsumeToken(TokenType.PRINT, out Token printToken))
            {
                return this.PrintStatement();
            }

            return this.ExpressionStatement();
        }

        private Statement ExpressionStatement()
        {
            Expression value = this.Expression();
            this.ConsumeOneOfTokens(new List<TokenType> { TokenType.COMMA, TokenType.EOL, TokenType.EOF }, "[ManuScriptwriter] Expected the line to end - make sure each sentence is on its own line.");
            return new StatementExpression(value);
        }

        private Statement PassageStatement()
        {
            if (!this.TryConsumeToken(TokenType.IDENTIFIER, out Token identifierToken))
            {
                throw this.ParseError(identifierToken, "[ManuScriptwriter] Passages must have some form of identifier/name!");
            }

            var body = new Block(this.Block());

            return new Passage(identifierToken, body);
        }

        private Statement WhenStatement()
        {
            if (!this.TryConsumeToken(TokenType.EVENT, out Token eventToken))
            {
                throw this.ParseError(eventToken, "[ManuScriptwriter] Then 'When' keyword must be followed by an event! Check your spelling, and the available events.");
            }

            IRunnerEvent runnerEvent = this.GetEventFromLine(eventToken.lexeme, out List<string> arguments);

            if (runnerEvent == null)
            {
                throw this.ParseError(eventToken, "[ManuScriptwriter] Could not find that event! Check your spelling, and the available events.");
            }

            List<Expression> args = new List<Expression>();
            // convert inputs into expressions
            foreach (var arg in arguments)
            {
                args.Add(this.ConvertStringArgumentToToken(arg));
            }

            var body = new Block(this.Block());

            return new When(runnerEvent, body, args);
        }

        private Statement IfStatement()
        {
            Expression condition = this.Expression();

            this.ConsumeToken(TokenType.COMMA, "[ManuScriptwriter] Expected a comma after the 'if' condition, in the format [If condition,].");

            Statement thenBranch = this.Statement();
            Statement elseBranch = null;
            if (this.TryConsumeToken(TokenType.ELSE, out Token elseToken))
            {
                this.TryConsumeToken(TokenType.COMMA, out Token commaToken); // An additional comma after an else is not necessary, but may be used as a style choice.

                elseBranch = this.Statement();
            }

            return new If(condition, thenBranch, elseBranch);
        }

        private Statement WhileStatement()
        {
            Expression condition = this.Expression();
            this.ConsumeToken(TokenType.COMMA, "[ManuScriptwriter] Expected a comma after the 'while' condition, in the format [While condition,].");
            Statement statement = this.Statement();

            return new While(condition, statement);
        }

        private Statement ForStatement()
        {
            Statement initialiser = null;
            if (this.TryConsumeToken(TokenType.VAR, out Token var))
            {
                initialiser = this.VarDeclaration();
            }
            else
            {
                this.TryDequeueToken();
                initialiser = this.ExpressionStatement();
            }

            Expression condition = this.Expression();
            this.ConsumeToken(TokenType.COMMA, "[ManuScriptwriter] Expected a comma after the 'for' condition, in the format [For variable, condition, increment,].");

            Expression increment = this.Expression();
            this.ConsumeToken(TokenType.COMMA, "[ManuScriptwriter] Expected a comma after the 'for' increment, in the format [For variable, condition, increment,].");

            Statement body = this.Statement();

            if (increment != null)
            {
                body = new Block(new List<Statement>() { body, new StatementExpression(increment) });
            }

            if(condition == null)
            {
                condition = new Literal(true);
            }

            body = new While(condition, body);

            if (initialiser != null)
            {
                body = new Block(new List<Statement>() { initialiser, body });
            }

            return body;
        }

        private Statement PrintStatement()
        {
            Expression value = this.Expression();
            this.ConsumeOneOfTokens(new List<TokenType> { TokenType.EOL, TokenType.EOF }, "[ManuScriptwriter] Expected the line to end - make sure each sentence is on its own line.");
            return new Print(value);
        }

        private List<Statement> Block()
        {
            List<Statement> statements = new List<Statement>();

            while (!this.IsEndOfQueue() && !this.CurrentMatchesOneOfTypes(new List<TokenType> { TokenType.EOF, TokenType.WHEN, TokenType.PASSAGE }))
            {
                if (this.TryConsumeToken(TokenType.EOL, out Token eolToken))
                {
                    continue;
                }

                statements.Add(this.Declaration());
            }
            return statements;
        }

        #endregion

        #region EXPRESSIONS
        private Expression Expression()
        {
            return this.Assignment();
        }

        private Expression Assignment()
        {
            Expression expression = this.Or();

            if (this.TryConsumeToken(TokenType.EQUAL, out Token equals))
            {
                Expression value = this.Assignment();

                if (expression is Variable var)
                {
                    Token token = var.token;
                    return new Assign(token, value);
                }

                this.ParseError(equals, "[ManuScriptwriter] Tried to assign a value to an invalid variable. Check the variable exists before setting one.");
            }

            return expression;
        }

        private Expression Or()
        {
            Expression expression = this.And();

            while (this.TryConsumeToken(TokenType.OR, out Token op))
            {
                Expression right = this.And();
                expression = new Logical(expression, op, right);
            }

            return expression;
        }

        private Expression And()
        {
            Expression expression = this.Equality();

            while (this.TryConsumeToken(TokenType.AND, out Token op))
            {
                Expression right = this.Equality();
                expression = new Logical(expression, op, right);
            }

            return expression;
        }

        private Expression Equality()
        {
            Expression expression = this.Comparison();

            while (this.TryConsumeOneOfTokens(new List<TokenType> { TokenType.NOT_EQUAL, TokenType.EQUAL_EQUAL }, out Token op))
            {
                Expression right = this.Comparison();
                expression = new Binary(expression, op, right);
            }

            return expression;
        }

        private Expression Comparison()
        {
            Expression expression = this.Term();

            while (this.TryConsumeOneOfTokens(new List<TokenType> { TokenType.GREATER, TokenType.GREATER_EQUAL, TokenType.LESS, TokenType.LESS_EQUAL }, out Token op))
            {
                Expression right = this.Term();
                expression = new Binary(expression, op, right);
            }

            return expression;
        }

        private Expression Term()
        {
            Expression expression = this.Factor();

            while (this.TryConsumeOneOfTokens(new List<TokenType> { TokenType.MINUS, TokenType.PLUS }, out Token op))
            {
                Expression right = this.Factor();
                expression = new Binary(expression, op, right);
            }

            return expression;
        }

        private Expression Factor()
        {
            Expression expression = this.Unary();

            while (this.TryConsumeOneOfTokens(new List<TokenType> { TokenType.MULTIPLY, TokenType.DIVIDE }, out Token op))
            {
                Expression right = this.Unary();
                expression = new Binary(expression, op, right);
            }

            return expression;
        }

        private Expression Unary()
        {
            if (this.TryConsumeOneOfTokens(new List<TokenType> { TokenType.NOT, TokenType.MINUS }, out Token op))
            {
                Expression right = this.Unary();
                return new Unary(op, right);
            }

            return this.Call();
        }

        private Expression Call()
        {
            if (this.TryConsumeToken(TokenType.FUNCTION, out Token function))
            {
                Delegate func = this.GetFunctionFromLine(function.lexeme, out List<string> arguments, out object target);

                if (func == null)
                {
                    throw this.ParseError(function, "[ManuScriptwriter] Found an invalid sentence! Check the spelling and the available statements.");
                }

                List<Expression> args = new List<Expression>();
                // convert inputs into expressions
                foreach (var arg in arguments)
                {
                    args.Add(this.ConvertStringArgumentToToken(arg));
                }

                // return new call with expressions
                return new Call(function, func, target, args);
            }

            if(this.TryConsumeToken(TokenType.GOTO, out Token Goto))
            {
                Expression callee = this.Primary();

                return new Goto(Goto, callee);
            }

            return this.Primary();
        }

        private Expression Primary()
        {
            if (this.TryConsumeToken(TokenType.FALSE, out Token opf))
            {
                return new Literal(false);
            }
            if (this.TryConsumeToken(TokenType.TRUE, out Token opt))
            {
                return new Literal(true);
            }
            if (this.TryConsumeToken(TokenType.NULL, out Token opn))
            {
                return new Literal(null);
            }

            if (this.TryConsumeOneOfTokens(new List<TokenType> { TokenType.STRING, TokenType.DOUBLE }, out Token opl))
            {
                return new Literal(opl.literal);
            }

            if (this.TryConsumeToken(TokenType.IDENTIFIER, out Token opv))
            {
                return new Variable(opv);
            }

            throw ParseError(this.TryPeekToken(), "[ManuScriptwriter] [Internal Error] Expected a full sentence, but couldn't find any valid options. Check your spelling and syntax.");
        }

        #endregion

        #region ARGUMENT HANDLING

        private Expression ConvertStringArgumentToToken(string argument)
        {
            return new Literal(argument);
        }

        #endregion
    }
}
