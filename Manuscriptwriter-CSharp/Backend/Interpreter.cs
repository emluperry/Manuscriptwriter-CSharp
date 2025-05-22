using System;
using System.Collections.Generic;
using System.Linq;
using MSW.Events;
using MSW.Reflection;

namespace MSW.Scripting
{
    internal class Interpreter : IMSWExpressionVisitor, IMSWStatementVisitor
    {
        // Context Settings
        private IRunnerEvent PauseEvent = null;

        internal bool IsFinished { get; private set; }
        internal Action OnFinish;

        // Error reporting
        public Action<MSWRuntimeException> ReportRuntimeError;

        private Stack<Environment> environmentStack;
        private Environment environment => this.environmentStack.Peek();

        private List<IRunnerEvent> activeEvents = new List<IRunnerEvent>();

        public Interpreter(Manuscript manuscript)
        {
            this.environmentStack = new Stack<Environment>();
            this.environmentStack.Push(new Environment(manuscript.statements));
        }

        ~Interpreter()
        {
            this.RunScriptCleanup();
        }

        public void RunScriptCleanup()
        {
            // iterate through events and clear invocation list
            foreach (IRunnerEvent runnerEvent in this.activeEvents)
            {
                runnerEvent.ClearAllEvents();
            }

            while (environmentStack.Any())
            {
                var env = environmentStack.Pop();
                env.Dispose();
            }
        }

        private void HandleEvent(object sender, IRunnerEventArgs eventArgs, When visitor)
        {
            // When receiving this event,
            // if the arguments are the same as the passed arguments,
            // then execute the body

            // Convert the expression arguments.
            var arguments = new object[visitor.arguments.Count];
            for (int i = 0; i < visitor.arguments.Count(); i++)
            {
                arguments[i] = this.Evaluate(visitor.arguments[i]);
            }

            if (!eventArgs.HasValidArguments(arguments))
            {
                return;
            }

            // get the event handler arguments
            for (int i = 0; i < visitor.arguments.Count(); i++)
            {
                if (!arguments[i].Equals(eventArgs.Args[i]))
                {
                    return;
                }
            }

            // if the event arguments match up, run the body.
            this.Execute(visitor.body);
            this.RunUntilBreak();
        }

        public void RunUntilBreak()
        {
            bool runNext = true;
            while (runNext)
            {
                runNext = this.InterpretNextLine();
            }
        }

        private bool InterpretNextLine()
        {
            if (this.PauseEvent != null)
            {
                return false;
            }

            try
            {
                if (this.environmentStack.Any())
                {
                    var currentEnv = this.environmentStack.Peek();
                    while (currentEnv.IsEmpty())
                    {
                        this.environmentStack.Pop();

                        if (!this.environmentStack.Any())
                        {
                            // Ensure the global environment is always present on the stack, as this allows global variables to be accessed.
                            this.environmentStack.Push(currentEnv);
                            this.IsFinished = true;
                            this.OnFinish?.Invoke();
                            return false;
                        }

                        currentEnv = this.environmentStack.Peek();
                    }

                    bool finishedExecution = this.Execute(currentEnv.Peek());
                    if (finishedExecution)
                    {
                        currentEnv.Dequeue();
                    }

                    return this.PauseEvent == null;
                }

                this.IsFinished = true;
                this.OnFinish?.Invoke();
                return false;
            }
            catch (MSWRuntimeException e)
            {
                ReportRuntimeError?.Invoke(e);
            }

            return false;
        }

        private void HandleContext(Context context)
        {
            if (context.pauseEvent != null)
            {
                this.PauseEvent = context.pauseEvent;
                this.PauseEvent.RegisterEvent(HandlePauseEvent);
            }
        }

        private void HandlePauseEvent(object sender, IRunnerEventArgs eventArgs)
        {
            this.PauseEvent?.UnregisterEvent(HandlePauseEvent);
            this.PauseEvent = null;

            this.RunUntilBreak();
        }

        private object Evaluate(Expression expr)
        {
            return expr.Accept(this);
        }

        private bool Execute(Statement statement)
        {
            return statement.Accept(this);
        }

        private void ExecuteBlock(Environment blockEnvironment)
        {
            this.environmentStack.Push(blockEnvironment);
        }

        private bool IsTrue(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj is bool b)
            {
                return b;
            }

            return true;
        }

        private bool IsEqual(object a, object b)
        {
            if (a == null && b == null)
            {
                return true;
            }

            if (a == null)
            {
                return false;
            }

            return a.Equals(b);
        }

        #region ERRORS + VALIDATION

        private void ValidateNumberOperand(Token op, object operand)
        {
            if (operand is double)
            {
                return;
            }

            throw new MSWRuntimeException(op, $"[ManuScriptwriter] Expected a number - instead found {(operand != null ? operand.ToString() : string.Empty)}. Numbers have to be numeric, i.e. 4, rather than four. Check your syntax.");
        }
        #endregion

        #region Expression Visitors
        public object VisitAssignment(Assign visitor)
        {
            object value = this.Evaluate(visitor.value);
            environment.Assign(visitor.token, value);
            return value;
        }

        public object VisitVariable(Variable visitor)
        {
            return environment.Get(visitor.token);
        }

        public object VisitLiteral(Literal expr)
        {
            return expr.literal;
        }

        public object VisitGrouping(Grouping expr)
        {
            return this.Evaluate(expr.expression);
        }

        public object VisitBinary(Binary visitor)
        {
            object left = this.Evaluate(visitor.left);
            object right = this.Evaluate(visitor.right);

            switch (visitor.op.type)
            {
                // ARITHMETIC
                case TokenType.MINUS:
                    this.ValidateNumberOperand(visitor.op, left);
                    this.ValidateNumberOperand(visitor.op, right);
                    return (double)left - (double)right;
                case TokenType.DIVIDE:
                    this.ValidateNumberOperand(visitor.op, left);
                    this.ValidateNumberOperand(visitor.op, right);
                    return (double)left / (double)right;
                case TokenType.MULTIPLY:
                    this.ValidateNumberOperand(visitor.op, left);
                    this.ValidateNumberOperand(visitor.op, right);
                    return (double)left * (double)right;
                case TokenType.PLUS:
                    if (left is double ld && right is double rd)
                    {
                        return ld + rd;
                    }

                    if (left is string ls && right is string rs)
                    {
                        return ls + rs;
                    }

                    throw new MSWRuntimeException(visitor.op, "[ManuScriptwriter] Tried to add two values of different types! Operands must be two numbers (i.e. 4, not four) or two strings.");

                // COMPARISON
                case TokenType.GREATER:
                    this.ValidateNumberOperand(visitor.op, left);
                    this.ValidateNumberOperand(visitor.op, right);
                    return (double)left > (double)right;
                case TokenType.GREATER_EQUAL:
                    this.ValidateNumberOperand(visitor.op, left);
                    this.ValidateNumberOperand(visitor.op, right);
                    return (double)left >= (double)right;
                case TokenType.LESS:
                    this.ValidateNumberOperand(visitor.op, left);
                    this.ValidateNumberOperand(visitor.op, right);
                    return (double)left < (double)right;
                case TokenType.LESS_EQUAL:
                    this.ValidateNumberOperand(visitor.op, left);
                    this.ValidateNumberOperand(visitor.op, right);
                    return (double)left <= (double)right;

                // EQUALITY
                case TokenType.EQUAL_EQUAL:
                    return this.IsEqual(left, right);
                case TokenType.NOT_EQUAL:
                    return !this.IsEqual(left, right);
            }

            return null;
        }

        public object VisitUnary(Unary visitor)
        {
            object right = this.Evaluate(visitor.right);

            switch (visitor.op.type)
            {
                case TokenType.NOT:
                    return !IsTrue(right);
                case TokenType.MINUS:
                    this.ValidateNumberOperand(visitor.op, right);
                    return -(double)right;
            }

            return null;
        }

        public object VisitLogical(Logical visitor)
        {
            object left = this.Evaluate(visitor.left);

            if (visitor.op.type == TokenType.OR)
            {
                if (this.IsTrue(left))
                {
                    return left;
                }
            }
            else
            {
                if (!this.IsTrue(left))
                {
                    return left;
                }
            }

            return this.Evaluate(visitor.right);
        }

        public object VisitCall(Call visitor)
        {
            object[] arguments = new object[visitor.arguments.Count + 1];

            Context ctx = new Context();
            arguments[0] = ctx;
            for (int i = 0; i < visitor.arguments.Count(); i++)
            {
                arguments[i + 1] = this.Evaluate(visitor.arguments[i]);
            }

            object output = visitor.function?.DynamicInvoke(visitor.target, arguments);

            // Handle Context
            this.HandleContext(ctx);

            return output;
        }

        #endregion

        #region STATEMENT VISITORS
        public bool VisitExpression(StatementExpression visitor)
        {
            this.Evaluate(visitor.expression);
            return true;
        }

        public bool VisitPrint(Print visitor)
        {
            object value = this.Evaluate(visitor.expression);
            Console.WriteLine(value != null ? value.ToString() : "Null");
            return true;
        }

        public bool VisitVar(VarDeclaration visitor)
        {
            object value = null;
            if (visitor.initialiser != null)
            {
                value = Evaluate(visitor.initialiser);
            }

            environment.Define(visitor.token.lexeme, value);
            return true;
        }

        public bool VisitBlock(Block visitor)
        {
            if (this.environmentStack.Any())
            {
                this.ExecuteBlock(new Environment(visitor.statements, this.environmentStack.Peek()));
            }
            else
            {
                this.ExecuteBlock(new Environment(visitor.statements));
            }

            return true;
        }

        public bool VisitIfBlock(If visitor)
        {
            if (this.IsTrue(this.Evaluate(visitor.condition)))
            {
                this.Execute(visitor.thenBranch);
            }
            else if (visitor.elseBranch != null)
            {
                this.Execute(visitor.elseBranch);
            }

            return true;
        }

        public bool VisitWhileBlock(While visitor)
        {
            if (this.IsTrue(this.Evaluate(visitor.condition)))
            {
                this.Execute(visitor.statement);
                return false;
            }

            return true;
        }

        public bool VisitWhenBlock(When visitor)
        {
            visitor.runnerEvent.RegisterEvent((s, e) => HandleEvent(s, e, visitor));

            if (!this.activeEvents.Contains(visitor.runnerEvent))
            {
                this.activeEvents.Add(visitor.runnerEvent);
            }

            return true;
        }

        #endregion
    }
}
