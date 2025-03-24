using BenchmarkDotNet.Attributes;
using System.Text;
using Communication.Sample.Transport;

namespace Communication.Sample.Benchmark
{
   public class ChannelBenchmark
    {
        private const string Message = "BenchmarkTest";
        private readonly byte[] _data = Encoding.UTF8.GetBytes(Message);

        private TCPCommunicationChannel _tcpClient;
        private UDPCommunicationChannel _udpClient;

        [GlobalSetup]
        public void Setup()
        {
            _tcpClient = new TCPCommunicationChannel();
            _udpClient = new UDPCommunicationChannel();
            _tcpClient.OpenAsync(default).Wait();
            _udpClient.OpenAsync(default).Wait();
        }

        [Benchmark]
        public async Task SendTcpAsync()
        {
            await _tcpClient.SendAsync(_data, default);
        }

        [Benchmark]
        public async Task SendUdpAsync()
        {
            await _udpClient.SendAsync(_data, default);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _tcpClient?.Dispose();
            _udpClient?.Dispose();
        }
    }
}
