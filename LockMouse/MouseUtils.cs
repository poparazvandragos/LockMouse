using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Drawing; // Or use whatever point class you like for the implicit cast operator

namespace LockMouse
{
    /// <summary>
    /// Struct representing a point.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;

        public static implicit operator Point(POINT point)
        {
            return new Point(point.X, point.Y);
        }

        public POINT(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    public struct RECT
    {
        #region Variables.
        /// <summary>
        /// Left position of the rectangle.
        /// </summary>
        public int Left;
        /// <summary>
        /// Top position of the rectangle.
        /// </summary>
        public int Top;
        /// <summary>
        /// Right position of the rectangle.
        /// </summary>
        public int Right;
        /// <summary>
        /// Bottom position of the rectangle.
        /// </summary>
        public int Bottom;
        #endregion

        #region Operators.
        /// <summary>
        /// Operator to convert a RECT to Drawing.Rectangle.
        /// </summary>
        /// <param name="rect">Rectangle to convert.</param>
        /// <returns>A Drawing.Rectangle</returns>
        public static implicit operator Rectangle(RECT rect)
        {
            return Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
        }

        /// <summary>
        /// Operator to convert Drawing.Rectangle to a RECT.
        /// </summary>
        /// <param name="rect">Rectangle to convert.</param>
        /// <returns>RECT rectangle.</returns>
        public static implicit operator RECT(Rectangle rect)
        {
            return new RECT(rect.Left, rect.Top, rect.Right, rect.Bottom);
        }
        #endregion

        #region Constructor.
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="left">Horizontal position.</param>
        /// <param name="top">Vertical position.</param>
        /// <param name="right">Right most side.</param>
        /// <param name="bottom">Bottom most side.</param>
        public RECT(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }
        #endregion
    }

    class MouseUtils
    {
        /// <summary>
        /// Retrieves the cursor's position, in screen coordinates.
        /// </summary>
        /// <see>See MSDN documentation for further information.</see>
        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("User32.Dll")]
        public static extern long SetCursorPos(int x, int y);

        [DllImport("User32.Dll")]
        public static extern bool ClientToScreen(IntPtr hWnd, ref POINT point);

        [DllImport("user32.dll", EntryPoint = "ClipCursor")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ClipCursor([In] IntPtr lpRect);

        public static Point GetCursorPosition()
        {
            POINT lpPoint;
            //GetCursorPos(out lpPoint);
            // NOTE: If you need error handling
            bool success = GetCursorPos(out lpPoint);
            if (success)
                return lpPoint;
            else
                return new POINT()
                {
                    X = -1,
                    Y = -1
                };
        }

        public static void SetMousePosition(Point point)
        {
            POINT p = new POINT((int)point.X, (int)point.Y);
            //ClientToScreen(this.Handle, ref p);
            SetCursorPos(p.X, p.Y);
        }

        public static void ClipCursorToRect(System.Drawing.Rectangle? rect)
        {

            if (rect != null && rect.Value != null)
            {
                var rectangle = new RECT(rect.Value.Left, rect.Value.Top, rect.Value.Right, rect.Value.Bottom);
                var p = Marshal.AllocHGlobal(Marshal.SizeOf(rectangle));
                Marshal.StructureToPtr(rect, p, false);
                ClipCursor(p);
            }
            else
                ClipCursor(IntPtr.Zero);
        }
    }
}
