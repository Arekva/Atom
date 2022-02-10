using System;
using System.Runtime.Serialization;

namespace SPIRVCross
{
    [Serializable]
    public class EntryPointException : Exception
    {
        public EntryPointException(string message) : base(message) { }
    }
}