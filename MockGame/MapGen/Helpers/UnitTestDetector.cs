// Copyright 2023 Treer (https://github.com/Treer)
// License: MIT, see LICENSE.txt for rights granted

using System;
using System.Reflection;

namespace MapGen.Helpers
{
    /// <summary>
    /// Detect if we are running as part of an xUnit or nUnit unittest.
    /// </summary>    
    static class UnitTestDetector
    {
        public static bool IsRunningAsUnitTest { get; private set; } = false;

        static UnitTestDetector() {
            foreach (Assembly assem in AppDomain.CurrentDomain.GetAssemblies()) {
                // Can't do something like this as it will load the nUnit assembly
                // if (assem == typeof(NUnit.Framework.Assert))

                var assemName = assem.FullName?.ToLowerInvariant() ?? "null";

                if (assemName.StartsWith("xunit.runner") || assemName.StartsWith("nunit.framework")) {
                    IsRunningAsUnitTest = true;
                    break;
                }
            }
        }
    }
}
