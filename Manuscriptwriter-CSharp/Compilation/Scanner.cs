using System;
using System.Collections.Generic;
using MSW.Scripting;

namespace MSW.Compiler
{
    internal class Scanner
    {
        private readonly string source;
        private readonly string[] lines;
        private int startIndex;
        private int endIndex;
        private int currentIndex;

        private string currentLine;
        private readonly int lastLine;
        private int line;

        private Dictionary<string, Token> identifiers = new Dictionary<string, Token>();

        internal Scanner(string source)
        {
            this.source = source;
            lines = source.Split(new string[] { "\n" }, StringSplitOptions.None);

            startIndex = 0;
            currentIndex = 0;

            lastLine = lines.Length;
            line = 0;
        }

        internal List<Token> ScanLines()
        {
            List<Token> tokens = new List<Token>();
            while (!this.IsFinished())
            {
                tokens.AddRange(this.ScanLine());
                tokens.Add(new Token(TokenType.EOL, null, null, this.line + 1));
                line++;
            }

            tokens.Add(new Token(TokenType.EOF, null, null, this.line + 1));
            return tokens;
        }

        private List<Token> ScanLine()
        {
            List<Token> tokens = new List<Token>();
            this.startIndex = 0;
            this.currentIndex = 0;

            this.currentLine = this.lines[this.line];
            this.endIndex = this.currentLine.Length;

            this.SkipWhitespace();
            if (this.EndOfLine())
            {
                return tokens;
            }

            // Check first token.
            Token previousToken = ErrorToken("[Internal Error] The previous token was accessed before being initialised.");
            Token currentToken = this.ScanToken(previousToken);

            switch (currentToken.type)
            {
                case TokenType.UNIDENTIFIED: // Get the full line as a function and return.
                    currentIndex = 0;
                    tokens.Add(this.ConvertLineToToken(TokenType.FUNCTION));
                    return tokens;

                case TokenType.WHEN: // Get the remainder of the line as a token and return.
                    tokens.Add(currentToken);

                    this.SkipWhitespace();
                    if (this.EndOfLine())
                    {
                        break;
                    }
                    startIndex = currentIndex;
                    tokens.Add(this.ConvertLineToToken(TokenType.EVENT));
                    return tokens;

                case TokenType.IDENTIFIER: // check for = token. If it doesn't exist, this isn't an assignment, so no point keeping it standalone.
                    previousToken = currentToken;
                    this.SkipWhitespace();
                    currentToken = this.ScanToken(previousToken);

                    if (currentToken.type != TokenType.EQUAL)
                    {
                        // Get the full line as a token and return.
                        currentIndex = 0;
                        tokens.Add(this.ConvertLineToToken(TokenType.FUNCTION));
                        return tokens;
                    }

                    tokens.Add(previousToken);
                    break;
            }

            tokens.Add(currentToken);
            while (!this.EndOfLine())
            {
                var tempToken = this.ScanNextToken(ref previousToken, currentToken);

                if (tempToken == null)
                {
                    continue;
                }

                currentToken = tempToken;
                tokens.Add(currentToken);
            }

            return tokens;
        }

        private Token ScanNextToken(ref Token previousToken, Token currentToken)
        {
            this.SkipWhitespace();
            if (this.EndOfLine())
            {
                return null;
            }

            previousToken = currentToken;
            startIndex = currentIndex;
            var nextToken = this.ScanToken(previousToken);

            if (nextToken.type == TokenType.IDENTIFIER)
            {
                this.identifiers[nextToken.lexeme] = nextToken;
            }

            return nextToken;
        }

        #region SCANNING

        private void SkipWhitespace()
        {
            while (!this.EndOfLine())
            {
                char c = this.PeekCharacter();
                switch (c)
                {
                    case ' ':
                    case '\t':
                        this.PopCharacter();
                        break;
                    case '\r':
                    case '\n':
                        this.PopCharacter();
                        return;
                    case '#':
                        while (!this.EndOfLine() && this.PeekCharacter() != '\n')
                        {
                            this.PopCharacter();
                        }

                        return;
                    default:
                        return;
                }
            }
        }

        private Token ScanToken(Token previousToken)
        {
            if (this.IsFinished())
            {
                return this.MakeToken(TokenType.EOF);
            }

            char c = this.PopCharacter();

            // Check for non-char values.
            if (this.IsAlpha(c)) return this.GetIdentifierToken(c, previousToken);
            if (this.IsDigit(c)) return this.GetNumberToken();

            switch (c)
            {
                case ',': return this.MakeToken(TokenType.COMMA);
                case ':': return this.MakeToken(TokenType.COLON);

                case '-': return this.MakeToken(TokenType.MINUS);
                case '+': return this.MakeToken(TokenType.PLUS);
                case '*': return this.MakeToken(TokenType.MULTIPLY);
                case '/': return this.MakeToken(TokenType.DIVIDE);

                case '!': return this.MakeToken(this.PopOnMatch('=') ? TokenType.NOT_EQUAL : TokenType.NOT);
                case '=': return this.MakeToken(this.PopOnMatch('=') ? TokenType.EQUAL_EQUAL : TokenType.EQUAL);
                case '<': return this.MakeToken(this.PopOnMatch('=') ? TokenType.LESS_EQUAL : TokenType.LESS);
                case '>': return this.MakeToken(this.PopOnMatch('=') ? TokenType.GREATER_EQUAL : TokenType.GREATER);

                case '"': return this.GetStringToken();
            }

            return this.ErrorToken("[ManuScriptwriter] Found an unexpected character! Did you mis-type something?");
        }

        private Token ConvertLineToToken(TokenType type)
        {
            while (!this.EndOfLine())
            {
                char nextCharacter = this.PeekCharacter();
                if (nextCharacter == '\n' || nextCharacter == '\r' || nextCharacter == '\t' || nextCharacter == '#')
                {
                    break;
                }

                PopCharacter();
            }

            // trim trailing spaces
            while (this.PeekPreviousCharacter() == ' ')
            {
                currentIndex--;
            }

            return new Token(type, currentLine.Substring(startIndex, currentIndex - startIndex), null, line + 1);
        }

        private Token MakeToken(TokenType type, object literal = null)
        {
            return new Token(type, this.currentLine.Substring(startIndex, currentIndex - startIndex), literal, line + 1);
        }

        private Token ErrorToken(string message)
        {
            return new Token(TokenType.ERROR, message, null, line + 1);
        }

        #endregion

        #region UTILITY

        internal bool IsFinished()
        {
            return line >= lastLine;
        }

        internal bool EndOfLine()
        {
            return currentIndex >= this.endIndex;
        }

        #endregion

        #region TYPE UTILITY

        private Token GetIdentifierToken(char c, Token previousToken)
        {
            string identifier = $"{c}";
            while (!this.EndOfLine() && (this.IsAlpha(this.PeekCharacter()) || this.IsDigit(this.PeekCharacter())))
            {
                identifier += this.PopCharacter();
            }

            if (identifier == string.Empty)
            {
                return this.MakeToken(TokenType.UNIDENTIFIED);
            }

            return this.MakeToken(this.GetIdentifierType(identifier, previousToken));
        }

        private TokenType GetIdentifierType(string identifier, Token previousToken)
        {
            identifier = identifier.ToLowerInvariant();
            int length = identifier.Length;
            switch (identifier[0])
            {
                case 'a': return CheckKeyword(1, 2, "nd", TokenType.AND);
                case 'e': return CheckKeyword(1, 3, "lse", TokenType.ELSE);
                case 'f':
                    if (length <= 1)
                        break;
                    switch (identifier[1])
                    {
                        case 'a': return this.CheckKeyword(2, 3, "lse", TokenType.FALSE);
                        case 'o': return this.CheckKeyword(2, 1, "r", TokenType.FOR);
                    }
                    break;
                case 'g': return CheckKeyword(1, 4, "iven", TokenType.GIVEN);
                case 'i':
                    if (length <= 1)
                        break;
                    switch (identifier[1])
                    {
                        case 'f': return CheckKeyword(2, 0, "", TokenType.IF);
                        case 's': return CheckKeyword(2, 0, "", TokenType.EQUAL_EQUAL);
                    }

                    break;
                case 'n':
                    if (length <= 1)
                        break;
                    switch (identifier[1])
                    {
                        case 'o': return CheckKeyword(2, 1, "t", TokenType.NOT);
                        case 'u': return CheckKeyword(2, 2, "ll", TokenType.NULL);
                    }
                    break;
                case 'o':
                    if (length <= 1)
                        break;
                    switch (identifier[1])
                    {
                        case 'r': return CheckKeyword(2, 0, "", TokenType.OR);
                        case 't': return CheckKeyword(2, 7, "herwise", TokenType.ELSE);
                    }
                    break;
                case 'p': return CheckKeyword(1, 4, "rint", TokenType.PRINT);
                case 't': return CheckKeyword(1, 3, "rue", TokenType.TRUE);
                case 'v': return CheckKeyword(1, 2, "ar", TokenType.VAR);
                case 'w':
                    if (length <= 1)
                        break;
                    if (identifier[1] == 'h')
                    {
                        if (length <= 2)
                            break;
                        switch (identifier[2])
                        {
                            case 'e': return CheckKeyword(3, 1, "n", TokenType.WHEN);
                            case 'i': return CheckKeyword(3, 2, "le", TokenType.WHILE);
                        }
                    }
                    break;
            }

            if (previousToken.type == TokenType.VAR)
            {
                return TokenType.IDENTIFIER;
            }

            if (this.identifiers.TryGetValue(this.currentLine.Substring(startIndex, currentIndex - startIndex), out Token val))
            {
                return TokenType.IDENTIFIER;
            }

            return TokenType.UNIDENTIFIED;
        }

        private TokenType CheckKeyword(int start, int length, string rest, TokenType type)
        {
            string keyword = this.currentLine.Substring(this.startIndex + start, length).ToLowerInvariant();

            if (this.currentIndex - this.startIndex == start + length && keyword == rest)
            {
                return type;
            }

            return TokenType.IDENTIFIER;
        }

        private bool IsAlpha(char c)
        {
            return (c >= 'a' && c <= 'z') ||
                   (c >= 'A' && c <= 'Z') ||
                   c == '_';
        }

        private Token GetStringToken()
        {
            while (!this.EndOfLine())
            {
                char nextCharacter = this.PeekCharacter();
                if (nextCharacter == '"' || nextCharacter == '\n' || nextCharacter == '\r')
                {
                    break;
                }

                PopCharacter();
            }

            if (PeekCharacter() == '"')
            {
                PopCharacter();
            }

            // trim quotes
            startIndex++;
            currentIndex--;
            string literal = this.currentLine.Substring(startIndex, currentIndex - startIndex);
            var token = this.MakeToken(TokenType.STRING, literal);
            startIndex--;
            currentIndex++;

            return token;
        }

        private bool IsDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

        private Token GetNumberToken()
        {
            while (this.IsDigit(this.PeekCharacter()))
            {
                this.PopCharacter();
            }

            if (this.PeekCharacter() == '.' && this.IsDigit(this.PeekNextCharacter()))
            {
                this.PopCharacter();

                while (this.IsDigit(this.PeekCharacter()))
                {
                    this.PopCharacter();
                }
            }

            double.TryParse(this.currentLine.Substring(startIndex, currentIndex - startIndex), out double literal);
            return this.MakeToken(TokenType.DOUBLE, literal);
        }

        #endregion

        #region LINE QUEUE MANAGEMENT

        private char PopCharacter()
        {
            var token = this.currentLine[this.currentIndex];
            this.currentIndex++;

            return token;
        }

        private char PeekCharacter()
        {
            return this.currentLine[this.currentIndex];
        }

        private char PeekPreviousCharacter()
        {
            return currentIndex - 1 >= 0 ? this.currentLine[this.currentIndex - 1] : '\0';
        }

        private char PeekNextCharacter()
        {
            return currentIndex + 1 >= this.endIndex ? '\0' : this.currentLine[this.currentIndex + 1];
        }

        private bool PopOnMatch(char c)
        {
            if (this.EndOfLine() || this.currentLine[this.currentIndex] != c)
            {
                return false;
            }

            this.currentIndex++;
            return true;
        }

        #endregion
    }
}