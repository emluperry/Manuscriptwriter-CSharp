using System;

namespace MSW.Scripting
{
    internal class MSWRuntimeException : SystemException
    {
        public readonly Token operatorToken;
        public MSWRuntimeException(Token token, string message) : base(message)
        {
            this.operatorToken = token;
        }
    }
}
