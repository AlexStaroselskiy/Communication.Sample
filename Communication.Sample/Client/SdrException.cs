
namespace Communication.Sample.Client
{
    [Serializable]
    public class SdrException : Exception
    {

        public SdrException()
        {
        }

        public SdrException(string? message, byte code) : base(message)
        {
            Code = code;
        }

        public SdrException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
        public byte Code { get; set; }
    }
}