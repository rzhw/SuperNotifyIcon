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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Zhwang.SuperNotifyIcon
{
    internal partial class FormDrop : Form
    {
        private SuperNotifyIcon _owner;
        private bool mouseLeftDown = false;
        private Point? lastNotifyIconPoint;
        private bool mouseDownAndWasInTaskbar = false;
        public static MouseHook MouseHook { get; set; }
        private bool _debug = false;
        private FormDebugger formDebugger;

        static FormDrop()
        {
            MouseHook = new MouseHook();
            MouseHook.Start();
        }

        private static readonly StaticDestructor sd = new StaticDestructor();
        private class StaticDestructor
        {
            ~StaticDestructor()
            {
                MouseHook.Stop();
            }
        }

        public FormDrop(SuperNotifyIcon owner) : this(owner, false) { }
        public FormDrop(SuperNotifyIcon owner, bool debug)
        {
            InitializeComponent();

            // Blah
            _owner = owner;
            Visible = false;

            // Debug
            if (debug)
            {
                _debug = true;
                formDebugger = new FormDebugger();
                OwnApplicationActive(); // blah
                formDebugger.Show();
            }

            // Keeping on top of things
            ControlBox = false;
            MinimizeBox = false;
            MaximizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Text = "";
            Deactivate += (sender2, e2) => Activate();
            VisibleChanged += (sender2, e2) => WindowState = FormWindowState.Normal;
            SizeChanged +=(sender2, e2) => WindowState = FormWindowState.Normal;

            // We don't want to be obvious!
            ShowInTaskbar = false;
            if (!debug)
                Opacity = 0.005;

            // Drop support
            AllowDrop = true;
            DragLeave += (sender2, e2) => TopMost = false;
            MouseEnter += (sender2, e2) => TopMost = false;
            MouseLeave += (sender2, e2) => TopMost = false;

            // Drop events
            DragDrop += _owner.HandleDragDrop;
            DragEnter += _owner.HandleDragEnter;
            DragLeave += _owner.HandleDragLeave;
            DragOver += _owner.HandleDragOver;

            // Whether the left mouse button is down
            MouseHook.MouseDown += (sender2, e2) => mouseLeftDown = e2.Button == MouseButtons.Left;
            MouseHook.MouseUp += (sender2, e2) => mouseLeftDown = false;

            // And now to initialise the behaviour...
            Init();
            ShowDrop();
        }

        private bool mouseWasInNotifyArea = false;
        private void Init()
        {
            // Does the owner have an icon set?
            if (_owner.Icon == null)
            {
                throw new InvalidOperationException("SuperNotifyIcon: Dropping cannot be initialised without an icon set!");
            }

            // When the mouse is close
            MouseHook.MouseMove += MouseHook_MouseMove;

            // Cancel the drop refreshing below if we do an actual click on the NotifyIcon
            _owner.NotifyIcon.MouseUp += (sender, e) =>
            {
                mouseWasInNotifyArea = false;
            };

            // Refresh the drop position if we click in the notification area on Windows 7; we might've moved an icon!
            if (SysInfo.IsWindows7OrLater)
            {
                MouseHook.MouseDown += (sender, e) =>
                {
                    mouseWasInNotifyArea = MouseInNotifyArea();

                    // Shall we cancel, then?
                    if (e.Button != MouseButtons.Left)
                    {
                        mouseWasInNotifyArea = false;
                    }
                };
                MouseHook.MouseUp += (sender, e) =>
                {
                    if (MouseInNotifyArea() && mouseWasInNotifyArea)
                    {
                        // We should wait for the icon to settle in before doing anything
                        Timer wait = new Timer();
                        wait.Tick += (sender2, e2) =>
                        {
                            if (mouseWasInNotifyArea)
                                ShowDrop();
                            mouseWasInNotifyArea = false;
                            wait.Dispose();
                        };
                        wait.Interval = 200;
                        wait.Start();
                    }
                };
            }

            // Refresh the drop position if the size of the notification area changes
            Size notifyAreaLastSize = NotifyArea.GetRectangle().Size;
            Timer notifyAreaTimer = new Timer();
            notifyAreaTimer.Tick += (sender, e) =>
            {
                if (NotifyArea.GetRectangle().Size != notifyAreaLastSize)
                {
                    notifyAreaLastSize = NotifyArea.GetRectangle().Size;
                    ShowDrop();
                }
            };
            notifyAreaTimer.Interval = 1000;
            notifyAreaTimer.Start();

            // Is the drop even in the right place at all?
            int unsuccessfulRefreshes = 0;
            Timer dropPlaceTimer = new Timer();
            dropPlaceTimer.Tick += (sender, e) =>
            {
                if (!NotifyArea.GetRectangle().Contains(new Point(this.Location.X + 2, this.Location.Y + 2)))
                {
                    ShowDrop();
                    unsuccessfulRefreshes++;

                    // Don't keep refreshing every second if we can't find our icon!
                    if (unsuccessfulRefreshes >= 3)
                        dropPlaceTimer.Interval = unsuccessfulRefreshes * 1000;
                }
                else
                {
                    unsuccessfulRefreshes = 0;
                    dropPlaceTimer.Interval = 1000;
                }
            };
            dropPlaceTimer.Interval = 1000;
            dropPlaceTimer.Start();

            // Okay... still no success? Let's fall back to the mouse timer...
            //// TODO: See whether this should only be run on WinXP/Vista systems and whether this should
            //// run even if we have a valid drop position
            MouseHoldTimed mouseHold = new MouseHoldTimed(500);
            mouseHold.MouseDown += (sender, e) =>
            {
                if (e.Button != MouseButtons.Left || OwnApplicationActive() || !lastNotifyIconPoint.HasValue)
                    mouseHold.Cancel();
            };
            mouseHold.MouseHoldTimeout += (sender, e) =>
            {
                if (lastNotifyIconPoint.HasValue)
                    ShowDrop();
            };
        }

        private bool OwnApplicationActive()
        {
            // This checks whether any form from another assembly but in the same application is active. We grab the forms here...
            System.Reflection.Assembly currentAssembly = System.Reflection.Assembly.GetExecutingAssembly();
            List<string> formsFromOtherAssemblies = new List<string>();
            foreach (Form form in Application.OpenForms)
            {
                if (form.GetType().Assembly != currentAssembly)
                {
                    formsFromOtherAssemblies.Add(form.GetType().Name);
                }
            }

            // Debug output
            if (_debug)
            {
                formDebugger.textBox.Text = "Forms from other assemblies: " + String.Join(",", formsFromOtherAssemblies.ToArray())
                    + Environment.NewLine + "Currently active form in the application: " + (Form.ActiveForm == null ? "None" : Form.ActiveForm.GetType().Name);
            }

            // And here's our check!
            return (Form.ActiveForm != null && formsFromOtherAssemblies.Contains(Form.ActiveForm.GetType().Name));
        }

        private void MouseHook_MouseMove(object sender, MouseEventArgs e)
        {
            if (mouseLeftDown && !OwnApplicationActive())
            {
                // Of course we have to be visible!
                if (Visible)
                {
                    // Don't get the form on top until we're close to it
                    if (Math.Sqrt(Math.Pow(Location.X + Size.Width / 2 - Cursor.Position.X, 2)
                        + Math.Pow(Location.Y + Size.Height / 2 - Cursor.Position.Y, 2)) <= 48)
                    {
                        TopMost = true;
                    }
                    else
                    {
                        TopMost = false;
                    }
                }

                // Autohide stuff
                if (Taskbar.GetTaskbarState() == Taskbar.TaskbarState.AutoHide)
                {
                    bool mouseInTaskbar = MouseInTaskbar();
                    if (mouseDownAndWasInTaskbar != mouseInTaskbar)
                    {
                        mouseDownAndWasInTaskbar = mouseInTaskbar;
                        ShowDrop();
                    }
                }
            }
            else
            {
                mouseDownAndWasInTaskbar = false;
            }
        }

        bool firstFind = false;
        public void ShowDrop()
        {
            // Wrong thread?
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(() => ShowDrop()));
                return;
            }

            // Somehow this can be run even when disposed...
            if (IsDisposed)
                return;

            // Don't do anything if we're out of the taskbar with auto-hide
            if (Taskbar.GetTaskbarState() == Taskbar.TaskbarState.AutoHide && !MouseInTaskbar())
            {
                Hide();
                return;
            }

            // Now then...
            lastNotifyIconPoint = _owner.GetLocation(false);
            Hide();

            // Point?
            _owner.DropRefreshCallback(lastNotifyIconPoint.HasValue);

            // Don't bother doing anything if we don't have a value
            if (lastNotifyIconPoint.HasValue)
            {
                Point notifyIconLocation = lastNotifyIconPoint.Value;

                // Stuff
                Size taskbarSize = Taskbar.GetTaskbarSize();
                Point taskbarLocation = Taskbar.GetTaskbarLocation();

                // We've got a find, yessiree!
                firstFind = true;

                // Anyway, the task at hand; where does our drop zone go?
                switch (Taskbar.GetTaskbarEdge())
                {
                    case Taskbar.TaskbarEdge.Bottom:
                        Top = taskbarLocation.Y + 2;
                        Left = notifyIconLocation.X;
                        Width = 24;
                        Height = taskbarSize.Height;
                        break;
                    case Taskbar.TaskbarEdge.Top:
                        Top = -2;
                        Left = notifyIconLocation.X;
                        Width = 24;
                        Height = taskbarSize.Height;
                        break;
                    case Taskbar.TaskbarEdge.Left:
                        Top = notifyIconLocation.Y;
                        Left = -2;
                        Width = taskbarSize.Width;
                        Height = 24;
                        break;
                    case Taskbar.TaskbarEdge.Right:
                        Top = notifyIconLocation.Y;
                        Left = taskbarLocation.X + 2;
                        Width = taskbarSize.Width;
                        Height = 24;
                        break;
                }

                // We still want to show again even if we fail to find, but only if we've found it at least once!
                if (firstFind)
                {
                    // Post-disposal exception horror fix pt.2
                    try
                    {
                        Show();
                        TopMost = false;
                    }
                    catch { }
                }
            }
        }

        private static bool MouseInTaskbar()
        {
            return Taskbar.GetTaskbarRectangle().Contains(Cursor.Position);
        }

        private static bool MouseInNotifyArea()
        {
            return NotifyArea.GetRectangle().Contains(Cursor.Position);
        }
    }
}
