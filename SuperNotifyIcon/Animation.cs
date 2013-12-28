/**
 * Copyright (c) 2010-2011,2013 Richard Z.H. Wang <http://rzhw.me/>
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
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Text;
using System.Threading.Tasks;

namespace Zhwang.SuperNotifyIcon
{
    public class Animation
    {
        internal SuperNotifyIcon _parent;

        Image _animBaseImage;
        Image[] _animFrames = new Image[] { };

        Timer _animRefreshTimer = new Timer() { Interval = 1000 / 30 }; // 30fps

        Timer _animFrameTimer = new Timer();
        int _animFrame = 0;
        int _animLastDrawnFrame = 0;
        int _animFramesToFade = 0;

        bool _animOpacityReversing = false;
        float _animOpacity = 0;

        TaskCompletionSource<int> _endingTcs;

        Bitmap _animCanvas = new Bitmap(16, 16);

        public Animation()
        {
            _animRefreshTimer.Tick += _animTimer_Tick;
            _animFrameTimer.Tick += _animFrameTimer_Tick;
        }

        public void BeginWithOverlay(Icon baseIcon, Image[] images, int interval, int framesToFade)
        {
            _animBaseImage = baseIcon.ToBitmap();
            _animFrames = images;

            // Say interval of 100ms, and updating at 30fps (interval of 33.3) and you want to fade out in 4 frames.
            // 4*100 would be equivalent to 400ms, and (4*100)/33 = 12.12 frames, which when multiplied is about 404ms
            _animFramesToFade = Math.Max(1, framesToFade) * interval / _animRefreshTimer.Interval;
            _animFrameTimer.Interval = interval;

            _animOpacity = 0;
            _animOpacityReversing = false;

            _animFrame = 0;
            _animLastDrawnFrame = 0;

            _animRefreshTimer.Start();
            _animFrameTimer.Start();
        }

        public Task End(int framesToFade)
        {
            if (!_animOpacityReversing) // only return a new tcs if we're not already ending
                _endingTcs = new TaskCompletionSource<int>();

            if ((_animRefreshTimer.Enabled || _animFrameTimer.Enabled) && framesToFade > 0)
            {
                _animFramesToFade = framesToFade;
                _animOpacityReversing = true;
            }
            else
            {
                _animRefreshTimer.Stop();
                _animFrameTimer.Stop();

                _endingTcs.SetResult(0);
            }

            return _endingTcs.Task;
        }

        private void _animTimer_Tick(object sender, EventArgs e)
        {
            // Opacity!
            if (!_animOpacityReversing)
            {
                if (_animOpacity < 1)
                    _animOpacity = Math.Min(100f, _animOpacity + 1f / _animFramesToFade);
                else if (_animFrame == _animLastDrawnFrame)
                    return;
            }
            else
            {
                if (_animOpacity > 0)
                    _animOpacity = Math.Max(0f, _animOpacity - 1f / _animFramesToFade);
                else if (_animFrame == _animLastDrawnFrame)
                    return;
            }

            using (Graphics g = Graphics.FromImage(_animCanvas))
            {
                g.Clear(Color.Transparent);

                ImageAttributes attributes = new ImageAttributes();
                attributes.SetColorMatrix(
                    new ColorMatrix { Matrix33 = _animOpacity },
                    ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                // And now to draw the icon!
                g.DrawImage(_animBaseImage, 0, 0, 16, 16);
                g.DrawImage(_animFrames[_animFrame],
                    new Rectangle(0, 0, 16, 16),
                    0, 0, 16, 16, // Oh dear god this code is ugly
                    GraphicsUnit.Pixel,
                    attributes);
                _parent.Icon = Icon.FromHandle(_animCanvas.GetHicon());

                // Update the last frame
                _animLastDrawnFrame = _animFrame;
            }

            // If we're finished, stop the timers!
            if (_animOpacityReversing && _animOpacity == 0)
            {
                if (_endingTcs != null)
                    _endingTcs.SetResult(0);
                _animOpacityReversing = false;
                _animRefreshTimer.Stop();
                _animFrameTimer.Stop();
            }
        }

        private void _animFrameTimer_Tick(object sender, EventArgs e)
        {
            // What frame are we on?
            _animFrame++;
            if (_animFrame == _animFrames.Length)
                _animFrame = 0;
        }
    }
}
