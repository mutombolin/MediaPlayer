using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace MediaPlayer
{
    public sealed class HookWindows
    {
        [DllImport("user32.dll", EntryPoint = "FindWindowEx", CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowTextLength(HandleRef hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowText(HandleRef hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        public static readonly HookWindows Instance = new HookWindows();

        private static int GWL_STYLE = -16;
        private static int WS_CHILD = 0x40000000;

        public HookWindows()
        { }

        private List<IntPtr> GetAllChildrenWindowsHandles(IntPtr hParent, int maxCount)
        {
            List<IntPtr> result = new List<IntPtr>();
            int ct = 0;
            IntPtr prevChild = IntPtr.Zero;
            IntPtr currChild = IntPtr.Zero;

            result.Add(hParent);

            while (true && ct < maxCount)
            {
                currChild = FindWindowEx(hParent, prevChild, null, null);
                
                if (currChild == IntPtr.Zero)
                    break;

                result.Add(currChild);
                prevChild = currChild;
                ++ct;
            }

            return result;
        }

        public IntPtr GetHostHandle(string process, string windowTitle)
        {
            IntPtr result = IntPtr.Zero;

            if (process == string.Empty)
                return result;

            Process hostProcess = Process.GetProcessesByName(process).FirstOrDefault();

            if (hostProcess != null)
            {
                IntPtr hostHandle = FindWindow(null, windowTitle);
                List<IntPtr> children = GetAllChildrenWindowsHandles(hostHandle, 10);

                for (int i = 0; i < children.Count; i++)
                {
                    int capacity = GetWindowTextLength(new HandleRef(this, children[i])) * 2;
                    StringBuilder sb = new StringBuilder(capacity);
                    GetWindowText(new HandleRef(this, children[i]), sb, sb.Capacity);
                    if (string.Compare(sb.ToString(), windowTitle, true) == 0)
                    {
                        result = children[i];
                        break;
                    }
                }
            }

            return result;
        }

        public IntPtr HookMainWindow(IntPtr hostHandle, IntPtr guestHandle)
        {
            IntPtr result = IntPtr.Zero;

            if (hostHandle != IntPtr.Zero)
            {
                SetWindowLong(guestHandle, GWL_STYLE, GetWindowLong(guestHandle, GWL_STYLE) | WS_CHILD);
                result = SetParent(guestHandle, hostHandle);
            }

            return result;
        }
    }
}
