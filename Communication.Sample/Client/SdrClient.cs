﻿using Communication.Sample.Client.Enums;
using Communication.Sample.Client.Exceptions;
using Communication.Sample.Client.Interface;
using Communication.Sample.Client.Tools;
using Communication.Sample.Transport.Interface;

namespace Communication.Sample.Client;

public class SdrClient : ISdrClient, IDisposable
{
    private readonly ICommunicationChannel _dataChannel;
    private readonly CancellationTokenSource _dataChannelCancelationTokenSource;
    private ICommunicationChannel _controlChannel;
    private Task? _dataChannelProcessingTask;
    private bool _isConnected;
    private bool _disposed = false;

    public Exception LastException { get; private set; }

    public SdrClient(ICommunicationChannel controlChannel, ICommunicationChannel dataChannel)
    {
        _controlChannel = controlChannel;
        _dataChannel = dataChannel;
        _dataChannelCancelationTokenSource = new CancellationTokenSource();
    }

    #region Events

    public event EventHandler<byte[]>? Data0Received;

    public event EventHandler<byte[]>? Data1Received;

    public event EventHandler<byte[]>? Data2Received;

    public event EventHandler<byte[]>? Data3Received;

    public event EventHandler<(byte[] command, IEnumerable<byte> options)>? UnsolicitedStatusChanged;

    #endregion Events

    public async Task CloseAsync(CancellationToken cancellationToken)
    {
        VerifyConnected();
        await _controlChannel.CloseAsync(cancellationToken).ConfigureAwait(false);

        if (_dataChannelProcessingTask != null && !_dataChannelProcessingTask.IsCanceled)
        {
            _dataChannelCancelationTokenSource.Cancel();
            await _dataChannelProcessingTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        else if (_dataChannelProcessingTask != null)
        {
            // ensure that the task is completed
            await _dataChannelProcessingTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        await _dataChannel.CloseAsync(cancellationToken).ConfigureAwait(false);

        _isConnected = false;
    }

    public async Task<long> GetReceiverFrequency(ReceiverChannel channel, CancellationToken cancellationToken)
    {
        VerifyConnected();

        if (channel == ReceiverChannel.All)
        {
            throw new ArgumentException("Invalid channel", nameof(channel));
        }

        var sdrCommand = SdrCommand.SetReceiverFrequency;
        MessageBuilder builder = new();
        builder.SetMessageType(HostMessageType.RequestCurrentControlItem);
        builder.SetCommandType(sdrCommand);
        builder.AddCommandParameter([(byte)channel]);

        var command = builder.BuildCommand();

        return await Dispatch(sdrCommand, command, message =>
        {
            var frequency = new ArraySegment<byte>(message, 5, 5);

            return BitConverter.ToInt64([.. frequency, 0, 0, 0]);
        }, cancellationToken);
    }

    public async Task<Dictionary<ReceiverChannel, (long min, long max, long vco)>> GetReceiverFrequencyRanges(CancellationToken cancellationToken)
    {
        VerifyConnected();
        var sdrCommand = SdrCommand.SetReceiverFrequency;

        MessageBuilder builder = new();
        builder.SetMessageType(HostMessageType.RequestControlItemRange);
        builder.SetCommandType(sdrCommand);
        builder.AddCommandParameter([(byte)ReceiverChannel.All]);

        var command = builder.BuildCommand();

        return await Dispatch(sdrCommand, command, message =>
        {

            // 2 header + 2 command + 1 channel id +1 channelid
            var frequency1 = ParseFrequencyRange(new Memory<byte>(message, 6, 15));
            var frequency2 = ParseFrequencyRange(new Memory<byte>(message, 21, 15));

            return new Dictionary<ReceiverChannel, (long min, long max, long vco)>()
            {
                { ReceiverChannel.Channel1, frequency1},
                { ReceiverChannel.Channel2, frequency2}
            };
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task OpenAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.WhenAll(
                [
                    _controlChannel.OpenAsync(cancellationToken),
                    _dataChannel.OpenAsync(cancellationToken)
                ])
                .ConfigureAwait(false);
            _isConnected = true;
        }
        catch (AggregateException ex)
        {
            // only first exception is important
            _isConnected = false;
            LastException = ex;
            throw new InvalidOperationException("Failed to open the connection", ex);
        }
    }

    public async Task SetReceiverFrequency(ReceiverChannel channel, Int64 frequency, CancellationToken cancellationToken)
    {
        VerifyConnected();

        if (frequency <= 0 || frequency > 1099511627775)
        {
            throw new ArgumentOutOfRangeException(nameof(frequency));
        }

        var frequencyBytes = BitConverter.GetBytes(frequency).Take(5);

        if (!BitConverter.IsLittleEndian)
        {
            frequencyBytes = frequencyBytes.Reverse();
        }

        MessageBuilder builder = new();
        builder.SetMessageType(HostMessageType.SetControlItem);
        builder.SetCommandType(SdrCommand.SetReceiverFrequency);
        builder.AddCommandParameter([(byte)channel]);
        builder.AddCommandParameter(frequencyBytes.ToArray());

        var command = builder.BuildCommand();

        await _controlChannel.SendAsync(command, cancellationToken);

        while (cancellationToken.IsCancellationRequested)
        {
            var response = await _controlChannel.ReceiveAsync(cancellationToken).ConfigureAwait(false);
            if (response == null || response.Length == 0)
            {
                throw new IOException("Nothing really returned!");
            }

            var messageType = MessageParser.GetMessageType(response);
            if (messageType == TargetMessageType.ResponseToRequestControlItemRange)
            {
                var data = MessageParser.GetCommandType(response);
                if (data == SdrCommand.StateCommand)
                {
                    break;
                }

                if (data == SdrCommand.Error)
                {
                    var param = MessageParser.GetCommandParameters(response);
                    throw new SdrException("On SetReceiver state error occured!", param[0]);
                }
            }
            else if (messageType == TargetMessageType.UnsolicitedControlItem)
            {
                var param = MessageParser.GetCommandParameters(response);

                UnsolicitedStatusChanged?.Invoke(this, (response.AsSpan(2, 2).ToArray(), param));
            }
            else
            {
                throw new InvalidOperationException($"Invalid response type [ {Enum.GetName(messageType)} ]");
            }
        }
    }

    public async Task SetReceiverState(
        SampleMode sampleMode,
        TransferOption transferOption,
        CaptureMode captureMode,
        byte? samplesCount,
        CancellationToken cancellationToken)
    {
        VerifyConnected();
        if (captureMode == CaptureMode.Fifo16 && samplesCount == null)
        {
            throw new ArgumentNullException($"if we use fifo samples count must be provided. {nameof(samplesCount)}");
        }

        if (transferOption == TransferOption.Start)
        {
            // if we going to start the transfer we need to start the data channel processing
            _dataChannelProcessingTask = Task.Run(async ()=>
            {
                while (_dataChannelCancelationTokenSource.IsCancellationRequested)
                {
                  await  ReceiveData(_dataChannelCancelationTokenSource.Token).ConfigureAwait(false);
                }
            }, cancellationToken);

        }

        if (transferOption == TransferOption.Stop && _dataChannelProcessingTask != null)
        {
            // if we going to stop the transfer we need to stop the data channel processing
            _dataChannelCancelationTokenSource.Cancel();
            await _dataChannelProcessingTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        SdrCommand sdrCommand = SdrCommand.StateCommand;
        MessageBuilder builder = new();

        builder
               .SetMessageType(HostMessageType.SetControlItem)
               .SetCommandType(sdrCommand)
               .AddCommandParameter([(byte)sampleMode])
               .AddCommandParameter([(byte)transferOption])
               .AddCommandParameter([(byte)captureMode])
               .AddCommandParameter([samplesCount ?? 0]);

        var command = builder.BuildCommand();

        await Dispatch(sdrCommand, command, cancellationToken);
    }

    private async Task Dispatch(SdrCommand sdrCommand, byte[] command, CancellationToken cancellationToken)
    {
        await _controlChannel.SendAsync(command, cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            var response = await _controlChannel.ReceiveAsync(cancellationToken).ConfigureAwait(false);
            if (response == null || response.Length == 0)
            {
                throw new IOException("Nothing really returned!");
            }

            if (MessageParser.GetMessageLength(response) == 2)
            {
                throw new SdrNackException("Not supported", sdrCommand);
            }

            var messageType = MessageParser.GetMessageType(response);
            if (messageType == TargetMessageType.ResponseToSetOrRequestCurrentControlItem)
            {
                var data = MessageParser.GetCommandType(response);
                if (data == sdrCommand)
                {
                    break;
                }

                if (data == SdrCommand.Error)
                {
                    var param = MessageParser.GetCommandParameters(response);
                    throw new SdrException("On SetReceiver state error occured!", param[0]);
                }
            }
            else if (messageType == TargetMessageType.UnsolicitedControlItem)
            {
                var param = MessageParser.GetCommandParameters(response);

                UnsolicitedStatusChanged?.Invoke(this, (response.AsSpan(2, 2).ToArray(), param));
            }
            else
            {
                throw new InvalidOperationException($"Invalid response type [ {Enum.GetName(messageType)} ]");
            }
        }
    }

    private async Task<T> Dispatch<T>(SdrCommand sdrCommand, byte[] commandData, Func<byte[], T> responseHandler, CancellationToken cancellationToken)
    {
        await _controlChannel.SendAsync(commandData, cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            var response = await _controlChannel.ReceiveAsync(cancellationToken).ConfigureAwait(false);
            if (response == null || response.Length == 0)
            {
                throw new IOException("Nothing really returned!");
            }

            if (MessageParser.GetMessageLength(response) == 2)
            {
                throw new SdrNackException("Not supported", sdrCommand);
            }

            var messageType = MessageParser.GetMessageType(response);
            if (messageType == TargetMessageType.ResponseToRequestControlItemRange
                || messageType == TargetMessageType.ResponseToSetOrRequestCurrentControlItem)
            {
                var data = MessageParser.GetCommandType(response);
                if (data == sdrCommand)
                {

                    return responseHandler(response);
                }

                if (data == SdrCommand.Error)
                {
                    var param = MessageParser.GetCommandParameters(response);
                    throw new SdrException("On SetReceiver state error occured!", param[0]);
                }
            }
            else if (messageType == TargetMessageType.UnsolicitedControlItem)
            {
                var param = MessageParser.GetCommandParameters(response);

                UnsolicitedStatusChanged?.Invoke(this, (response.AsSpan(2, 2).ToArray(), param));
            }
            else
            {
                throw new InvalidOperationException($"Invalid response type [ {Enum.GetName(messageType)} ]");
            }
        }
        return default;
    }

    private (long min, long max, long vco) ParseFrequencyRange(Memory<byte> data)
    {
        var min = data.Slice(0, 5);
        var max = data.Slice(5, 5);
        var vco = data.Slice(10, 5);

        return (
            BitConverter.ToInt64([.. min.Span, 0, 0, 0]),
            BitConverter.ToInt64([.. max.Span, 0, 0, 0]),
            BitConverter.ToInt64([.. vco.Span, 0, 0, 0])
            );
    }

    private async Task ReceiveData(CancellationToken cancellationToken)
    {
        var data = await _dataChannel.ReceiveAsync(cancellationToken).ConfigureAwait(false);
        var messageType = MessageParser.GetCommandType(data);

        switch ((HostMessageType)messageType)
        {
            case HostMessageType.DataItem0:
                {
                    var dataItem = MessageParser.GetCommandData(data);
                    Data0Received?.Invoke(this, dataItem);
                }
                break;

            case HostMessageType.DataItem1:
                {
                    var dataItem = MessageParser.GetCommandData(data);
                    Data1Received?.Invoke(this, dataItem);
                }
                break;

            case HostMessageType.DataItem2:
                {
                    var dataItem = MessageParser.GetCommandData(data);
                    Data2Received?.Invoke(this, dataItem);
                }
                break;

            case HostMessageType.DataItem3:
                {
                    var dataItem = MessageParser.GetCommandData(data);
                    Data3Received?.Invoke(this, dataItem);
                }
                break;

            default:
                {
                    break;
                }
        }
    }

    private void VerifyConnected() { if (!_isConnected) throw new InvalidOperationException("Not connected to device!"); }

    #region Dispose handling
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _controlChannel.Dispose();
                _dataChannel.Dispose();
                _dataChannelCancelationTokenSource.Dispose();
            }

            _disposed = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    #endregion Dispose handling
}