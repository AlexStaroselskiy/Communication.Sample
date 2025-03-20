namespace Communication.Sample.Client.Enums;

public enum SdrCommand: short
{
    StateCommand = 0x0018,
    SetReceiverChannel = 0x0019,
    SetReceiverFrequency = 0x0020,
    Error = 0x0005
}
