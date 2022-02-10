using System;
using System.Runtime.Serialization;

namespace SPIRVCross
{
    [Serializable]
    public class ResourceException : Exception
    {
        public ResourceException(string message) : base(message) { }
    }
}