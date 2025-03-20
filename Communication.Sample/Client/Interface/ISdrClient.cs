

using Communication.Sample.Client.Enums;

namespace Communication.Sample.Client.Interface;

public interface ISdrClient
{
    event EventHandler<byte[]> Data0Received;
    event EventHandler<byte[]> Data1Received;
    event EventHandler<byte[]> Data2Received;
    event EventHandler<byte[]> Data3Received;
    event EventHandler<(byte[] command, IEnumerable<byte> options)> UnsolicitedStatusChanged;

    Task CloseAsync(CancellationToken cancellationToken);
    Task OpenAsync(CancellationToken cancellationToken);

    Task<long> GetReceiverFrequency(ReceiverChannel channel, CancellationToken cancellationToken);
    Task<Dictionary<ReceiverChannel, (long min, long max, long vco)>> GetReceiverFrequencyRanges(CancellationToken cancellationToken);
    Task SetReceiverFrequency(ReceiverChannel channel, long frequency, CancellationToken cancellationToken);
    Task SetReceiverState(SampleMode sampleMode, TransferOption transferOption, CaptureMode captureMode, byte? samplesCount, CancellationToken cancellationToken);
}
