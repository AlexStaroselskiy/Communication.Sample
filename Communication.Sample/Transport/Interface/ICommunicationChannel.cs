namespace Communication.Sample.Transport.Interface;

public interface ICommunicationChannel: IDisposable
{
    Task OpenAsync(CancellationToken cancellationToken);
    Task CloseAsync(CancellationToken cancellationToken);
    Task SendAsync(byte[] data,CancellationToken cancellationToken);
    Task<byte[]> ReceiveAsync(CancellationToken cancellationToken);
}
