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
        SEMICOLON
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
                case "where": return Symbol.WHERE;
            }

            Debugger.Break();
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
            for (int i = 0; i < mTokens.Count; ++i)
            {
                if (mTokens[i].mTokenType == TokenType.SYMBOL && mTokens[i].mSymbol == symbol)
                    return true;
            }
            return false;
        }

        public Query(string query)
        {
            char[] queryChars = query.ToCharArray();
            StringBuilder tokenBuilder = new StringBuilder();
            bool inString = false;
            bool inIdentifier = false;
            bool inOperator = false;
            char stringDelimiter = new char();
            char c = new char();
            char previousChar = new char();

            for (int i = 0; i < queryChars.Length; ++i)
            {
                previousChar = c;
                c = queryChars[i];

                if (inString)
                {
                    if (c == stringDelimiter && previousChar != '\\')
                    {
                        inString = false;

                        tokenBuilder.Append(c);
                        mTokens.Add(new Token(tokenBuilder.ToString(), TokenType.STRING));
                        tokenBuilder = new StringBuilder();
                        continue;
                    }
                }
                else if (inIdentifier)
                {
                    if (!IsSubsequentIdentifierChar(c))
                    {
                        inIdentifier = false;

                        mTokens.Add(new Token(tokenBuilder.ToString(), TokenType.SYMBOL));
                        tokenBuilder = new StringBuilder();
                    }
                }
                else if (inOperator)
                {
                    if (!IsOperatorChar(c))
                    {
                        inOperator = false;
                        mTokens.Add(new Token(tokenBuilder.ToString(), TokenType.OPERATOR));
                        tokenBuilder = new StringBuilder();
                    }
                }

                if (!inString && !inIdentifier && !inOperator)
                {
                    if (c == '\'' || c == '"')
                    {
                        inString = true;
                        stringDelimiter = c;
                    }
                    else if (IsInitialIdentifierChar(c))
                    {
                        inIdentifier = true;
                    }
                    else if (IsOperatorChar(c))
                    {
                        inOperator = true;
                    }
                    else if (Char.IsWhiteSpace(c))
                    {
                        continue;
                    }
                    else if (c == ';')
                    {
                        mTokens.Add(new Token(";", TokenType.SEMICOLON));
                    }
                    else
                    {
                        Debugger.Break();
                    }
                }

                tokenBuilder.Append(c);                
            }
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

        void Describe()
        {
            Debugger.Break();
        }

        public override ConnectionState Execute(Connection connection)
        {
            if (mQuery.IsSymbolAt(0, Symbol.SET) && mQuery.IsSymbolAt(1, Symbol.NAMES))
            {
                Token namesToken = mQuery.mTokens[2];
                Util.Verify(namesToken.mTokenType == TokenType.STRING && namesToken.mTokenText == "'utf8'");
                connection.Status = Status.GetStatus(Status.OK);
            }
            else if (mQuery.IsSymbolAt(0, Symbol.DESCRIBE))
            {
                Describe();
            }
            else if (mQuery.IsSymbolAt(0, Symbol.SHOW) && mQuery.IsSymbolAt(1, Symbol.TABLES))
            {
                Debugger.Break();
            }
            else
            {
                if (mQuery.HasSymbol(Symbol.LIMIT))
                {
                    Debugger.Break();
                }
                if (mQuery.IsSymbolAt(0, Symbol.CREATE) && mQuery.IsSymbolAt(1, Symbol.TABLE))
                {
                    Debugger.Break();
                }

                OdbcCommand command = new System.Data.Odbc.OdbcCommand(mQueryString, connection.DatabaseConnection);

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
