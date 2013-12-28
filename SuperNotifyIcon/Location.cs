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

namespace Zhwang.SuperNotifyIcon
{
    public partial class SuperNotifyIcon
    {
        public Point? GetLocation()
        {
            return GetLocation(0);
        }
        public Point? GetLocation(int accuracy)
        {
            return GetLocation(accuracy, false);
        }
        public Point? GetLocation(bool tryReturnIfHidden)
        {
            return GetLocation(0, tryReturnIfHidden);
        }

        public Point? GetLocation(int accuracy, bool tryReturnIfHidden)
        {
            // Try using APIs first
            Rectangle rect = NotifyIconHelpers.GetNotifyIconRectangle(NotifyIcon, tryReturnIfHidden);
            if (!rect.IsEmpty)
                return rect.Location;

            // Don't fallback if the icon isn't visible
            if (!tryReturnIfHidden)
            {
                Rectangle rect2 = NotifyIconHelpers.GetNotifyIconRectangle(NotifyIcon, true);
                if (rect2.IsEmpty)
                    return null;
            }

            // Ugly fallback time :(
            var finder = new Zhwang.SuperNotifyIcon.Finder.NotifyIconColorFinder(NotifyIcon);
            Point? point = finder.GetLocation(accuracy);
            if (point.HasValue)
                return point;

            return null;
        }
    }
}
