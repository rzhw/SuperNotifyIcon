/**
 * Copyright (c) 2010-2011, Richard Z.H. Wang <http://zhwang.me/>
 * Copyright (c) 2010-2011, David Warner <http://quppa.net/>
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
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Reflection;

namespace Zhwang.SuperNotifyIcon
{
    public static class NotifyIconHelpers
    {
        public static Rectangle GetNotifyIconRectangle(NotifyIcon notifyIcon, bool returnIfHidden)
        {
            Rectangle? rect;

            if (SysInfo.IsWindows7OrLater)
                rect = GetNotifyIconRectangleWin7(notifyIcon, returnIfHidden);
            else
                rect = GetNotifyIconRectangleLegacy(notifyIcon, returnIfHidden);

            if (rect.HasValue)
                return rect.Value;

            return Rectangle.Empty;
        }

        /// <summary>
        /// Determines whether the specified System.Windows.Forms.NotifyIcon is contained within the Windows 7 notification area fly-out.
        /// Note that this function will return false if the fly-out is closed, or if run on older versions of Windows.
        /// </summary>
        /// <param name="notifyIcon">Notify icon to test.</param>
        /// <returns>True if the notify icon is in the fly-out, false if not.</returns>
        public static bool IsNotifyIconInFlyOut(NotifyIcon notifyIcon)
        {
            if (!SysInfo.IsWindows7OrLater)
                return false;

            Rectangle notifyIconRect = GetNotifyIconRectangle(notifyIcon, true);
            if (notifyIconRect.IsEmpty)
                return false;

            return IsRectangleInFlyOut(notifyIconRect);
        }

        /// <summary>
        /// Determines whether the specified System.Drawing.Rectangle is contained within the Windows 7 notification area fly-out.
        /// Note that this function will return false if the fly-out is closed, or if run on older versions of Windows.
        /// </summary>
        /// <param name="point">System.Drawing.Rectangle to test.</param>
        /// <returns>True if the notify icon is in the fly-out, false if not.</returns>
        public static bool IsRectangleInFlyOut(Rectangle rectangle)
        {
            if (!SysInfo.IsWindows7OrLater)
                return false;

            Rectangle taskbarRect = Taskbar.GetTaskbarRectangle();

            // Don't use Rectangle.IntersectsWith since we want to check if it's ENTIRELY inside
            return (rectangle.Left > taskbarRect.Right || rectangle.Right < taskbarRect.Left
                 || rectangle.Bottom < taskbarRect.Top || rectangle.Top > taskbarRect.Bottom);
        }

        /// <summary>
        /// Checks whether a point is within the bounds of the specified notify icon.
        /// </summary>
        /// <param name="point">Point to check.</param>
        /// <param name="notifyicon">Notify icon to check.</param>
        /// <returns>True if the point is contained in the bounds, false otherwise.</returns>
        public static bool IsPointInNotifyIcon(Point point, NotifyIcon notifyicon)
        {
            Rectangle? nirect = NotifyIconHelpers.GetNotifyIconRectangle(notifyicon, true);
            if (nirect == null)
                return false;
            return ((Rectangle)nirect).Contains(point);
        }

        private static bool CanGetNotifyIconIdentifier(NotifyIcon notifyIcon,
            out NativeMethods.NOTIFYICONIDENTIFIER identifier)
        {
            // You can either use uID + hWnd or a GUID, but GUID is new to Win7 and isn't used by NotifyIcon anyway.

            identifier = new NativeMethods.NOTIFYICONIDENTIFIER();

            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
            Type niType = typeof(NotifyIcon);

            FieldInfo idFieldInfo = niType.GetField("id", flags);
            identifier.uID = (uint)(int)idFieldInfo.GetValue(notifyIcon);

            FieldInfo windowFieldInfo = niType.GetField("window", flags);
            NativeWindow nativeWindow = (NativeWindow)windowFieldInfo.GetValue(notifyIcon);
            identifier.hWnd = nativeWindow.Handle;

            if (identifier.hWnd == null || identifier.hWnd == IntPtr.Zero)
                return false;

            identifier.cbSize = (uint)Marshal.SizeOf(identifier);
            return true;
        }

        private static Rectangle? GetNotifyIconRectangleWin7(NotifyIcon notifyIcon, bool returnIfHidden)
        {
            // Get the identifier
            NativeMethods.NOTIFYICONIDENTIFIER identifier;
            if (!CanGetNotifyIconIdentifier(notifyIcon, out identifier))
                return null;

            // And plug it in to get our rectangle!
            var iconLocation = new NativeMethods.RECT();
            int result = NativeMethods.Shell_NotifyIconGetRect(ref identifier, out iconLocation);
            Rectangle rect = iconLocation;

            // 0 means success, 1 means the notify icon is in the fly-out - either is fine
            if ((result == 0 || (result == 1 && returnIfHidden)) && rect.Width > 0 && rect.Height > 0)
                return iconLocation;
            else
                return null;
        }

        private static Rectangle? GetNotifyIconRectangleLegacy(NotifyIcon notifyIcon, bool returnIfHidden)
        {
            Rectangle? nirect = null;

            NativeMethods.NOTIFYICONIDENTIFIER niidentifier;
            if (!CanGetNotifyIconIdentifier(notifyIcon, out niidentifier))
                return null;

            // find the handle of the task bar
            IntPtr taskbarparenthandle = NativeMethods.FindWindow("Shell_TrayWnd", null);

            if (taskbarparenthandle == IntPtr.Zero)
                return null;

            // find the handle of the notification area
            IntPtr naparenthandle = NativeMethods.FindWindowEx(taskbarparenthandle, IntPtr.Zero, "TrayNotifyWnd", null);

            if (naparenthandle == IntPtr.Zero)
                return null;

            // make a list of toolbars in the notification area (one of them should contain the icon)
            List<IntPtr> natoolbarwindows = NativeMethods.GetChildToolbarWindows(naparenthandle);

            bool found = false;

            for (int i = 0; !found && i < natoolbarwindows.Count; i++)
            {
                IntPtr natoolbarhandle = natoolbarwindows[i];

                // retrieve the number of toolbar buttons (i.e. notify icons)
                int buttoncount = NativeMethods.SendMessage(natoolbarhandle, NativeMethods.TB_BUTTONCOUNT, IntPtr.Zero, IntPtr.Zero).ToInt32();

                // get notification area's process id
                uint naprocessid;
                NativeMethods.GetWindowThreadProcessId(natoolbarhandle, out naprocessid);

                // get handle to notification area's process
                IntPtr naprocesshandle = NativeMethods.OpenProcess(NativeMethods.ProcessAccessFlags.All, false, naprocessid);

                if (naprocesshandle == IntPtr.Zero)
                    return null;

                // allocate enough memory within the notification area's process to store the button info we want
                IntPtr toolbarmemoryptr = NativeMethods.VirtualAllocEx(naprocesshandle, (IntPtr)null, (uint)Marshal.SizeOf(typeof(NativeMethods.TBBUTTON)), NativeMethods.AllocationType.Commit, NativeMethods.MemoryProtection.ReadWrite);

                if (toolbarmemoryptr == IntPtr.Zero)
                    return null;

                try
                {
                    // loop through the toolbar's buttons until we find our notify icon
                    for (int j = 0; !found && j < buttoncount; j++)
                    {
                        int bytesread = -1;

                        // ask the notification area to give us information about the current button
                        NativeMethods.SendMessage(natoolbarhandle, NativeMethods.TB_GETBUTTON, new IntPtr(j), toolbarmemoryptr);

                        // retrieve that information from the notification area's process
                        NativeMethods.TBBUTTON buttoninfo = new NativeMethods.TBBUTTON();
                        NativeMethods.ReadProcessMemory(naprocesshandle, toolbarmemoryptr, out buttoninfo, Marshal.SizeOf(buttoninfo), out bytesread);

                        if (bytesread != Marshal.SizeOf(buttoninfo) || buttoninfo.dwData == IntPtr.Zero)
                            return null;

                        // the dwData field contains a pointer to information about the notify icon:
                        // the handle of the notify icon (an 4/8 bytes) and the id of the notify icon (4 bytes)
                        IntPtr niinfopointer = buttoninfo.dwData;

                        // read the notify icon handle
                        IntPtr nihandlenew;
                        NativeMethods.ReadProcessMemory(naprocesshandle, niinfopointer, out nihandlenew, Marshal.SizeOf(typeof(IntPtr)), out bytesread);

                        if (bytesread != Marshal.SizeOf(typeof(IntPtr)))
                            return null;

                        // read the notify icon id
                        uint niidnew;
                        NativeMethods.ReadProcessMemory(naprocesshandle, (IntPtr)((int)niinfopointer + (int)Marshal.SizeOf(typeof(IntPtr))), out niidnew, Marshal.SizeOf(typeof(uint)), out bytesread);

                        if (bytesread != Marshal.SizeOf(typeof(uint)))
                            return null;

                        // if we've found a match
                        if (nihandlenew == niidentifier.hWnd && niidnew == niidentifier.uID)
                        {
                            // check if the button is hidden: if it is, return the rectangle of the 'show hidden icons' button
                            if ((byte)(buttoninfo.fsState & NativeMethods.TBSTATE_HIDDEN) != 0)
                            {
                                if (returnIfHidden)
                                    nirect = NotifyArea.GetButtonRectangle();
                                else
                                    return null;
                            }
                            else
                            {
                                NativeMethods.RECT result = new NativeMethods.RECT();

                                // get the relative rectangle of the toolbar button (notify icon)
                                NativeMethods.SendMessage(natoolbarhandle, NativeMethods.TB_GETITEMRECT, new IntPtr(j), toolbarmemoryptr);

                                NativeMethods.ReadProcessMemory(naprocesshandle, toolbarmemoryptr, out result, Marshal.SizeOf(result), out bytesread);

                                if (bytesread != Marshal.SizeOf(result))
                                    return null;

                                // find where the rectangle lies in relation to the screen
                                NativeMethods.MapWindowPoints(natoolbarhandle, (IntPtr)null, ref result, 2);

                                nirect = result;
                            }

                            found = true;
                        }
                    }
                }
                finally
                {
                    // free memory within process
                    NativeMethods.VirtualFreeEx(naprocesshandle, toolbarmemoryptr, 0, NativeMethods.FreeType.Release);

                    // close handle to process
                    NativeMethods.CloseHandle(naprocesshandle);
                }
            }

            return nirect;
        }
    }
}
