using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Data.Odbc;
using fxmy.net.Grammar;
using Antlr.Runtime;
using Antlr.Runtime.Tree;

namespace fxmy.net
{
    class QueryCommand : Command
    {
        string mQueryString;
        CommonTree mQueryTree;

        public QueryCommand(NetworkBufferReader reader)
        {
            string queryString = reader.ReadString().Trim();
            mQueryString = queryString;
            Log.LogQuery(queryString);

            ANTLRStringStream queryStringStream = new ANTLRStringStream(queryString);
            MySQLLexer lexer = new MySQLLexer(queryStringStream);
            CommonTokenStream tokenStream = new CommonTokenStream(lexer);
            MySQLParser parser = new MySQLParser(tokenStream);

            CommonTreeAdaptor treeAdaptor = new CommonTreeAdaptor();
            parser.TreeAdaptor = treeAdaptor;

            AstParserRuleReturnScope<object, IToken> parsed = parser.root_statement();

            mQueryTree = parsed.Tree as CommonTree;
            if (mQueryTree == null)
                throw new fxmy.net.ConnectionException("Unable to parse query.");
        }

        void Describe(Connection connection, List<CommonTree> parseTree)
        {
            string describeCommand = string.Format("sp_columns {0}", parseTree[1].Text);
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
                // actually exists. TODO: Implement!
                Debugger.Break();
            }
            catch (OdbcException e)
            {
                Log.LogErrors(e);
                connection.Status = Status.GetStatus(e.Errors[0]);
            }
        }

        public override ConnectionState Execute(Connection connection)
        {
            IList<ITree> rawChildren = mQueryTree.Children;
            List<CommonTree> children = new List<CommonTree>(rawChildren.Count);

            foreach (ITree treeInstance in rawChildren)
            {
                children.Add((CommonTree)treeInstance);
            }

            Debug.Assert(children.Count > 0);
            int child0 = children[0].Type;

            if (child0 == MySQLLexer.SET_SYM)
            {
                if (children[1].Type == MySQLLexer.NAMES_SYM)
                {
                    Util.Verify(children[2].Text == "'utf8'");
                    connection.Status = Status.GetStatus(Status.OK);
                }
            }
            else if (child0 == MySQLLexer.DESCRIBE || child0 == MySQLLexer.DESC)
            {
                Describe(connection, children);
            }
            else if (child0 == MySQLLexer.SHOW 
                && (children[1].Type == MySQLLexer.TABLES 
                    || (children[1].Type == MySQLLexer.FULL && children[2].Type == MySQLLexer.TABLES)))
            {
                Debugger.Break();
            }
            else if (child0 == MySQLLexer.CREATE && children[1].Type == MySQLLexer.TABLE)
            {
                Debugger.Break();
            }
            else
            {
                string queryString = mQueryString;
                bool isSelect = child0 == MySQLLexer.SELECT;

                if (mQueryString.Contains("LIMIT"))
                {
                    StringBuilder topBuilder = new StringBuilder();
                    StringBuilder queryBuilder = new StringBuilder();
                    int i;

                    topBuilder.Append("SELECT TOP ");

                    // Skip the SELECT token, begin at 1.
                    for (i = 1; i < children.Count; ++i)
                    {
                        if (children[i].Type == MySQLLexer.LIMIT)
                        {
                            ++i;
                            topBuilder.Append(children[i].Text);
                        }
                        else
                        {
                            queryBuilder.Append(' ').Append(children[i].Text);
                        }
                    }

                    queryString = topBuilder.Append(' ').Append(queryBuilder).ToString();
                }

                OdbcCommand command = new OdbcCommand(queryString, connection.DatabaseConnection);

                if (isSelect)
                {
                    try
                    {
                        OdbcDataReader reader = command.ExecuteReader();
                        Debugger.Break();
                    }
                    catch (OdbcException e)
                    {
                        Log.LogErrors(e);
                        connection.Status = Status.GetStatus(e.Errors[0]);
                    }
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