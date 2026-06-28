using System;
using System.Windows.Forms;

namespace CT.Epladdin.PPReportHelper
{
    internal sealed class WindowWrapper : IWin32Window
    {
        public IntPtr Handle { get; private set; }

        internal WindowWrapper(IntPtr handle)
        {
            Handle = handle;
        }
    }
}