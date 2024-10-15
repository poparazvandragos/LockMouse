using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LockMouse
{
    public class MouseHook
    {

        #region Constants
        public const int WH_MIN = (-1);
        public const int WH_MSGFILTER = (-1);
        public const int WH_JOURNALRECORD = 0;
        public const int WH_JOURNALPLAYBACK = 1;
        public const int WH_KEYBOARD = 2;
        public const int WH_GETMESSAGE = 3;
        public const int WH_CALLWNDPROC = 4;
        public const int WH_CBT = 5;
        public const int WH_SYSMSGFILTER = 6;
        public const int WH_MOUSE = 7;
        public const int WH_HARDWARE = 8;
        public const int WH_DEBUG = 9;
        public const int WH_SHELL = 10;
        public const int WH_FOREGROUNDIDLE = 11;
        public const int WH_CALLWNDPROCRET = 12;
        public const int WH_KEYBOARD_LL = 13;
        public const int WH_MOUSE_LL = 14;
        public const int WH_MAX = 14;
        public const int WH_MINHOOK = WH_MIN;
        public const int WH_MAXHOOK = WH_MAX;

        public const int HC_ACTION = 0;


        public const int WM_MOUSEMOVE = 0x0200;     //512

        public const int WM_LBUTTONDOWN = 0x0201;   //513
        public const int WM_LBUTTONUP = 0x0202;     //514

        public const int WM_RBUTTONDOWN = 0x0204;   //516
        public const int WM_RBUTTONUP = 0x0205;     //517

        public const int WM_MBUTTONDOWN = 0x0207;   //519
        public const int WM_MBUTTONUP = 0x0208;     //520

        public const int WM_MOUSEWHEEL = 0x020A;    //522

        public const int WM_XBUTTONDOWN = 0x020B;   //523
        public const int WM_XBUTTONUP = 0x020C;     //524

        public const int WM_MOUSEHWHEEL = 0x020E;   //526
        #endregion

        #region Mouse Hook
        [StructLayoutAttribute(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct MSLLHOOKSTRUCT
        {
            public System.Drawing.Point pt;
            public int mouseData;
            public int flags;
            public int time;
            public uint dwExtraInfo;
        }

        public static System.Drawing.Point lastMousePosition;

        public delegate int HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("User32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hInstance, int threadId);

        [DllImport("User32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern bool UnhookWindowsHookEx(int idHook);

        [DllImport("User32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int CallNextHookEx(int idHook, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("User32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        static int hHook = 0;
        static HookProc MouseLLProcedure;
        #endregion

        public static int MouseLLProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if ((nCode >= 0))
            {
                MSLLHOOKSTRUCT pMSLLHOOKSTRUCT = new MSLLHOOKSTRUCT();
                pMSLLHOOKSTRUCT = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, pMSLLHOOKSTRUCT.GetType());
                if (lastMousePosition.X != pMSLLHOOKSTRUCT.pt.X &&
                    lastMousePosition.Y != pMSLLHOOKSTRUCT.pt.Y)
                {
                    MouseMove?.Invoke(null, pMSLLHOOKSTRUCT.pt);
                    //ShellViewModel.LockMouse(pMSLLHOOKSTRUCT.pt);
                    lastMousePosition.X = pMSLLHOOKSTRUCT.pt.X;
                    lastMousePosition.Y = pMSLLHOOKSTRUCT.pt.Y;
                }
            }
            return nCode < 0 ? CallNextHookEx(hHook, nCode, wParam, lParam) : 0;
        }

        internal static void SendMouseEvent(int param, MSLLHOOKSTRUCT pMSLLHOOKSTRUCT)
        {
            //send mouse input to system
            SendMessage(IntPtr.Zero, param, 0, MAKELPARAM(pMSLLHOOKSTRUCT.pt.X, pMSLLHOOKSTRUCT.pt.Y));

        }

        private static int MAKELPARAM(int x, int y)
        {
            return ((y << 16) | (x & 0xFFFF));
        }

        public static event EventHandler<Point> MouseMove;

        public MouseHook()
        {
            lastMousePosition = new Point();
            MouseLLProcedure = new HookProc(MouseLLProc);
            hHook = SetWindowsHookEx(WH_MOUSE_LL, MouseLLProcedure, (IntPtr)0, 0);

        }
    }
}
