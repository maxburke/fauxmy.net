using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace fxmy.net
{
    enum CommandType
    {
        SLEEP,
        QUIT,
        INIT_DATABASE,
        QUERY,
        FIELD_LIST,
        CREATE_DATABASE,
        DROP_DATABASE,
        REFRESH,
        SHUTDOWN,
        STATISTICS,
        PROCESS_INFO,
        CONNECT,
        PROCESS_KILL,
        DEBUG,
        PING,
        TIME,
        DELAYED_INSERT,
        CHANGE_USER,
        BINLOG_DUMP,
        TABLE_DUMP,
        CONNECT_OUT,
        REGISTER_SLAVE,
        STATEMENT_PREPARE,
        STATEMENT_EXECUTE,
        STATEMENT_SEND_LONG_DATA,
        STATEMENT_CLOSE,
        STATEMENT_RESET,
        SET_OPTION,
        STATEMENT_FETCH,
    }

    class UnsupportedCommandException : Exception
    {
        public UnsupportedCommandException(CommandType command)
            : base(string.Format("Command {0} is currently unsupported.", command))
        {
        }
    }

    abstract class Command : IDisposable
    {
        public abstract ConnectionState Execute(Connection connection);

        public virtual void Dispose()
        {
        }
    }

    class SleepCommand : Command
    {
        public SleepCommand(NetworkBufferReader reader)
        {
            Debugger.Break();
        }

        public override ConnectionState Execute(Connection connection)
        {
            throw new NotImplementedException();
        }
    }

    class QuitCommand : Command
    {
        public QuitCommand(NetworkBufferReader reader)
        {
            Debugger.Break();
        }

        public override ConnectionState Execute(Connection connection)
        {
            return ConnectionState.EXIT;
        }
    }

    class InitializeDatabaseCommand : Command
    {
        string mDatabase;

        public InitializeDatabaseCommand(NetworkBufferReader reader)
        {
            mDatabase = reader.ReadString();
        }

        public override ConnectionState Execute(Connection connection)
        {
            connection.Connect(mDatabase);
            connection.Status = Status.GetStatus(Status.OK);

            return ConnectionState.CONTINUE;
        }
    }

    class SetOptionCommand : Command
    {
        bool mMultiStatements;

        public SetOptionCommand(NetworkBufferReader reader)
        {
            ushort option = reader.ReadUint16();
            mMultiStatements = (option == 1);
        }

        public override ConnectionState Execute(Connection connection)
        {
            connection.MultiStatements = mMultiStatements;
            connection.Status = Status.GetStatus(Status.OK);

            return ConnectionState.CONTINUE;
        }
    }

    class CommandPacket
    {
        public static Command CreateCommand(byte[] commandPacketData)
        {
            CommandType command = (CommandType)commandPacketData[0];
            int packetSizeLessCommand = commandPacketData.Length - 1;
            MemoryStream memoryStream = new MemoryStream(commandPacketData, 1, packetSizeLessCommand);
            NetworkBufferReader reader = new NetworkBufferReader(memoryStream);

            switch (command)
            {
                case CommandType.SLEEP:
                    return new SleepCommand(reader);
                case CommandType.QUIT:
                    return new QuitCommand(reader);
                case CommandType.INIT_DATABASE:
                    return new InitializeDatabaseCommand(reader);
                case CommandType.QUERY:
                    return new QueryCommand(reader);
                case CommandType.SET_OPTION:
                    return new SetOptionCommand(reader);

                case CommandType.FIELD_LIST:
                case CommandType.CREATE_DATABASE:
                case CommandType.DROP_DATABASE:
                case CommandType.REFRESH:
                case CommandType.SHUTDOWN:
                case CommandType.STATISTICS:
                case CommandType.PROCESS_INFO:
                case CommandType.CONNECT:
                case CommandType.PROCESS_KILL:
                case CommandType.DEBUG:
                case CommandType.PING:
                case CommandType.TIME:
                case CommandType.DELAYED_INSERT:
                case CommandType.CHANGE_USER:
                case CommandType.BINLOG_DUMP:
                case CommandType.TABLE_DUMP:
                case CommandType.CONNECT_OUT:
                case CommandType.REGISTER_SLAVE:
                case CommandType.STATEMENT_PREPARE:
                case CommandType.STATEMENT_EXECUTE:
                case CommandType.STATEMENT_SEND_LONG_DATA:
                case CommandType.STATEMENT_CLOSE:
                case CommandType.STATEMENT_RESET:
                case CommandType.STATEMENT_FETCH:
                    throw new UnsupportedCommandException(command);
            }

            return null;
        }
    }
}
