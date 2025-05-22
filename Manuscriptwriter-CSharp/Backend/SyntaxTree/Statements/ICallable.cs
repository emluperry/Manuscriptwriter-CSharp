using System.Collections.Generic;

namespace MSW.Scripting
{
    internal interface ICallable
    {
        int Arity();
        object Call(Interpreter interpreter, List<object> arguments);
    }
}