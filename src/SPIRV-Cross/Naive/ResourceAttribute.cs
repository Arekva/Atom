using System;

namespace SPIRVCross.Naive
{
    [AttributeUsage(AttributeTargets.Class)]
    sealed class ResourceAttribute : Attribute
    {
        public ResourceType Type { get; }
        public ResourceAttribute(ResourceType type) => Type = type;
    }
}