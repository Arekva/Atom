using System;
using System.Runtime.Serialization;

namespace SPIRVCross.Naive
{
    [Serializable]
    internal abstract class SpvcException : Exception
    {
        public Result Result { get; }
        
        public SpvcException() { }
        public SpvcException(string message, Result result) : base(message) => Result = result;
        public SpvcException(string message, Result result, Exception inner) : base(message, inner) => Result = result;
        protected SpvcException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}