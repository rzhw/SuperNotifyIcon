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
using System.Drawing;
using System.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Zhwang.SuperNotifyIcon
{
    public partial class SuperNotifyIcon : IDisposable
    {
        public NotifyIcon NotifyIcon { get; set; }

        private FormDrop _formDrop;
        private string _text;
        public string Text
        {
            get { return _text; }
            set
            {
                // Code from http://stackoverflow.com/q/579665/580264#580264
                if (value.Length >= 128)
                    throw new ArgumentOutOfRangeException("ToolTip text must be less than 128 characters long");
                _text = value;
                Type t = typeof(NotifyIcon);
                BindingFlags hidden = BindingFlags.NonPublic | BindingFlags.Instance;
                t.GetField("text", hidden).SetValue(NotifyIcon, _text);
                if ((bool)t.GetField("added", hidden).GetValue(NotifyIcon))
                    t.GetMethod("UpdateIcon", hidden).Invoke(NotifyIcon, new object[] { true });
            }
        }

        public bool AutoIconCleanup { get; set; }
        public Icon Icon
        {
            get { return NotifyIcon.Icon; }
            set
            {
                // Automatically cleaning up the icon helps a lot with GDI handle usage
                IntPtr oldIconHandle = NotifyIcon.Icon == null ? IntPtr.Zero : NotifyIcon.Icon.Handle;
                NotifyIcon.Icon = value;
                if (AutoIconCleanup && oldIconHandle != IntPtr.Zero)
                    DestroyIcon(oldIconHandle);
            }
        }

        public Animation Animation { get; private set; }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        public SuperNotifyIcon()
        {
            AutoIconCleanup = true;
            NotifyIcon = new NotifyIcon();
            Animation = new Animation() { _parent = this };
        }

        private bool disposed = false;

        ~SuperNotifyIcon()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this); // The finalise process no longer needs to be run for this
        }

        protected virtual void Dispose(bool disposeManagedResources)
        {
            if (!disposed)
            {
                if (disposeManagedResources)
                {
                    try
                    {
                        NotifyIcon.Dispose();
                        _formDrop.Dispose();
                    }
                    catch { }
                }
                disposed = true;
            }
        }
    }
}
