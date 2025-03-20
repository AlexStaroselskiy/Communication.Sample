using Communication.Sample.Transport.Interface;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Communication.Sample.Transport;

public class UDPCommunicationChannel : ICommunicationChannel
{
    private readonly int _bufferSize;
    private readonly int _port;
    private readonly IPEndPoint _serverEndpoint;
    private readonly Socket _socket;

    public UDPCommunicationChannel()
    {
        _port = 60000;
        _bufferSize = 8194;
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        _serverEndpoint = new IPEndPoint(IPAddress.Loopback, _port);
        _socket.ReceiveBufferSize = _bufferSize;
    }

    public async Task CloseAsync(CancellationToken cancellationToken)
    {
        _socket.Shutdown(SocketShutdown.Both);
        _socket.Close();
    }

    public async Task OpenAsync(CancellationToken cancellationToken)
    {
        await _socket.ConnectAsync(_serverEndpoint, cancellationToken);
    }

    public async Task<byte[]> ReceiveAsync(CancellationToken cancellationToken)
    {
        byte[] responseBuffer = ArrayPool<byte>.Shared.Rent(_bufferSize);
        try
        {
            var segment = new ArraySegment<byte>(responseBuffer);
            SocketReceiveMessageFromResult result = await _socket.ReceiveMessageFromAsync(segment, SocketFlags.None, _serverEndpoint, cancellationToken);

            Console.WriteLine($"Received: {Encoding.ASCII.GetString(responseBuffer, 0, result.ReceivedBytes)}");

            return responseBuffer.AsSpan(0, result.ReceivedBytes).ToArray();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(responseBuffer);
        }
    }

    public async Task SendAsync(byte[] data, CancellationToken cancellationToken)
    {
        // Send message
        await _socket.SendToAsync(data, SocketFlags.None, _serverEndpoint, cancellationToken);
    }
}