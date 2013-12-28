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
using System.Windows.Forms;
using Zhwang.SuperNotifyIcon.Finder;

namespace Zhwang.SuperNotifyIcon
{
    public partial class SuperNotifyIcon
    {
        // Activating the drop is done through this instead of something like a property "AllowDrop" because of the lag
        public void InitDrop(bool debug)
        {
            _formDrop = new FormDrop(this, debug);
        }
        public void InitDrop()
        {
            InitDrop(false);
        }

        // These don't attach/detach from the events in FormDrop as we should allow attaching and detaching even
        // when FormDrop isn't initialised.
        public event DragEventHandler DragDrop;
        public event DragEventHandler DragEnter;
        public event EventHandler DragLeave;
        public event DragEventHandler DragOver;

        internal void HandleDragDrop(object sender, DragEventArgs e)
        {
            if (DragDrop != null)
                DragDrop(sender, e);
        }

        internal void HandleDragEnter(object sender, DragEventArgs e)
        {
            if (DragEnter != null)
                DragEnter(sender, e);
        }

        internal void HandleDragLeave(object sender, EventArgs e)
        {
            if (DragLeave != null)
                DragLeave(sender, e);
        }

        internal void HandleDragOver(object sender, DragEventArgs e)
        {
            if (DragOver != null)
                DragOver(sender, e);
        }

        // Call 1-800-DropRefreshed
        public delegate void DropRefreshedEventHandler(object sender, DropRefreshedEventArgs e);
        public event DropRefreshedEventHandler DropRefreshed;

        internal void DropRefreshCallback(bool successful)
        {
            if (DropRefreshed != null)
                DropRefreshed(this, new DropRefreshedEventArgs() { Successful = successful });
        }
    }

    public class DropRefreshedEventArgs : EventArgs
    {
        public bool Successful { get; internal set; }
        public DropRefreshedEventArgs() { }
    }
}
