using System;

namespace SPIRVCross.Naive
{
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class ResultAttribute : Attribute
    {
        public Result Result { get; }
        public ResultAttribute(Result result) => Result = result;
    }
}