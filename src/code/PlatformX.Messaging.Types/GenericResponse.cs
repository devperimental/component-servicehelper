using System;
using System.Collections.Generic;
using System.Text;

namespace PlatformX.Messaging.Types
{
    public class GenericResponse
    {
        public GenericResponse() { }
        public string MessageId { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
        public string FailureReason { get; set; }
}
}
