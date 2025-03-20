// See https://aka.ms/new-console-template for more information

using Communication.Sample.Client;
using Communication.Sample.Client.Enums;
using Communication.Sample.Transport;
using System.IO;

const string iqFileName = "somefile.iq";

if(!File.Exists(iqFileName))
{
    File.Create(iqFileName).Close();
}
else
{
    File.Open(iqFileName, FileMode.Truncate).Close();
}

var client = new SdrClient(new TCPCommunicationChannel(),new UDPCommunicationChannel());
client.Data0Received += Client_Data0Received;

await client.OpenAsync(cancellationToken: CancellationToken.None);
await client.SetReceiverState(SampleMode.Real, TransferOption.Start, CaptureMode.Fifo16, 10, CancellationToken.None);

void Client_Data0Received(object? sender, byte[] e)
{
    using var file = new FileStream(iqFileName, FileMode.Append,  FileAccess.Write);
    file.Write(e, 0, e.Length);
}