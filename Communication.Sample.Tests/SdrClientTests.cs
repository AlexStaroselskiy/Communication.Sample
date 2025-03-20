using Communication.Sample.Client.Interface;
using Communication.Sample.Client;
using Moq;
using Communication.Sample.Transport;
using Communication.Sample.Transport.Interface;

namespace Communication.Sample.Tests
{

    public class SdrClientTests
    {

        private static class SdrRequests
        {
            public static byte[] StartCaptureRequest  = new byte[] { 0x08, 0x00, 0x18, 0x00, 0x80, 0x02, 0x01, 0x0A };
            public static byte[] StopCaptureRequest = new byte[] { 0x08, 0x00, 0x18, 0x00, 0x00, 0x01, 0x00, 0x00 };
            public static byte[] SetFrequencyCh1Request = new byte[] { 0x0A, 0x00, 0x20, 0x00, 0x00, 0x90, 0xC6, 0xD5, 0x00, 0x00 };
            public static byte[] GetCurrentFrequencyCh2Request = new byte[] { 0x05, 0x20, 0x20, 0x00, 0x02 };
            public static byte[] GetAvailableFrequencyRequest = new byte[] { 0x05, 0x40, 0x20, 0x00, 0x00 };
        }
        private static class SdrResponses
        {
            public static byte[] ComplexSetupStartACK = new byte[] { 0x08, 0x00, 0x18, 0x00, 0x80, 0x02, 0x00, 0x00 };
            public static byte[] ComplexSetupStopACK = new byte[] { 0x08, 0x00, 0x18, 0x00, 0x00, 0x01, 0x00, 0x00 };
            public static byte[] FrequencySetAck = new byte[] { 0x0A, 0x00, 0x20, 0x00, 0x00, 0x90, 0xC6, 0xD5, 0x00, 0x00 };
            public static byte[] FrequencyGetAck = new byte[] { 0x0A, 0x00, 0x20, 0x00, 0x02, 0x90, 0xC6, 0xD5, 0x00, 0x00 };
            public static byte[] FrequencyGetRangesACK = new byte[] {
    0x24, 0x40, 0x20, 0x00, 0x00, 0x02,  0xA0, 0x86, 0x01, 0x00, 0x00,  0x80, 0xCC, 0x06, 0x02, 0x00,  0x00, 0x00, 0x00, 0x00, 0x00,  
                                         0x00, 0x3B, 0x58, 0x08, 0x00,  0x80, 0xD1, 0xF0, 0x08, 0x00,  0x00, 0x68, 0x89, 0x09, 0x00
};
        }

        private readonly ISdrClient _sdrClient;
        private Mock<ICommunicationChannel> _mockControlChannel;
        private readonly Mock<ICommunicationChannel> _mockDataChannel;

        public SdrClientTests()
        {
            _mockControlChannel = new Mock<ICommunicationChannel>();
            _mockDataChannel = new Mock<ICommunicationChannel>();

            BasicControllChannelSetup();

            _sdrClient = new SdrClient(_mockControlChannel.Object, _mockDataChannel.Object); // Assuming SdrClient implements ISdrClient
        }

        private void BasicControllChannelSetup()
        {
            _mockControlChannel.Setup(arg => arg.OpenAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockControlChannel.Setup(arg => arg.CloseAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockDataChannel.Setup(arg => arg.OpenAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockDataChannel.Setup(arg => arg.CloseAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        }

        [Fact]
        public async Task OpenAsync_Should_Call_OpenAsync_On_Channel()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;

            // Act
            await _sdrClient.OpenAsync(cancellationToken);

            // Assert
            _mockControlChannel.Verify(ch => ch.OpenAsync(cancellationToken), Times.Once);
        }

        [Fact]
        public async Task CloseAsync_Should_Call_CloseAsync_On_Channel()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            await _sdrClient.OpenAsync(cancellationToken); // Assuming it is open

            // Act
            await _sdrClient.CloseAsync(cancellationToken);

            // Assert
            _mockControlChannel.Verify(ch => ch.CloseAsync(cancellationToken), Times.Once);
        }

        [Fact]
        public async Task SetReceiverState_Should_Send_Expected_Command()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            _mockControlChannel.Setup(ch => ch.SendAsync(It.IsAny<byte[]>(), cancellationToken))
                        .Returns(Task.CompletedTask);

            _mockControlChannel.Setup(ch => ch.ReceiveAsync( cancellationToken))
                .Returns(Task.FromResult(SdrResponses.ComplexSetupStartACK));

            await _sdrClient.OpenAsync(cancellationToken); // Assuming it is open
            // Act
            await _sdrClient.SetReceiverState(SampleMode.Complex, TransferOption.Start, CaptureMode.Fifo16, 10, cancellationToken);

            // Assert
            _mockControlChannel.Verify(ch => ch.SendAsync(It.Is<byte[]>(b => b.SequenceEqual(SdrRequests.StartCaptureRequest)), cancellationToken), Times.Once);
        }

        [Fact]
        public async Task SetReceiverFrequency_Should_Return_Expected_Data()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            var expectedData = 14010000;

            _mockControlChannel.Setup(ch => ch.ReceiveAsync(cancellationToken))
                        .ReturnsAsync(SdrResponses.FrequencySetAck);
            await _sdrClient.OpenAsync(cancellationToken);
            // Act
            await _sdrClient.SetReceiverFrequency(ReceiverChannel.Channel1,expectedData,cancellationToken);

            _mockControlChannel.Verify(ch => ch.SendAsync(It.Is<byte[]>(b => b.SequenceEqual(SdrRequests.SetFrequencyCh1Request)), cancellationToken), Times.Once);
        }

        [Fact]
        public async Task GeteceiverFrequency_Should_Return_Expected_Data()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            var expectedData = 14010000;

            _mockControlChannel.Setup(ch => ch.ReceiveAsync(cancellationToken))
                        .ReturnsAsync(SdrResponses.FrequencySetAck);
            await _sdrClient.OpenAsync(cancellationToken);
            // Act
            var frequency = await _sdrClient.GetReceiverFrequency(ReceiverChannel.Channel2, cancellationToken);

            _mockControlChannel.Verify(ch => ch.SendAsync(It.Is<byte[]>(b => b.SequenceEqual(SdrRequests.GetCurrentFrequencyCh2Request)), cancellationToken), Times.Once);

            Assert.Equal(expectedData, frequency);
        }

        [Fact]
        public async Task GetReceiverFrequencyRanges_Should_Return_Expected_Data()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            var expectedData = new Dictionary<ReceiverChannel, (long, long, long)>
        {
            { ReceiverChannel.Channel1, (0x00000186A0, 0x000206CC80, 0) },
            { ReceiverChannel.Channel2, (0x0008583B00, 0x0008F0D180, 0x0009896800) }
        };

            _mockControlChannel.Setup(ch => ch.ReceiveAsync(cancellationToken))
                        .ReturnsAsync(SdrResponses.FrequencyGetRangesACK);
            await _sdrClient.OpenAsync(cancellationToken);
            // Act
            var result = await _sdrClient.GetReceiverFrequencyRanges(cancellationToken);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedData.Count, result.Count);
            Assert.Equal(expectedData[ReceiverChannel.Channel1], result[ReceiverChannel.Channel1]);
            Assert.Equal(expectedData[ReceiverChannel.Channel2], result[ReceiverChannel.Channel2]);
        }
    }
}