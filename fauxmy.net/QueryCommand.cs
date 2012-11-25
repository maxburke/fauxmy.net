using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Data.Odbc;

namespace fxmy.net
{
    public enum TokenType
    {
        INVALID,
        SYMBOL,
        STRING,
        OPERATOR,
        NUMBER,
        SEMICOLON,
        STRING_DELIMITER,
        LPAREN,
        RPAREN,
        COMMA
    }

    public enum Symbol
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
        LIKE,
    }

    public class Token
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
                case "like": return Symbol.LIKE;
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

    public class Query
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

        static bool IsNumericCharacter(char c)
        {
            return Char.IsDigit(c) || c == '.' || c == 'x' || c == 'X' || c == 'e' || c == 'E';
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

        static Token ParseSymbol(string query, ref int i)
        {
            StringBuilder builder = new StringBuilder();

            for (; i < query.Length; ++i)
            {
                char c = query[i];

                if (!IsSubsequentIdentifierChar(c))
                {
                    // Move the pointer back one as the top level loop in the
                    // function that calls this will advance it to the correct
                    // position.
                    --i;
                    break;
                }

                builder.Append(c);
            }

            return new Token(builder.ToString(), TokenType.SYMBOL);
        }

        static Token ParseOperator(string query, ref int i)
        {
            StringBuilder builder = new StringBuilder();

            for (; i < query.Length; ++i)
            {
                char c = query[i];

                if (!IsOperatorChar(c))
                {
                    --i;
                    break;
                }

                builder.Append(c);
            }

            return new Token(builder.ToString(), TokenType.OPERATOR);
        }

        static Token ParseNumber(string query, ref int i)
        {
            StringBuilder builder = new StringBuilder();

            for (; i < query.Length; ++i)
            {
                char c = query[i];

                if (!IsNumericCharacter(c))
                {
                    --i;
                    break;
                }

                builder.Append(c);
            }

            return new Token(builder.ToString(), TokenType.NUMBER);
        }

        static Token ParseString(string query, ref int i)
        {
            char delimiter = query[i];
            StringBuilder builder = new StringBuilder();

            ++i;

            for (; i < query.Length; ++i)
            {
                char c = query[i];
                bool isEscaped = (i >= 1) && query[i - 1] == '\\';

                if (c == delimiter && !isEscaped)
                {
                    // Technically we need to move the pointer i ahead one 
                    // character but the top level loop in the function that
                    // calls this one will do it for us.
                    break;
                }

                builder.Append(c);
            }

            return new Token(builder.ToString(), TokenType.STRING);
        }

        public Query(string query)
        {
            TokenType tokenType = new TokenType();

            for (int i = 0; i < query.Length; ++i)
            {
                char c = query[i];

                if (IsInitialIdentifierChar(c))
                {
                    mTokens.Add(ParseSymbol(query, ref i));
                }
                else if (IsOperatorChar(c))
                {
                    mTokens.Add(ParseOperator(query, ref i));
                }
                else if (Char.IsWhiteSpace(c))
                {
                    continue;
                }
                else if (c == ';')
                {
                    mTokens.Add(new Token(";", TokenType.SEMICOLON));
                }
                else if (Char.IsNumber(c))
                {
                    mTokens.Add(ParseNumber(query, ref i));
                }
                else if (c == '\'' || c == '"')
                {
                    mTokens.Add(new Token(new String(c, 1), TokenType.STRING_DELIMITER));
                    mTokens.Add(ParseString(query, ref i));
                    mTokens.Add(new Token(new String(c, 1), TokenType.STRING_DELIMITER));
                }
                else if (c == ',')
                {
                    mTokens.Add(new Token(",", TokenType.COMMA));
                }
                else if (c == '(')
                {
                    mTokens.Add(new Token("(", TokenType.LPAREN));
                }
                else if (c == ')')
                {
                    mTokens.Add(new Token(")", TokenType.RPAREN));
                }
                else
                {
                    Debugger.Break();
                }
            }
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

            Log.LogQuery(mQueryString);

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
            string tablesCommand = "sp_tables @table_owner='dbo'";
            int likeIndex = mQuery.IndexOfSymbol(Symbol.LIKE);

            if (likeIndex >= 0)
            {
                Debug.Assert(mQuery.mTokens.Count >= likeIndex + 2);

                Token delimiterToken = mQuery.mTokens[likeIndex + 1];
                Token likeToken = mQuery.mTokens[likeIndex + 2];

                Debug.Assert(delimiterToken.mTokenType == TokenType.STRING_DELIMITER);

                tablesCommand += string.Format(", @table_name={0}{1}{0}",
                    delimiterToken.mTokenText,
                    likeToken.mTokenText);
            }

            OdbcCommand command = new OdbcCommand(tablesCommand, connection.DatabaseConnection);

            try
            {
                OdbcDataReader reader = command.ExecuteReader();

                if (!reader.HasRows)
                {
                    connection.Status = Status.GetStatus(Status.OK);
                    return;
                }

                // Currently there is no implementation for SHOW TABLES if the table
                // actually exists. TODO: implement!
                Debugger.Break();
            }
            catch (OdbcException e)
            {
                Log.LogErrors(e);
                connection.Status = Status.GetStatus(e.Errors[0]);
            }
        }

        void CreateTable(Connection connection)
        {
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
            else if (mQuery.IsSymbolAt(0, Symbol.CREATE) && mQuery.IsSymbolAt(1, Symbol.TABLE))
            {
                CreateTable(connection);
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

                    //Debugger.Break();
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
