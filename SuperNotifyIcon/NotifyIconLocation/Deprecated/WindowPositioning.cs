/**
 * Copyright (c) 2010-2011, David Warner <http://quppa.net/>
 * Copyright (c) 2010-2011, Richard Z.H. Wang <http://zhwang.me/>
 * 
 * This library is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as
 * published by the Free Software Foundation, either version 3 of the
 * License, or (at your option) any later version.
 * 
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public License
 * along with this license.  If not, see <http://www.gnu.org/licenses/>.
 */

// NOTE: Methods that have a non-deprecated equivalent have been removed

namespace Zhwang.SuperNotifyIcon.Finder
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Windows;
    using System.Windows.Forms;

    using Zhwang.SuperNotifyIcon;

    /// <summary>
    /// Helper class for positioning the main window.
    /// </summary>
    public class WindowPositioning
    {
        /// <summary>
        /// Gets the distance from the edge of the screen/taskbar that the window should be drawn from.
        /// This is 8 under Windows 7 with the DWM enabled, and 0 in Windows Vista and in Windows 7 when the DWM is disabled.
        /// </summary>
        public static int WindowEdgeOffset
        {
            get
            {
                // check if the DWM is enabled
                if (Zhwang.SuperNotifyIcon.SysInfo.IsDWMEnabled)
                    if (Zhwang.SuperNotifyIcon.SysInfo.IsWindows7OrLater)
                        return 8; // dwm enabled in 7+ = offset of 8
                    else
                        return 1; // dwm enabled in Vista = offset of 1
                else
                    return 0; // dwm disabled = offset of 0
            }
        }

        /// <summary>
        /// Gets a value indicating whether the notification area is active.
        /// </summary>
        public static bool IsNotificationAreaActive
        {
            get
            {
                IntPtr activewindowhandle = NativeMethods.GetForegroundWindow();

                IntPtr taskbarhandle = NativeMethods.FindWindow("Shell_TrayWnd", string.Empty);

                // Windows 7 notification area fly-out
                IntPtr notificationareaoverflowhandle = NativeMethods.FindWindow("NotifyIconOverflowWindow", string.Empty);

                return (activewindowhandle == taskbarhandle || activewindowhandle == notificationareaoverflowhandle);
            }
        }

        /// <summary>
        /// Returns the optimum window position in relation to the specified notify icon.
        /// </summary>
        /// <param name="notifyicon">The notify icon that the window should be aligned to.</param>
        /// <param name="windowwidth">The width of the window.</param>
        /// <param name="windowheight">The height of the window.</param>
        /// <param name="dpi">The system's DPI (in relative units: 1.0 = 96 DPI, 1.25 = 120 DPI, etc.).</param>
        /// <param name="pinned">Whether the window is pinned open or not. Affects positioning in Windows 7 only.</param>
        /// <returns>A Point specifying the suggested location of the window (top left point).</returns>
        public static Point GetWindowPosition(NotifyIcon notifyicon, double windowwidth, double windowheight, double dpi, bool pinned)
        {
            // retrieve taskbar rect
            Rectangle taskbarRectangle = Taskbar.GetTaskbarRectangle();

            // retrieve notify icon location
            Rectangle nipositiontemp = NotifyIconHelpers.GetNotifyIconRectangle(notifyicon, true);

            // if our functions can't find the rectangle, align it to a corner of the screen
            Rectangle niposition;
            if (nipositiontemp == Rectangle.Empty)
            {
                switch (Taskbar.GetTaskbarEdge())
                {
                    case Taskbar.TaskbarEdge.Bottom: // bottom right corner
                        niposition = new Rectangle(taskbarRectangle.Right - 1, taskbarRectangle.Bottom - 1, 1, 1);
                        break;
                    case Taskbar.TaskbarEdge.Top: // top right corner
                        niposition = new Rectangle(taskbarRectangle.Right - 1, taskbarRectangle.Top, 1, 1);
                        break;
                    case Taskbar.TaskbarEdge.Right: // bottom right corner
                        niposition = new Rectangle(taskbarRectangle.Right - 1, taskbarRectangle.Bottom - 1, 1, 1);
                        break;
                    case Taskbar.TaskbarEdge.Left: // bottom left corner
                        niposition = new Rectangle(taskbarRectangle.Left, taskbarRectangle.Bottom - 1, 1, 1);
                        break;
                    default:
                        goto case Taskbar.TaskbarEdge.Bottom;
                }
            }
            else
                niposition = (Rectangle)nipositiontemp;

            // check if notify icon is in the fly-out
            bool inflyout = NotifyIconHelpers.IsRectangleInFlyOut(niposition);

            // if the window is pinned open and in the fly-out (Windows 7 only),
            // we should position the window above the 'show hidden icons' button
            if (inflyout && pinned)
                niposition = (Rectangle)NotifyArea.GetButtonRectangle();

            // determine centre of notify icon
            Point nicentre = new Point(niposition.Left + (niposition.Width / 2), niposition.Top + (niposition.Height / 2));

            // get window offset from edge
            double edgeoffset = WindowEdgeOffset * dpi;

            // get working area bounds
            Rectangle workarea = GetWorkingArea(niposition);

            // calculate window position
            double windowleft = 0, windowtop = 0;

            switch (Taskbar.GetTaskbarEdge())
            {
                case Taskbar.TaskbarEdge.Bottom:
                    // horizontally centre above icon
                    windowleft = nicentre.X - (windowwidth / 2);
                    if (inflyout)
                        windowtop = niposition.Top - windowheight - edgeoffset;
                    else
                        windowtop = taskbarRectangle.Top - windowheight - edgeoffset;

                    break;

                case Taskbar.TaskbarEdge.Top:
                    // horizontally centre below icon
                    windowleft = nicentre.X - (windowwidth / 2);
                    if (inflyout)
                        windowtop = niposition.Bottom + edgeoffset;
                    else
                        windowtop = taskbarRectangle.Bottom + edgeoffset;

                    break;

                case Taskbar.TaskbarEdge.Left:
                    // vertically centre to the right of icon (or above icon if in flyout and not pinned)
                    if (inflyout && !pinned)
                    {
                        windowleft = nicentre.X - (windowwidth / 2);
                        windowtop = niposition.Top - windowheight - edgeoffset;
                    }
                    else
                    {
                        windowleft = taskbarRectangle.Right + edgeoffset;
                        windowtop = nicentre.Y - (windowheight / 2);
                    }

                    break;

                case Taskbar.TaskbarEdge.Right:
                    // vertically centre to the left of icon (or above icon if in flyout and not pinned)
                    if (inflyout && !pinned)
                    {
                        windowleft = nicentre.X - (windowwidth / 2);
                        windowtop = niposition.Top - windowheight - edgeoffset;
                    }
                    else
                    {
                        windowleft = taskbarRectangle.Left - windowwidth - edgeoffset;
                        windowtop = nicentre.Y - (windowheight / 2);
                    }

                    break;

                default:
                    goto case Taskbar.TaskbarEdge.Bottom; // should be unreachable
            }

            //// check that the window is within the working area
            //// if not, put it next to the closest edge

            if (windowleft + windowwidth + edgeoffset > workarea.Right) // too far right
                windowleft = workarea.Right - windowwidth - edgeoffset;
            else if (windowleft < workarea.Left) // too far left
                windowleft = workarea.Left + edgeoffset;

            if (windowtop + windowheight + edgeoffset > workarea.Bottom) // too far down
                windowtop = workarea.Bottom - windowheight - edgeoffset;
            //// the window should never be too far up, so we can skip checking for that

            return new Point((int)windowleft, (int)windowtop);
        }

        #region Window Size

        /// <summary>
        /// Returns a Rectangle containing the bounds of the specified window's client area (i.e. area excluding border).
        /// </summary>
        /// <param name="hWnd">Handle of the window.</param>
        /// <returns>Rectangle containing window client area bounds.</returns>
        public static Rectangle GetWindowClientAreaSize(IntPtr hWnd)
        {
            NativeMethods.RECT result = new NativeMethods.RECT();
            if (NativeMethods.GetClientRectangle(hWnd, out result))
                return result;
            else
                throw new Exception(String.Format("Could not find client area bounds for specified window (handle {0:X})", hWnd));
        }

        /// <summary>
        /// Returns a Rectangle containing the bounds of the specified window's area (i.e. area excluding border).
        /// </summary>
        /// <param name="hWnd">Handle of the window.</param>
        /// <returns>Rectangle containing window bounds.</returns>
        public static Rectangle GetWindowSize(IntPtr hWnd)
        {
            NativeMethods.RECT result = new NativeMethods.RECT();
            if (NativeMethods.GetWindowRect(hWnd, out result))
                return result;
            else
                throw new Exception(String.Format("Could not find window bounds for specified window (handle {0:X})", hWnd));
        }

        #endregion

        #region Mouse Positioning

        /// <summary>
        /// Returns the cursor's current position as a System.Windows.Point.
        /// </summary>
        /// <returns>Cursor's current position.</returns>
        public static Point GetCursorPosition()
        {
            NativeMethods.POINT result;
            if (NativeMethods.GetCursorPos(out result))
                return result;
            else
                throw new Exception("Failed to retrieve mouse position");
        }

        /// <summary>
        /// Returns true if the cursor is currently over the specified notify icon.
        /// </summary>
        /// <param name="notifyicon">The notify icon to test.</param>
        /// <returns>True if the cursor is over the notify icon, false if not.</returns>
        public static bool IsCursorOverNotifyIcon(NotifyIcon notifyicon)
        {
            return NotifyIconHelpers.IsPointInNotifyIcon(GetCursorPosition(), notifyicon);
        }

        #endregion


        #region Monitor Bounds

        /// <summary>
        /// Returns the working area of the monitor that intersects most with the specified rectangle.
        /// If no monitor can be found, the closest monitor to the rectangle is returned.
        /// </summary>
        /// <param name="rectangle">The rectangle that is located on the monitor whose working area should be returned.</param>
        /// <returns>A rectangle defining the working area of the monitor closest to containing the specified rectangle.</returns>
        public static Rectangle GetWorkingArea(Rectangle rectangle)
        {
            NativeMethods.RECT rect = (NativeMethods.RECT)rectangle;
            IntPtr monitorhandle = NativeMethods.MonitorFromRect(ref rect, NativeMethods.MONITOR_DEFAULTTONEAREST);

            NativeMethods.MONITORINFO monitorinfo = new NativeMethods.MONITORINFO();
            monitorinfo.cbSize = (uint)Marshal.SizeOf(monitorinfo);

            bool result = NativeMethods.GetMonitorInfo(monitorhandle, ref monitorinfo);
            if (!result)
                throw new Exception("Failed to retrieve monitor information.");

            return monitorinfo.rcWork;
        }

        #endregion
    }
}
