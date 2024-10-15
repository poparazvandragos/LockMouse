using NHotkey;
using NHotkey.Wpf;
using System;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Channels;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Drawing;
using System.Runtime.CompilerServices;

namespace LockMouse
{
    public class ShellViewModel : Caliburn.Micro.PropertyChangedBase, IShell
    {
        internal static string MOUSE_NOLOCK = "No Lock";

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

        static int hHook = 0;
        static HookProc MouseLLProcedure;
        #endregion

        public int MouseLLProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if ((nCode >= 0))
            {
                MSLLHOOKSTRUCT pMSLLHOOKSTRUCT = new MSLLHOOKSTRUCT();
                pMSLLHOOKSTRUCT = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, pMSLLHOOKSTRUCT.GetType());
                int wParamInt = wParam.ToInt32();

                if (wParamInt == MouseHook.WM_MOUSEMOVE)
                    if (lastMousePosition.X != pMSLLHOOKSTRUCT.pt.X &&
                        lastMousePosition.Y != pMSLLHOOKSTRUCT.pt.Y)
                    {
                        HandleMouseMove(pMSLLHOOKSTRUCT.pt);
                        lastMousePosition.X = pMSLLHOOKSTRUCT.pt.X;
                        lastMousePosition.Y = pMSLLHOOKSTRUCT.pt.Y;
                    }

                if (wParamInt != MouseHook.WM_MOUSEMOVE)
                {
                    HandleMouse(wParamInt, pMSLLHOOKSTRUCT);
                }
            }


            return nCode < 0 ? CallNextHookEx(hHook, nCode, wParam, lParam) : 0;
        }

        private void HandleMouse(int wParamInt, MSLLHOOKSTRUCT pMSLLHOOKSTRUCT)
        {
            Trace.WriteLine("Mouse wParamInt: " + wParamInt);
            Trace.WriteLine("Mouse pMSLLHOOKSTRUCT.pt.X: " + pMSLLHOOKSTRUCT.pt.X);
            Trace.WriteLine("Mouse pMSLLHOOKSTRUCT.pt.Y: " + pMSLLHOOKSTRUCT.pt.Y);
            Trace.WriteLine("Mouse pMSLLHOOKSTRUCT.mouseData: " + pMSLLHOOKSTRUCT.mouseData);
            Trace.WriteLine("Mouse pMSLLHOOKSTRUCT.flags: " + pMSLLHOOKSTRUCT.flags);

            //wParamInt WM_MOUSEWHEEL mouse wheel
            //pMSLLHOOKSTRUCT.mouseData > 0 = wheel up
            //pMSLLHOOKSTRUCT.mouseData < 0 = wheel down

            if (FlagTranslateMouseWheelToLeftClick)
            { 
                if (wParamInt == MouseHook.WM_MOUSEWHEEL)
                {
                    if (pMSLLHOOKSTRUCT.mouseData != 0)
                    {
                        MouseHook.SendMouseEvent(MouseHook.WM_LBUTTONDOWN);
                        MouseHook.SendMouseEvent(MouseHook.WM_LBUTTONUP);
                    }
                }
            }

            //wParamInt WM_XBUTTONDOWN / WM_XBUTTONUP
            //pMSLLHOOKSTRUCT.mouseData 131072 = side top button
            //pMSLLHOOKSTRUCT.mouseData 65536 = side bottom button
        }

        #region UI Props
        private string screen;
        public string Screen
        {
            get { return screen; }
            set
            {
                screen = value;
                NotifyOfPropertyChange(() => Screen);
            }
        }

        private double pointX;
        public double PointX
        {
            get { return pointX; }
            set
            {
                pointX = value; NotifyOfPropertyChange(() => PointX);
            }
        }

        private double pointY;
        public double PointY
        {
            get { return pointY; }
            set
            {
                pointY = value; NotifyOfPropertyChange(() => PointY);
            }
        }

        private double screenX;
        public double ScreenX
        {
            get { return screenX; }
            set
            {
                screenX = value; NotifyOfPropertyChange(() => ScreenX);
            }
        }

        private double screenY;
        public double ScreenY
        {
            get { return screenY; }
            set
            {
                screenY = value; NotifyOfPropertyChange(() => ScreenY);
            }
        }

        private string lockName;
        public string LockName
        {
            get { return lockName; }
            private set
            {
                lockName = value; NotifyOfPropertyChange(() => LockName);
            }
        }
        #endregion

        #region AppState
        private Screen lockScreen;
        private Screen LockScreen
        {
            get { return lockScreen; }
            set
            {
                lockScreen = value;
                NotifyOfPropertyChange(() => LockScreen);
            }
        }
        private static EnumLockState mouselock = EnumLockState.NoLock;

        private bool FlagTranslateMouseWheelToLeftClick = false;
        #endregion

        System.Timers.Timer timer = new System.Timers.Timer();
        System.Windows.Forms.Screen[] screens = System.Windows.Forms.Screen.AllScreens;

        MouseHook hook = new MouseHook();

        public ShellViewModel()
        {

            HotkeyManager.Current.AddOrReplace("LockToWindow", Key.Multiply, ModifierKeys.Control | ModifierKeys.Alt, LockToWindow);
            HotkeyManager.Current.AddOrReplace("LockToScreen", Key.Add, ModifierKeys.Control | ModifierKeys.Alt, LockToScreen);
            HotkeyManager.Current.AddOrReplace("NoLock", Key.Subtract, ModifierKeys.Control | ModifierKeys.Alt, NoLock);

            HotkeyManager.Current.AddOrReplace("TranslateMouseWheelToLeftClick", Key.Subtract, ModifierKeys.Control | ModifierKeys.Alt, TranslateMouseWheelToLeftClick);

            LockName = MOUSE_NOLOCK;

            lastMousePosition = new Point();
            MouseLLProcedure = new HookProc(MouseLLProc);
            hHook = SetWindowsHookEx(MouseHook.WH_MOUSE_LL, MouseLLProcedure, (IntPtr)0, 0);
        }

        private void TranslateMouseWheelToLeftClick(object sender, HotkeyEventArgs e)
        {
            FlagTranslateMouseWheelToLeftClick = !FlagTranslateMouseWheelToLeftClick;
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var position = MouseUtils.GetCursorPosition();
            HandleMouseMove(position);
        }

        private void HandleMouseMove(System.Drawing.Point position)
        {
            var scr = GetCurrentScreen(position);
            Screen = scr.DeviceName;
            PointX = position.X;
            PointY = position.Y;
            var screenPosition = GetPositionInScreenSpace(position, scr);
            ScreenX = screenPosition.X;
            ScreenY = screenPosition.Y;
        }

        private void LockToWindow(object sender, HotkeyEventArgs ev)
        {
            var position = MouseUtils.GetCursorPosition();
            LockScreen = GetCurrentScreen(position);
            if (LockScreen != null)
                LockName = LockScreen.DeviceName;
            else
                LockName = MOUSE_NOLOCK;

            MouseUtils.ClipCursorToRect(LockScreen.Bounds);
            mouselock = EnumLockState.LockToWindow;
        }

        private void LockToScreen(object sender, HotkeyEventArgs ev)
        {
            var position = MouseUtils.GetCursorPosition();
            LockScreen = GetCurrentScreen(position);
            if (LockScreen != null)
                LockName = LockScreen.DeviceName;
            else
                LockName = MOUSE_NOLOCK;
            NotifyOfPropertyChange(() => LockName);

            MouseUtils.ClipCursorToRect(LockScreen.Bounds);

            mouselock = EnumLockState.LockToScreen;
        }

        private void NoLock(object sender, HotkeyEventArgs ev)
        {
            LockScreen = null;
            mouselock = EnumLockState.NoLock;
            LockName = MOUSE_NOLOCK;
            NotifyOfPropertyChange(() => LockName);
            MouseUtils.ClipCursorToRect(null);
        }

        private Point GetPositionInScreenSpace(Point position, System.Windows.Forms.Screen screen)
        {
            return new Point(position.X - screen.Bounds.Left, position.Y - screen.Bounds.Top);
        }

        private System.Windows.Forms.Screen GetCurrentScreen(Point position)
        {
            foreach (var screen in screens)
            {
                if (screen.Bounds.Top <= position.Y &&
                    screen.Bounds.Bottom >= position.Y &&
                    screen.Bounds.Left <= position.X &&
                    screen.Bounds.Right >= position.X)
                {
                    return screen;
                }
            }
            return screens[0];
        }

        public void OnClose(CancelEventArgs ev)
        {
            timer.Stop();
        }
    }
}