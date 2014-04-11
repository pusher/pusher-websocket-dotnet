using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
