using System;
using System.Collections.Generic;
using MSW.Events;

namespace MSW.Scripting
{
    internal class When : Statement
    {
        public readonly IRunnerEvent runnerEvent;
        public readonly Statement body;
        public readonly List<Expression> arguments;

        public When(IRunnerEvent runnerEvent, Statement body, List<Expression> arguments)
        {
            this.runnerEvent = runnerEvent;
            this.body = body;
            this.arguments = arguments;
        }

        public override bool Accept(IMSWStatementVisitor visitor)
        {
            return visitor.VisitWhenBlock(this);
        }
    }
}