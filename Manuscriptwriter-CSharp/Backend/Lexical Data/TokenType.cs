namespace MSW.Scripting
{
    internal enum TokenType
    {
        // Single-Character
        COLON, COMMA, HASH,

        // -- Mathematics
        MINUS, PLUS, MULTIPLY, DIVIDE, NEGATE,

        // Conditionals
        EQUAL, EQUAL_EQUAL, NOT, NOT_EQUAL,
        GREATER, GREATER_EQUAL, LESS, LESS_EQUAL,

        AND, OR,

        // Literals and Types
        IDENTIFIER, STRING, DOUBLE,

        // -- Keywords
        // Values
        TRUE, FALSE, NULL,

        // Declarations
        VAR,

        // Conditionals
        IF, ELSE,
        WHILE, FOR,

        // Passage definition
        PASSAGE, WHEN,
        FUNCTION, EVENT,
        GIVEN,

        // Baked functions
        PRINT, // Prints to console.

        EOF, EOL, UNIDENTIFIED, ERROR
    }
}
