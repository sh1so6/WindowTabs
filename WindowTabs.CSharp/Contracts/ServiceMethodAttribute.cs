using System;

namespace WindowTabs.CSharp.Contracts
{
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class ServiceMethodAttribute : Attribute
    {
        public bool Async { get; set; }
    }
}
