using Communication.Sample.Client.Enums;

namespace Communication.Sample.Client.Exceptions
{
    [Serializable]
    internal class SdrNackException : Exception
    {
        private string v;
        private SdrCommand sdrCommand;

        public SdrNackException()
        {
        }

        public SdrNackException(string? message) : base(message)
        {
        }

        public SdrNackException(string v, SdrCommand sdrCommand)
        {
            this.v = v;
            this.sdrCommand = sdrCommand;
        }

        public SdrNackException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}