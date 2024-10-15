using System.Drawing;

namespace LockMouse
{
    public class MouseMoveEventArgs
    {
        private Point pt;

        public MouseMoveEventArgs(Point pt)
        {
            this.pt = pt;
        }
    }
}