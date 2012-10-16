using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Data.Odbc;

namespace fxmy.net
{
    enum TokenType
    {
        INVALID,
        SYMBOL,
        STRING,
        OPERATOR,
        NUMBER,
        SEMICOLON,
        STRING_DELIMITER,
        COMMA
    }

    enum Symbol
    {
        INVALID,
        IDENTIFIER,
        CREATE,
        DATABASE,
        DESCRIBE,
        FROM,
        LIMIT,
        NAMES,
        SELECT,
        SET,
        SHOW,
        TABLE,
        TABLES,
        TOP,
        WHERE,
    }

    class Token
    {
        public string mTokenText;
        public TokenType mTokenType;
        public Symbol mSymbol;

        static Symbol ClassifySymbol(string symbolString)
        {
            string lowerCaseString = symbolString.ToLower();

            switch (lowerCaseString)
            {
                case "create": return Symbol.CREATE;
                case "database": return Symbol.DATABASE;
                case "describe": return Symbol.DESCRIBE;
                case "from": return Symbol.FROM;
                case "limit": return Symbol.LIMIT;
                case "names": return Symbol.NAMES;
                case "select": return Symbol.SELECT;
                case "set": return Symbol.SET;
                case "show": return Symbol.SHOW;
                case "table": return Symbol.TABLE;
                case "tables": return Symbol.TABLES;
                case "top": return Symbol.TOP;
                case "where": return Symbol.WHERE;
            }

            return Symbol.IDENTIFIER;
        }

        public Token(string tokenText, TokenType tokenType)
        {
            mTokenText = tokenText;
            mTokenType = tokenType;

            if (tokenType == TokenType.SYMBOL)
            {
                mSymbol = ClassifySymbol(mTokenText);
            }
        }

        public override string ToString()
        {
            if (mTokenType == TokenType.SYMBOL)
            {
                return string.Format("{0}: {1} \"{2}\"", mTokenType, mSymbol, mTokenText);
            }
            else
            {
                return string.Format("{0}: \"{1}\"", mTokenType, mTokenText);
            }
        }
    }

    class Query
    {
        public List<Token> mTokens = new List<Token>();

        static bool IsOperatorChar(char c)
        {
            return c == '=';
        }

        static bool IsInitialIdentifierChar(char c)
        {
            return Char.IsLetter(c) || c == '_' || c == '@' || c == '#';
        }

        static bool IsSubsequentIdentifierChar(char c)
        {
            return Char.IsLetterOrDigit(c) || c == '@' || c == '#' || c == '$' || c == '_';
        }

        public int IndexOfSymbol(Symbol symbol)
        {
            for (int i = 0; i < mTokens.Count; ++i)
            {
                if (mTokens[i].mTokenType == TokenType.SYMBOL && mTokens[i].mSymbol == symbol)
                    return i;
            }

            return -1;
        }

        public bool IsSymbolAt(int index, Symbol symbol)
        {
            if (mTokens.Count <= index)
                return false;

            if (mTokens[index].mTokenType != TokenType.SYMBOL)
                return false;

            return mTokens[index].mSymbol == symbol;
        }

        public bool HasSymbol(Symbol symbol)
        {
            return IndexOfSymbol(symbol) >= 0;
        }

        delegate bool EndOfTokenDelegate(char c, char previousChar, char stringDelimiter);

        static bool EndOfString(char c, char previousChar, char stringDelimiter)
        {
            return c == stringDelimiter && previousChar != '\\';
        }

        static bool EndOfSymbol(char c, char previousChar, char stringDelimiter)
        {
            return !IsSubsequentIdentifierChar(c);
        }

        static bool EndOfOperator(char c, char previousChar, char stringDelimiter)
        {
            return !IsOperatorChar(c);
        }

        static bool EndOfNumber(char c, char previousChar, char stringDelimiter)
        {
            return !Char.IsNumber(c);
        }

        static bool EndOfSingleCharToken(char c, char previousChar, char stringDelimiter)
        {
            return true;
        }

        static bool EndOfInvalid(char c, char previousChar, char stringDelimiter)
        {
            return false;
        }

        static EndOfTokenDelegate[] mEndOfTokenFunctions = new EndOfTokenDelegate[] { 
            EndOfInvalid,
            EndOfSymbol,
            EndOfString,
            EndOfOperator,
            EndOfNumber,
            EndOfSingleCharToken, 
            EndOfSingleCharToken,
            EndOfSingleCharToken
        };

        public Query(string query)
        {
            char[] queryChars = query.ToCharArray();
            StringBuilder tokenBuilder = new StringBuilder();

            char stringDelimiter = new char();
            char c = new char();
            char previousChar = new char();

            TokenType tokenType = TokenType.INVALID;

            for (int i = 0; i < queryChars.Length; ++i)
            {
                previousChar = c;
                c = queryChars[i];

                if (mEndOfTokenFunctions[(int)tokenType](c, previousChar, stringDelimiter))
                {
                    mTokens.Add(new Token(tokenBuilder.ToString(), tokenType));

                    if (tokenType == TokenType.STRING_DELIMITER)
                    {
                        int tokenCount = mTokens.Count;
                        if (tokenCount > 1 && mTokens[tokenCount - 2].mTokenType != TokenType.STRING)
                        {
                            tokenType = TokenType.STRING;
                        }
                        else
                        {
                            tokenType = TokenType.INVALID;
                        }
                    }
                    else
                    {
                        tokenType = TokenType.INVALID;
                    }

                    tokenBuilder = new StringBuilder();
                }

                if (tokenType == TokenType.INVALID)
                {
                    if (IsInitialIdentifierChar(c))
                    {
                        tokenType = TokenType.SYMBOL;
                    }
                    else if (IsOperatorChar(c))
                    {
                        tokenType = TokenType.OPERATOR;
                    }
                    else if (Char.IsWhiteSpace(c))
                    {
                        continue;
                    }
                    else if (c == ';')
                    {
                        tokenType = TokenType.SEMICOLON;
                    }
                    else if (Char.IsNumber(c))
                    {
                        tokenType = TokenType.NUMBER;
                    }
                    else if (c == '\'' || c == '"')
                    {
                        tokenType = TokenType.STRING_DELIMITER;
                        stringDelimiter = c;
                    }
                    else if (c == ',')
                    {
                        tokenType = TokenType.COMMA;
                    }
                    else
                    {
                        Debugger.Break();
                    }
                }

                tokenBuilder.Append(c);
            }

            if (tokenBuilder.Length > 0 && tokenType != TokenType.INVALID)
                mTokens.Add(new Token(tokenBuilder.ToString(), tokenType));
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            for (int i = 0; i < mTokens.Count - 1; ++i)
            {
                stringBuilder.Append(mTokens[i].mTokenText);

                // Add a space between all tokens unless we're adding the quotes around a string.
                // In that case, check to see if the current token is a string delimiter and the
                // next is a string, or the current is a string and the one after is a delimter,
                // ie: *'foo* or *foo'*, and skip adding the delimiter.
                if (!((mTokens[i].mTokenType == TokenType.STRING_DELIMITER && mTokens[i + 1].mTokenType == TokenType.STRING)
                    || (mTokens[i].mTokenType == TokenType.STRING && mTokens[i + 1].mTokenType == TokenType.STRING_DELIMITER)))
                {
                    stringBuilder.Append(' ');
                }
            }

            stringBuilder.Append(mTokens[mTokens.Count - 1].mTokenText);
            return stringBuilder.ToString();
        }
    }
    
    class QueryCommand : Command
    {
        string mQueryString;
        Query mQuery;

        public QueryCommand(NetworkBufferReader reader)
        {
            mQueryString = reader.ReadString().Trim();
            mQuery = new Query(mQueryString);
        }

        void Describe(Connection connection)
        {
            string describeCommand = string.Format("sp_columns {0}", mQuery.mTokens[1].mTokenText);
            OdbcCommand command = new OdbcCommand(describeCommand, connection.DatabaseConnection);

            try
            {
                OdbcDataReader reader = command.ExecuteReader();

                if (!reader.HasRows)
                {
                    connection.Status = Status.GetStatus(Status.ERROR_UNKNOWN_OBJECT);
                    return;
                }

                // Currently there is no implementation for Describe if the table
                // actually exists. TODO: implement!
                Debugger.Break();
            }
            catch (OdbcException e)
            {
                Log.LogErrors(e);
                connection.Status = Status.GetStatus(e.Errors[0]);
            }
        }

        void ShowTables(Connection connection)
        {
            // No implementation for SHOW TABLES yet. TODO: implement!
            Debugger.Break();
        }

        public override ConnectionState Execute(Connection connection)
        {
            string queryString = mQueryString;

            if (mQuery.IsSymbolAt(0, Symbol.SET) && mQuery.IsSymbolAt(1, Symbol.NAMES))
            {
                Token namesToken = mQuery.mTokens[3];
                Util.Verify(namesToken.mTokenType == TokenType.STRING && namesToken.mTokenText == "utf8");
                connection.Status = Status.GetStatus(Status.OK);
            }
            else if (mQuery.IsSymbolAt(0, Symbol.DESCRIBE))
            {
                Describe(connection);
            }
            else if (mQuery.IsSymbolAt(0, Symbol.SHOW) && mQuery.IsSymbolAt(1, Symbol.TABLES))
            {
                ShowTables(connection);
            }
            else
            {
                // MySQL limits rows with a "LIMIT x" clause at the end of the query,
                // but MSSQL expects a "SELECT TOP x" instead. This bit of code
                // rearranges the query so that MSSQL doesn't barf.
                if (mQuery.HasSymbol(Symbol.LIMIT))
                {
                    int limitIndex = mQuery.IndexOfSymbol(Symbol.LIMIT);
                    Token numberToken = mQuery.mTokens[limitIndex + 1];
                    Util.Verify(numberToken.mTokenType == TokenType.NUMBER);

                    // Remove the LIMIT token
                    mQuery.mTokens.RemoveAt(limitIndex);
                    // Remove the number token
                    mQuery.mTokens.RemoveAt(limitIndex);
                    
                    // Add the "TOP x" after the SELECT token.
                    int selectIndex = mQuery.IndexOfSymbol(Symbol.SELECT);
                    mQuery.mTokens.Insert(selectIndex + 1, new Token("TOP", TokenType.SYMBOL));
                    mQuery.mTokens.Insert(selectIndex + 2, numberToken);
                    queryString = mQuery.ToString();
                }
                if (mQuery.IsSymbolAt(0, Symbol.CREATE) && mQuery.IsSymbolAt(1, Symbol.TABLE))
                {
                    Debugger.Break();
                }

                OdbcCommand command = new OdbcCommand(queryString, connection.DatabaseConnection);

                if (mQuery.IsSymbolAt(0, Symbol.SELECT))
                {
                    try
                    {
                        OdbcDataReader reader = command.ExecuteReader();
                    }
                    catch (OdbcException e)
                    {
                        Log.LogErrors(e);
                        connection.Status = Status.GetStatus(e.Errors[0]);
                    }

                    Debugger.Break();
                }
                else
                {
                    int rowsAffected = command.ExecuteNonQuery();
                    Debugger.Break();
                }
            }

            return ConnectionState.CONTINUE;
        }
    }
}
