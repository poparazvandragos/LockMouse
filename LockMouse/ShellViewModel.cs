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
using static LockMouse.MouseHook;

namespace LockMouse
{
    public class ShellViewModel : Caliburn.Micro.PropertyChangedBase, IShell
    {
        internal static string MOUSE_NOLOCK = "No Lock";
        public static System.Drawing.Point lastMousePosition;

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

        private bool flagTranslateMouseWheelToLeftClick = false;
        public bool FlagTranslateMouseWheelToLeftClick
        {
            get => flagTranslateMouseWheelToLeftClick;
            set
            {
                flagTranslateMouseWheelToLeftClick = value;
                NotifyOfPropertyChange(() => FlagTranslateMouseWheelToLeftClick);
            }
        }
        #endregion

        System.Timers.Timer timer = new System.Timers.Timer();
        System.Windows.Forms.Screen[] screens = System.Windows.Forms.Screen.AllScreens;

        MouseHook mouseHook;

        public ShellViewModel()
        {
            HotkeyManager.Current.AddOrReplace("LockToWindow", Key.Multiply, ModifierKeys.Control | ModifierKeys.Alt, LockToWindow);
            HotkeyManager.Current.AddOrReplace("LockToScreen", Key.Add, ModifierKeys.Control | ModifierKeys.Alt, LockToScreen);
            HotkeyManager.Current.AddOrReplace("NoLock", Key.Subtract, ModifierKeys.Control | ModifierKeys.Alt, NoLock);

            HotkeyManager.Current.AddOrReplace("TranslateMouseWheelToLeftClick", Key.NumPad8, ModifierKeys.Control | ModifierKeys.Alt, TranslateMouseWheelToLeftClick);

            LockName = MOUSE_NOLOCK;
            mouseHook = new MouseHook(this, screens);
        }

        private void TranslateMouseWheelToLeftClick(object sender, HotkeyEventArgs e)
        {
            FlagTranslateMouseWheelToLeftClick = !FlagTranslateMouseWheelToLeftClick;
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

        internal Point GetPositionInScreenSpace(Point position, System.Windows.Forms.Screen screen)
        {
            return new Point(position.X - screen.Bounds.Left, position.Y - screen.Bounds.Top);
        }

        internal System.Windows.Forms.Screen GetCurrentScreen(Point position)
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
            mouseHook.Dispose();
            timer.Stop();
        }
    }
}