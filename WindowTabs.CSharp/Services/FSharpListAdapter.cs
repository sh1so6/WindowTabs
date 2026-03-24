using System;
using System.Collections.Generic;
using Bemo;
using Microsoft.FSharp.Core;

namespace WindowTabs.CSharp.Services
{
    internal static class FSharpListAdapter
    {
        public static List2<T> ToList2<T>(IEnumerable<T> items)
        {
            return new List2<T>(FSharpOption<IEnumerable<T>>.Some(items ?? Array.Empty<T>()));
        }
    }
}
