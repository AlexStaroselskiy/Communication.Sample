namespace Communication.Sample.Client.Tools;

internal class MessageBuilder
{
    private Queue<byte[]> _options = new Queue<byte[]>();
    private readonly int headerLength = 2;
    private readonly int commandLength = 2;
    private HostMessageType _messageType;
    private SdrCommand? _command;

    internal MessageBuilder SetCommandType(SdrCommand command)
    {
        _command = command;
        return this;
    }
    internal MessageBuilder AddCommandParameter(byte[] options) { _options.Enqueue(options); return this; }
    internal MessageBuilder SetMessageType(HostMessageType messageType)
    {
        _messageType = messageType;
        return this;
    }

    internal byte[] BuildCommand()
    {
        var length = headerLength;
        if (_command != null) length += commandLength;
        if (_options.Any())
        {
            var listOptions = new List<byte>();
            while (_options.TryDequeue(out var option))
            {
                if (option != null)
                {
                    listOptions.AddRange(option);
                }
            }

            if (listOptions.Any())
            {
                length += listOptions.Count;
            }

            var header = CalculateHeader(length, (byte)_messageType, (short)_command);

            return [.. header,  .. listOptions];
        }
        else
        {
            var header = CalculateHeader(length, (byte)_messageType, (short)_command);

            return [.. header];
        }


    }

    private static byte[] CalculateHeader(int length, byte messageType, short? command)
    {
        var msb = (length & 0x1f00) >> 8;
        var lsb = (byte)(length & 0xff);
        var typeMsb = (byte)(((byte)messageType << 5) | msb);
        
        var commandByte = command !=null? BitConverter.GetBytes((short)command) : [];

        if (!BitConverter.IsLittleEndian)
        {
            commandByte = [.. commandByte.Reverse()];
        }
        return  [lsb, typeMsb, ..commandByte];
    }
}
