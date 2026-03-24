using System;
using WindowTabs.CSharp.Contracts;

namespace WindowTabs.CSharp.Services
{
    internal sealed class ManagedWindowGroupRuntimeFactory
    {
        public IWindowGroupRuntime Create(IntPtr groupHandle)
        {
            return new ManagedWindowGroupRuntime(groupHandle);
        }
    }
}
