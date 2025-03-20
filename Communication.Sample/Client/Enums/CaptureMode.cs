namespace Communication.Sample.Client.Enums;

public enum CaptureMode
{
    None = 0,

    /// <summary>
    /// 16 bit Contiguous mode where the data is contiguously sent back to the Host.
    /// </summary>
    Contiguos16 = 0x00,

    /// <summary>
    /// 24 bit Contiguous mode where the data is contiguously sent back to the Host.
    /// </summary>
    Contiguos24 = 0x80,

    /// <summary>
    /// 16 bit FIFO mode where N samples of data is captured in a FIFO then sent back to the Host.
    /// </summary>
    Fifo16 = 0x01,

    /// <summary>
    /// 24 bit FIFO mode where N samples of data is captured in a FIFO then sent back to the Host.
    /// </summary>
    HwPulse24 = 0x83,

    /// <summary>
    /// 16 bit Hardware Triggered Pulse mode.(start/stop controlled by HW trigger input)(**Optional)
    /// </summary>
    HwPulse16 = 0x03
}