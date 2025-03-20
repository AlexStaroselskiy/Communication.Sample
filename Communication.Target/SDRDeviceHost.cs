using System.Buffers;
using System.Net;
using System.Net.Sockets;

namespace Communication.Sample.Host;

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
        Console.WriteLine("UDP Server is running...");
        _socket.Listen(10);

        while (true)
        {
            var socket =  _socket.Accept();
            Console.WriteLine("Client Connected!");
            byte[] buffer = ArrayPool<byte>.Shared.Rent(_bufferSize);
            try
            {
                var segment = new ArraySegment<byte>(buffer);
                var result = await socket.ReceiveAsync(segment, SocketFlags.None);

                // Process in a separate task (non-blocking)
                _ = Task.Run(() => ProcessPacket(socket, new ArraySegment<byte>(buffer).AsSpan()));
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }

    private void ProcessPacket(Socket socket, Span<byte> arraySegment)
    {
        Console.WriteLine($"Received {arraySegment.Length} bytes");
        socket.Send(arraySegment);
    }

    public void Stop()
    {
        _socket.Shutdown(SocketShutdown.Both);
        _socket.Close();
    }
}
