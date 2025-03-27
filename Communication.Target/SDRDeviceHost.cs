using System.Buffers;
using System.Net;
using System.Net.Sockets;

namespace Communication.Target;

public class SDRDeviceHost

{
    private readonly int _port;
    private readonly int _bufferSize;
    private readonly Socket _socket;

    public SDRDeviceHost()
    {
        _port = 50000;
        _bufferSize = 8194;
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _socket.ReceiveBufferSize = _bufferSize;
    }

    public async Task Start()
    {
        _socket.Bind(new IPEndPoint(IPAddress.Any, _port));
        Console.WriteLine("[TCP] Server is running...");
        _socket.Listen();

        while (true)
        {
            var socket = await _socket.AcceptAsync().ConfigureAwait(false);
            Console.WriteLine("[TCP] Client Connected!");
            while (socket.Connected)
            {

                byte[] buffer = ArrayPool<byte>.Shared.Rent(_bufferSize);
                try
                {
                    var segment = new Memory<byte>(buffer);
                    var result = await socket.ReceiveAsync(segment, SocketFlags.None).ConfigureAwait(false);
                    if(result == 0)
                    {
                        break;
                    }
                    var message = segment.Slice(0, result).ToArray();
                    // Process in a separate task (non-blocking)
                    Task.Run(() => ProcessPacket(socket, message));
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
        }
    }

    private static void ProcessPacket(Socket socket, Span<byte> arraySegment)
    {
        Console.WriteLine($"[TCP] Received {arraySegment.Length} bytes");
        socket.Send(arraySegment);
    }

    public void Stop()
    {
        _socket.Shutdown(SocketShutdown.Both);
        _socket.Close();
    }
}
