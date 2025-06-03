using System;
using System.Collections.Generic;
using MSW.Input;
using MSW.Scripting;

namespace MSW.Compiler
{
    public class Runner
    {
        public Action<string> Logger;
        public Action OnFinish;

        private Interpreter interpreter;
        public Runner(Manuscript manuscript, IChoiceHandler inputHandler)
        {
            interpreter = new Interpreter(manuscript) { ReportRuntimeError = ReportRuntimeError, OnFinish = RunOnFinish, ChoiceHandler = inputHandler };
        }

        public bool IsFinished()
        {
            return interpreter.IsFinished;
        }

        public void Run()
        {
            interpreter.RunUntilBreak();
        }

        private void RunOnFinish()
        {
            this.OnFinish?.Invoke();
        }

        public void RunCleanup()
        {
            this.interpreter.RunScriptCleanup();
        }

        private void ReportRuntimeError(MSWRuntimeException ex)
        {
            this.Report(ex.operatorToken.line, ex.operatorToken.lexeme, ex.Message);
        }

        private void Report(int line, string where, string message)
        {
            Logger?.Invoke($"[ManuScriptwriter] ERROR: [Line {line} - {where}]: {message}");
        }
    }
}
