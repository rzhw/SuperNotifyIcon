/**
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

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;

namespace Zhwang.SuperNotifyIcon
{
    // http://www.crowsprogramming.com/archives/88 used as a reference

    internal partial class Taskbar
    {
        /// <summary>
        /// Represents the edge of the screen the taskbar is docked to.
        /// </summary>
        public enum TaskbarEdge
        {
            Left = NativeMethods.ABEdge.ABE_LEFT,
            Top = NativeMethods.ABEdge.ABE_TOP,
            Right = NativeMethods.ABEdge.ABE_RIGHT,
            Bottom = NativeMethods.ABEdge.ABE_BOTTOM
        }

        /// <summary>
        /// The states the taskbar can be in.
        /// </summary>
        [Flags]
        public enum TaskbarState
        {
            /// <summary>
            /// No autohide, not always top
            /// </summary>
            None = NativeMethods.ABState.ABS_MANUAL,

            /// <summary>
            /// Hides task bar when mouse exits task bar region
            /// </summary>
            AutoHide = NativeMethods.ABState.ABS_AUTOHIDE,

            /// <summary>
            /// Taskbar is always on top of other windows
            /// </summary>
            AlwaysTop = NativeMethods.ABState.ABS_ALWAYSONTOP
        }

        /// <summary>
        /// Gets the rectangle of the taskbar.
        /// </summary>
        /// <returns>The taskbar rectangle.</returns>
        public static Rectangle GetTaskbarRectangle()
        {
            var appBar = GetTaskBarData();
            return appBar.rc;
        }

        /// <summary>
        /// Gets the location, in screen coordinates of the taskbar.
        /// </summary>
        /// <returns>The taskbar location.</returns>
        public static Point GetTaskbarLocation()
        {
            return GetTaskbarRectangle().Location;
        }

        /// <summary>
        /// Gets the size, in pixels of the taskbar.
        /// </summary>
        /// <returns>The taskbar size.</returns>
        public static Size GetTaskbarSize()
        {
            return GetTaskbarRectangle().Size;
        }

        /// <summary>
        /// Gets the edge of the screen that the taskbar is docked to.
        /// </summary>
        /// <returns></returns>
        public static TaskbarEdge GetTaskbarEdge()
        {
            var appBar = GetTaskBarData();
            return (TaskbarEdge)appBar.uEdge;
        }

        /// <summary>
        /// Gets the current state of the taskbar.
        /// </summary>
        /// <returns></returns>
        public static TaskbarState GetTaskbarState()
        {
            var appBar = CreateAppBarData();
            return (TaskbarState)NativeMethods.SHAppBarMessage(NativeMethods.ABMsg.ABM_GETSTATE, ref appBar);
        }

        /// <summary>
        /// Sets the state of the taskbar.
        /// </summary>
        /// <param name="state">The new state.</param>
        public static void SetTaskBarState(TaskbarState state)
        {
            var appBar = CreateAppBarData();
            appBar.lParam = (IntPtr)state;
            NativeMethods.SHAppBarMessage(NativeMethods.ABMsg.ABM_SETSTATE, ref appBar);
        }

        /// <summary>
        /// Gets an APPBARDATA struct with valid location, size, and edge of the taskbar.
        /// </summary>
        /// <returns></returns>
        private static NativeMethods.APPBARDATA GetTaskBarData()
        {
            var appBar = CreateAppBarData();
            System.IntPtr ret = NativeMethods.SHAppBarMessage(NativeMethods.ABMsg.ABM_GETTASKBARPOS, ref appBar);
            return appBar;
        }

        /// <summary>
        /// Creats an APPBARDATA struct with its hWnd member set to the task bar window.
        /// </summary>
        /// <returns></returns>
        private static NativeMethods.APPBARDATA CreateAppBarData()
        {
            var appBar = new NativeMethods.APPBARDATA();
            appBar.hWnd = NativeMethods.FindWindow("Shell_TrayWnd", "");
            appBar.cbSize = (uint)Marshal.SizeOf(appBar);
            return appBar;
        }

    }
}
