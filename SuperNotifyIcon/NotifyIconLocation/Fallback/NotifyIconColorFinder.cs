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
using System.Windows.Forms;
using Zhwang.SuperNotifyIcon;

namespace Zhwang.SuperNotifyIcon.Finder
{
    /**
     * =====================
     * READ THIS, SERIOUSLY.
     * =====================
     * Here be dragons. This bit of the code was coded before coming across Shell_NotifyIconGetRect.
     * Then again, that was introduced in Windows 7. So... expect this to be quite, well, kludgey. Very.
     */

    class NotifyIconColorFinder
    {
        Color nearColor;
        bool nearColorSet = false;
        NotifyIcon _notifyIcon;

        public NotifyIconColorFinder(NotifyIcon notifyIcon)
        {
            _notifyIcon = notifyIcon;
        }

        public void GetLocationPrepare()
        {
            // Get a screenshot of the notification area...
            Rectangle notifyAreaRect = NotifyArea.GetRectangle();
            Size notifyAreaSize = notifyAreaRect.Size;
            using (Bitmap notifyAreaBitmap = GetNotifyAreaScreenshot())
            {
                // Something gone wrong? Give up.
                if (notifyAreaBitmap == null)
                    return;

                // Determine a good spot...
                if (notifyAreaSize.Width > notifyAreaSize.Height)
                    nearColor = notifyAreaBitmap.GetPixel(notifyAreaSize.Width - 3, notifyAreaSize.Height / 2);
                else
                    nearColor = notifyAreaBitmap.GetPixel(notifyAreaSize.Width / 2, notifyAreaSize.Height - 3);

                // And now we have our base colour!
                nearColorSet = true;
            }
        }

        public Point? GetLocation(int accuracy)
        {
            // Got something fullscreen running? Of course we can't find our icon!
            if (SysInfo.ForegroundWindowIsFullScreen)
                return null;

            // The accuracy can't be below 0!
            if (accuracy < 0)
                throw new ArgumentOutOfRangeException("accuracy", "The accuracy value provided can't be negative!");

            // The notification area
            Rectangle notifyAreaRect = NotifyArea.GetRectangle();
            if (notifyAreaRect.IsEmpty)
                return null;

            // Back up the NotifyIcon's icon so we can reset it later on
            Icon notifyIconIcon = _notifyIcon.Icon;

            // Have we got a colour we could base the find pixel off?
            if (!nearColorSet)
                GetLocationPrepare();

            // Blah
            List<int> colMatchIndexes = new List<int>();
            Point last = new Point(-1, -1);
            int hits = 0;
            int hitsMax = accuracy + 1;

            // Our wonderful loop
            for (int attempt = 0; attempt < 5 && hits < hitsMax; attempt++)
            {
                // Set the notify icon thingy to a random colour
                Random random = new Random();
                int rgbRange = 32;
                Color col;
                if (nearColorSet)
                    col = Color.FromArgb(
                        SafeColourVal(nearColor.R + random.Next(rgbRange) - 8),
                        SafeColourVal(nearColor.G + random.Next(rgbRange) - 8),
                        SafeColourVal(nearColor.B + random.Next(rgbRange) - 8));
                else
                    col = Color.FromArgb(
                        SafeColourVal(255 - random.Next(rgbRange)),
                        SafeColourVal(255 - random.Next(rgbRange)),
                        SafeColourVal(255 - random.Next(rgbRange)));

                // Set the find colour...
                SetFindColour(col);

                // Take a screenshot...
                Color[] taskbarPixels;
                using (Bitmap notifyAreaBitmap = GetNotifyAreaScreenshot())
                {
                    // If something goes wrong, let's just assume we don't know where we should be
                    if (notifyAreaBitmap == null)
                        return null;

                    // We can reset the NotifyIcon now, and then...
                    _notifyIcon.Icon = notifyIconIcon;

                    // Grab the pixels of the taskbar using my very own Pfz-derived bitmap to pixel array awesomeness
                    taskbarPixels = BitmapToPixelArray(notifyAreaBitmap);
                }

                // Get every occurence of our lovely colour next to something the same...
                bool colMatched = false;
                int colMatchIndex = -1;
                int colAttempt = 0; // this determines whether we -1 any of the RGB
                while (true)
                {
                    Color col2 = Color.FromArgb(0, 0, 0);
                    //int colModAmount = colAttempt < 8 ? -1 : 1;
                    int colMod1 = (colAttempt % 8) < 4 ? 0 : -1;
                    int colMod2 = (colAttempt % 8) < 4 ? -1 : 0;

                    switch (colAttempt % 4)
                    {
                        case 0: col2 = Color.FromArgb(SafeColourVal(col.R + colMod1), SafeColourVal(col.G + colMod1), SafeColourVal(col.B + colMod1)); break;
                        case 1: col2 = Color.FromArgb(SafeColourVal(col.R + colMod1), SafeColourVal(col.G + colMod1), SafeColourVal(col.B + colMod2)); break;
                        case 2: col2 = Color.FromArgb(SafeColourVal(col.R + colMod1), SafeColourVal(col.G + colMod2), SafeColourVal(col.B + colMod1)); break;
                        case 3: col2 = Color.FromArgb(SafeColourVal(col.R + colMod1), SafeColourVal(col.G + colMod2), SafeColourVal(col.B + colMod2)); break;
                    }

                    colAttempt++;

                    colMatchIndex = Array.FindIndex<Color>(taskbarPixels, colMatchIndex + 1, (Color c) => { return c == col2; });

                    if (colMatchIndex == -1)
                    {
                        if (colAttempt < 8)
                            continue;
                        else
                            break;
                    }
                    else
                    {
                        if (taskbarPixels[colMatchIndex + 1] == col2)
                        {
                            colMatched = true;
                            break;
                        }
                    }
                }

                if (colMatched)
                {
                    hits++;
                    last.X = colMatchIndex % notifyAreaRect.Width;
                    last.Y = colMatchIndex / notifyAreaRect.Width; // Integer rounding is always downwards
                }
                else
                {
                    hits = 0;
                    last.X = -1;
                    last.Y = -1;
                }
            }

            // Don't forget, our current values are relative to the notification area and are at the bottom right of the icon!
            Point location = new Point(last.X, last.Y);
            if (location != new Point(-1, -1))
                return new Point(notifyAreaRect.X + (last.X - 16), notifyAreaRect.Y + (last.Y - 14));
            else
                return null;
        }

        private static Bitmap GetNotifyAreaScreenshot()
        {
            Rectangle notifyAreaRect = NotifyArea.GetRectangle();
            Bitmap notifyAreaBitmap = new Bitmap(notifyAreaRect.Width, notifyAreaRect.Height);
            using (Graphics notifyAreaGraphics = Graphics.FromImage(notifyAreaBitmap))
            {
                try
                {
                    notifyAreaGraphics.CopyFromScreen(notifyAreaRect.X, notifyAreaRect.Y, 0, 0, notifyAreaRect.Size);
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    return null;
                }
            }
            return notifyAreaBitmap;
        }

        private void SetFindColour(Color col)
        {
            // Grab the notification icon
            Bitmap iconBitmap = _notifyIcon.Icon.ToBitmap();

            // Draw on it
            Graphics iconGraphics = Graphics.FromImage(iconBitmap);
            iconGraphics.DrawRectangle(new Pen(col, 1), 12, 14, 3, 2);
            _notifyIcon.Icon = Icon.FromHandle(iconBitmap.GetHicon());
            iconGraphics.Dispose();
        }

        private static int SafeColourVal(int val)
        {
            return Math.Min(255, Math.Max(0, val) + 0);
        }

        /// <summary>
        /// Converts a System.Drawing.Bitmap to an array of System.Drawing.Colors.
        /// Based on code by Paulo Zemek written under The Code Project Open License
        /// http://www.codeproject.com/KB/graphics/ManagedBitmaps.aspx
        /// </summary>
        /// <param name="bitmap">The bitmap to convert.</param>
        /// <returns>An array of System.Drawing.Colors</returns>
        private static Color[] BitmapToPixelArray(Bitmap bitmap)
        {
            Bitmap fOriginalSystemBitmap = bitmap;
            Color[] cols = new Color[fOriginalSystemBitmap.Size.Width * fOriginalSystemBitmap.Size.Height];

            System.Drawing.Imaging.BitmapData sourceData = null;

            // The below structure of try/finally runs a block of code, guaranting that:
            // The allocation block will not be aborted.
            // The finally block will be called, independent if the allocation block was
            // run.
            // The code block is the only one that could be aborted.
            try
            {
                try { }
                finally
                {
                    sourceData = fOriginalSystemBitmap.LockBits(
                        new Rectangle(new Point(), fOriginalSystemBitmap.Size),
                        System.Drawing.Imaging.ImageLockMode.ReadOnly,
                        System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                }

                Size size = fOriginalSystemBitmap.Size;
                unsafe
                {
                    byte* sourceScanlineBytes = (byte*)sourceData.Scan0;
                    for (int y = 0; y < size.Height; y++)
                    {
                        int* sourceScanline = (int*)sourceScanlineBytes;

                        for (int x = 0; x < size.Width; x++)
                        {
                            int color = sourceScanline[x];
                            int index = x % size.Width + y * size.Width;
                            cols[index] = Color.FromArgb((color >> 16) & 0xFF, (color >> 8) & 0xFF, color & 0xFF);
                        }

                        sourceScanlineBytes += sourceData.Stride;
                    }
                }
            }
            finally
            {
                if (sourceData != null)
                    fOriginalSystemBitmap.UnlockBits(sourceData);
            }

            return cols;
        }
    }
}
