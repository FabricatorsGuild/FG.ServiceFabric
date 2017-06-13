using System;
using System.Diagnostics;

namespace FG.ServiceFabric.Diagnostics.Tracing
{
    public class DebugOutputEventListener : OutputEventListener
    {
        protected override void WriteLine(string line)
        {
            Debug.WriteLine(line);
        }

        protected override ConsoleColor Color { get; set; }
    }
}