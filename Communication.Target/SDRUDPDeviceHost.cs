using System.Buffers;
using System.Net;
using System.Net.Sockets;

namespace Communication.Target;

public class SDRUDPDeviceHost

{
    private readonly int _port;
    private readonly int _bufferSize;
    private readonly Socket _socket;

    public SDRUDPDeviceHost()
    {
        _port = 60000;
        _bufferSize = 8194;
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        _socket.ReceiveBufferSize = _bufferSize;
        _socket.Bind(new IPEndPoint(IPAddress.Loopback, _port));
    }

    public async Task Start()
    {
        EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
        Console.WriteLine("[UDP] Server is running...");

        while (true)
        {

            byte[] buffer = ArrayPool<byte>.Shared.Rent(_bufferSize);
            try
            {
                var segment = new Memory<byte>(buffer);
                var result = await _socket.ReceiveFromAsync(segment, SocketFlags.None, remoteEndPoint).ConfigureAwait(false);

                var message = segment.Slice(0, result.ReceivedBytes).ToArray();
                // Process in a separate task (non-blocking)
                Task.Run(() => ProcessPacket(result.RemoteEndPoint, message));
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }

    private void ProcessPacket(EndPoint remoteEndPoint, Span<byte> arraySegment)
    {
        Console.WriteLine($"[UDP] Received {arraySegment.Length} bytes");
        _socket.SendTo(arraySegment, remoteEndPoint);
    }

    public void Stop()
    {
        _socket.Shutdown(SocketShutdown.Both);
        _socket.Close();
    }
}
