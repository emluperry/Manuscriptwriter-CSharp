using System;
using System.Collections.Generic;

namespace MSW.Scripting
{
    internal class Call : Expression
    {
        public readonly Token callToken;
        public readonly Delegate function;
        public readonly object target;
        public readonly List<Expression> arguments;

        public Call(Token callToken, Delegate function, object target, List<Expression> arguments)
        {
            this.callToken = callToken;
            this.function = function;
            this.target = target;
            this.arguments = arguments;
        }

        public override object Accept(IMSWExpressionVisitor visitor)
        {
            return visitor.VisitCall(this);
        }
    }
}