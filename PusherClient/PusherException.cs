using System;

namespace PusherClient
{
    public class PusherException : Exception
    {
        public ErrorCodes PusherCode { get; set; }

        public PusherException(string message, ErrorCodes code)
            : base(message)
        {
            PusherCode = code;
        }

    }
}
