namespace Communication.Sample.Client.Enums;

public enum HostMessageType : byte
{
    SetControlItem = 0b000,       // 000
    RequestCurrentControlItem = 0b001,  // 001
    RequestControlItemRange = 0b010,    // 010
    DataItemAck = 0b011,         // 011
    DataItem0 = 0b100,           // 100
    DataItem1 = 0b101,           // 101
    DataItem2 = 0b110,           // 110
    DataItem3 = 0b111            // 111
}
