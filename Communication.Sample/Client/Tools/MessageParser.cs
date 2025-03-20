using Communication.Sample.Client.Enums;

namespace Communication.Sample.Client.Tools;

internal static class MessageParser
{
    internal static SdrCommand GetCommandType(byte[] command)
    {
        byte[] number = BitConverter.IsLittleEndian? [command[2], command[3]]: [command[3], command[2]];
        return (SdrCommand)BitConverter.ToInt16(number);
    }
    internal static byte[] GetCommandData(byte[] command)
    {
        var length = GetMessageLength(command) - 4/*header 2 fifo sequence number 2*/;
        return new ArraySegment<byte>(command, 3, length).ToArray();
    }

    internal static byte[] GetCommandParameters(byte[] command)
    {
        var length = GetMessageLength(command) -4/*header 2 command 2*/;
        return new ArraySegment<byte>(command, 3, length).ToArray();
    }

    internal static short GetMessageLength(byte[] message)
    {
        return (short)(((message[1] & 0b00011111) << 8) | message[0]);
    }

    internal static TargetMessageType GetMessageType(byte[] response)
    {
        return (TargetMessageType)((response[1] & 0b11100000) >> 5);
    }
}
