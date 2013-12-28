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
using System.Linq;
using System.Text;
using System.Timers;
using System.Windows.Forms;

namespace Zhwang.SuperNotifyIcon
{
    internal class MouseHoldTimed
    {
        public event MouseEventHandler MouseDown;
        public event MouseEventHandler MouseUp;
        public event MouseEventHandler MouseHoldTimeout;

        private MouseButtons mouseHoldWhich;

        private System.Timers.Timer mouseDownTimer = new System.Timers.Timer();

        public MouseHoldTimed(int duration)
        {
            // Detecting mouse down/up events
            FormDrop.MouseHook.MouseDown += new MouseEventHandler(MouseHook_MouseDown);
            FormDrop.MouseHook.MouseUp += new MouseEventHandler(MouseHook_MouseUp);

            // The mouse down timer
            mouseDownTimer.Stop();
            mouseDownTimer.Interval = duration;
            mouseDownTimer.Elapsed += mouseDownTimer_Elapsed;
        }

        public void Cancel()
        {
            mouseDownTimer.Stop();
        }

        void mouseDownTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            mouseDownTimer.Stop();

            // TODO: detect scroll wheel clicks (last param of MouseEventArgs)
            if (this.MouseHoldTimeout != null)
                this.MouseHoldTimeout(this, new MouseEventArgs(mouseHoldWhich, 1, Cursor.Position.X, Cursor.Position.Y, 0));
        }

        void MouseHook_MouseDown(object sender, MouseEventArgs e)
        {
            mouseHoldWhich = e.Button;
            mouseDownTimer.Start();

            if (this.MouseDown != null)
                this.MouseDown(this, e);
        }

        void MouseHook_MouseUp(object sender, MouseEventArgs e)
        {
            mouseHoldWhich = MouseButtons.None;
            mouseDownTimer.Stop();

            if (this.MouseUp != null)
                this.MouseUp(this, e);
        }
    }
}
