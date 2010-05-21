using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Combat
{
    public static class Utility
    {
        public static void Log(string message, params object[] args)
        {
            System.Diagnostics.EventLog.WriteEntry("Combat", string.Format(message, args));
        }
    }
}
