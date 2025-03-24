using Communication.Sample.Transport.Interface;
using System.Buffers;
using System.Net.Sockets;
using System.Net;
using System.Text;

namespace Communication.Sample.Transport;

public class TCPCommunicationChannel : ICommunicationChannel
{
    private readonly int _bufferSize;
    private readonly int _port;
    private readonly IPEndPoint _serverEndpoint;
    private readonly Socket _socket;
    private bool _disposed;

    public TCPCommunicationChannel()
    {
        _port = 50000;
        _bufferSize = 8194;
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _serverEndpoint = new IPEndPoint(IPAddress.Loopback, _port);
        _socket.ReceiveBufferSize = _bufferSize;
    }

    public async Task CloseAsync(CancellationToken cancellationToken)
    {
        _socket.Shutdown(SocketShutdown.Both);
        _socket.Close();

        await Task.CompletedTask;
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
            var result = await _socket.ReceiveAsync(segment, SocketFlags.None, cancellationToken);

            Console.WriteLine($"Received: {Encoding.ASCII.GetString(responseBuffer, 0, result)}");

            return segment.ToArray();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(responseBuffer);
        }
    }

    public async Task SendAsync(byte[] data, CancellationToken cancellationToken)
    {
        // Send message
        await _socket.SendAsync(data, SocketFlags.None, cancellationToken);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _socket.Dispose();
            }

            _disposed = true;
        }
    }


    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}