using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Zhwang.SuperNotifyIcon;

namespace Zhwang.SuperNotifyIcon.Demo
{
    public partial class Form1 : Form
    {
        private SuperNotifyIcon notifyIcon = new SuperNotifyIcon();

        public Form1()
        {
            Font = SystemFonts.MessageBoxFont;
            AutoScaleMode = AutoScaleMode.Font;
            InitializeComponent();

            // You can create a new instance and set the properties like follows, or you can create a standard NotifyIcon
            // and set the NotifyIcon property to it.
            notifyIcon.Text = "SuperNotifyIcon Demo\nLook ma, this string is more than 64 characters long!";
            notifyIcon.NotifyIcon.Icon = Properties.Resources.NotifyIconIdle;
            notifyIcon.NotifyIcon.Visible = true;
            notifyIcon.InitDrop(true);

            // A standard NotifyIcon requires manual disposal, and unfortunately, so does SuperNotifyIcon
            FormClosing += (sender, e) => notifyIcon.Dispose();
        }

        private void buttonLocationRefresh_Click(object sender, EventArgs e)
        {
            labelLocation.Text = "Refreshing...";
            Refresh();

            DateTime startTime = DateTime.Now;
            Point? notifyIconLocation = notifyIcon.GetLocation(false);
            DateTime finishTime = DateTime.Now;
            double duration = ((TimeSpan)(finishTime - startTime)).TotalMilliseconds;

            labelLocation.Text = (!notifyIconLocation.HasValue ? "Couldn't find" :
                "X: " + notifyIconLocation.Value.X + " Y: " + notifyIconLocation.Value.Y) + " (" + duration + "ms)";
        }
    }
}
