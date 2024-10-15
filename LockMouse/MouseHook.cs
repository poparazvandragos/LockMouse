using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LockMouse
{
    public class MouseHook : IDisposable
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

        private Screen[] screens;
        private ShellViewModel shellViewModel;

        public delegate int HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("User32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hInstance, int threadId);

        [DllImport("User32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern bool UnhookWindowsHookEx(int idHook);

        [DllImport("User32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int CallNextHookEx(int idHook, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("User32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        //DLLImport for GetLastError
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetLastError();

        static int hHook = 0;
        static HookProc MouseLLProcedure;
        private DateTime lastTime = DateTime.Now;
        #endregion

        public int MouseLLProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if ((nCode >= 0))
            {
                MSLLHOOKSTRUCT pMSLLHOOKSTRUCT = new MSLLHOOKSTRUCT();
                pMSLLHOOKSTRUCT = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, pMSLLHOOKSTRUCT.GetType());
                int wParamInt = wParam.ToInt32();

                if (wParamInt == WM_MOUSEMOVE)
                    if (ShellViewModel.lastMousePosition.X != pMSLLHOOKSTRUCT.pt.X &&
                        ShellViewModel.lastMousePosition.Y != pMSLLHOOKSTRUCT.pt.Y)
                    {
                        MouseMove?.Invoke(this, new MouseMoveEventArgs(pMSLLHOOKSTRUCT.pt));
                        HandleMouseMove(pMSLLHOOKSTRUCT.pt);
                        ShellViewModel.lastMousePosition.X = pMSLLHOOKSTRUCT.pt.X;
                        ShellViewModel.lastMousePosition.Y = pMSLLHOOKSTRUCT.pt.Y;
                    }

                if (wParamInt != WM_MOUSEMOVE && wParamInt != WM_LBUTTONDOWN && wParamInt != WM_LBUTTONUP)
                {
                    HandleMouse(wParamInt, pMSLLHOOKSTRUCT);
                }
            }


            return nCode < 0 ? CallNextHookEx(hHook, nCode, wParam, lParam) : 0;
        }
        private void HandleMouseMove(System.Drawing.Point position)
        {
            var scr = shellViewModel.GetCurrentScreen(position);
            shellViewModel.Screen = scr.DeviceName;
            shellViewModel.PointX = position.X;
            shellViewModel.PointY = position.Y;
            var screenPosition = shellViewModel.GetPositionInScreenSpace(position, scr);
            shellViewModel.ScreenX = screenPosition.X;
            shellViewModel.ScreenY = screenPosition.Y;
        }

        int mouseInputThreads = 0;

        private void HandleMouse(int wParamInt, MSLLHOOKSTRUCT pMSLLHOOKSTRUCT)
        {
            //Trace.WriteLine("Mouse wParamInt: " + wParamInt);
            //Trace.WriteLine("Mouse pMSLLHOOKSTRUCT.pt.X: " + pMSLLHOOKSTRUCT.pt.X);
            //Trace.WriteLine("Mouse pMSLLHOOKSTRUCT.pt.Y: " + pMSLLHOOKSTRUCT.pt.Y);
            //Trace.WriteLine("Mouse pMSLLHOOKSTRUCT.mouseData: " + pMSLLHOOKSTRUCT.mouseData);
            //Trace.WriteLine("Mouse pMSLLHOOKSTRUCT.flags: " + pMSLLHOOKSTRUCT.flags);

            //wParamInt WM_MOUSEWHEEL mouse wheel
            //pMSLLHOOKSTRUCT.mouseData > 0 = wheel up
            //pMSLLHOOKSTRUCT.mouseData < 0 = wheel down

            if (shellViewModel.FlagTranslateMouseWheelToLeftClick)
            {
                if (wParamInt == WM_MOUSEWHEEL)
                {
                    if (pMSLLHOOKSTRUCT.mouseData != 0)
                    {
                        var currentTime = DateTime.Now;
                        if (currentTime - lastTime < TimeSpan.FromMilliseconds(50))
                        {
                            return;
                        }
                        mouseInputThreads++;
                        //Create a new thread to send the mouse click
                        if (mouseInputThreads > 10)
                        {
                            Trace.WriteLine("Too many mouse input threads: " + mouseInputThreads);
                            return;
                        }

                        Thread thread = new Thread(() =>
                        {
                            SendMouseInput(MOUSEEVENTF_LEFTDOWN, pMSLLHOOKSTRUCT);
                            Thread.Sleep(10);
                            SendMouseInput(MOUSEEVENTF_LEFTUP, pMSLLHOOKSTRUCT);

                            //SendMouseClick(pMSLLHOOKSTRUCT);
                            lastTime = currentTime;
                            mouseInputThreads--;
                        });
                        thread.Start();
                    }
                }
            }

            //wParamInt WM_XBUTTONDOWN / WM_XBUTTONUP
            //pMSLLHOOKSTRUCT.mouseData 131072 = side top button
            //pMSLLHOOKSTRUCT.mouseData 65536 = side bottom button
        }

        //define INPUT struct
        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT
        {
            public int type;
            public MOUSEINPUT mi;
        }
        //define MOUSEINPUT struct
        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public int mouseData;
            public int dwFlags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        //define INPUT type
        const int INPUT_MOUSE = 0;
        const int INPUT_KEYBOARD = 1;
        const int INPUT_HARDWARE = 2;

        //define mouse event type
        const int MOUSEEVENTF_LEFTDOWN = 0x02;
        const int MOUSEEVENTF_LEFTUP = 0x04;

        //import SendInput function
        [DllImport("user32.dll", SetLastError = true)]
        static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        internal static void SendMouseInput(int param, MSLLHOOKSTRUCT pMSLLHOOKSTRUCT)
        {
            //send mouse input to system
            //var lParam = MAKELPARAM(pMSLLHOOKSTRUCT.pt.X, pMSLLHOOKSTRUCT.pt.Y);
            //Trace.WriteLine("SendMouseEvent: " + param + " " + lParam); 
            //var result = SendMessage(IntPtr.Zero, param, 0, lParam);

            //use SendInput instead of SendMessage
            INPUT[] inputs = new INPUT[1];
            inputs[0].type = INPUT_MOUSE;
            inputs[0].mi.dx = pMSLLHOOKSTRUCT.pt.X;
            inputs[0].mi.dy = pMSLLHOOKSTRUCT.pt.Y;
            inputs[0].mi.dwFlags = param;

            var result = SendInput(1, inputs, Marshal.SizeOf(inputs[0]));

            if (result == 0)
            {
                Trace.WriteLine("SendMouseEvent error: " + GetLastError());
            }
        }

        internal static void SendMouseClick(MSLLHOOKSTRUCT pMSLLHOOKSTRUCT)
        {
            //send mouse input to system
            //var lParam = MAKELPARAM(pMSLLHOOKSTRUCT.pt.X, pMSLLHOOKSTRUCT.pt.Y);
            //Trace.WriteLine("SendMouseEvent: " + param + " " + lParam); 
            //var result = SendMessage(IntPtr.Zero, param, 0, lParam);

            //use SendInput instead of SendMessage
            INPUT[] inputs = new INPUT[2];
            inputs[0].type = INPUT_MOUSE;
            inputs[0].mi.dx = pMSLLHOOKSTRUCT.pt.X;
            inputs[0].mi.dy = pMSLLHOOKSTRUCT.pt.Y;
            inputs[0].mi.dwFlags = MOUSEEVENTF_LEFTDOWN;

            inputs[1].type = INPUT_MOUSE;
            inputs[1].mi.dx = pMSLLHOOKSTRUCT.pt.X;
            inputs[1].mi.dy = pMSLLHOOKSTRUCT.pt.Y;
            inputs[1].mi.dwFlags = MOUSEEVENTF_LEFTDOWN;

            var result = SendInput(2, inputs, Marshal.SizeOf(inputs[0]));

            if (result == 0)
            {
                Trace.WriteLine("SendMouseClick error: " + GetLastError());
            }
            else
            {
                Trace.WriteLine("SendMouseClick result: " + result);
            }
        }

        private static int MAKELPARAM(int x, int y)
        {
            return ((y << 16) | (x & 0xFFFF));
        }

        public static event EventHandler<MouseMoveEventArgs> MouseMove;

        public MouseHook(ShellViewModel shellViewModel, Screen[] screens)
        {
            this.screens = screens;
            this.shellViewModel = shellViewModel;
            MouseLLProcedure = new HookProc(MouseLLProc);
            hHook = SetWindowsHookEx(WH_MOUSE_LL, MouseLLProcedure, (IntPtr)0, 0);
        }

        public void Dispose()
        {
            //unhook the mouse hook
            UnhookWindowsHookEx(hHook);
            Trace.WriteLine("MouseHook disposed");
        }
    }
}
