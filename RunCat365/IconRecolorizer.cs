// Copyright 2025 Takuto Nakamura
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace RunCat365 {
    internal static class IconExtension
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        internal static Icon Recolor(this Icon icon, Color color)
        {
            var original = icon.ToBitmap();
            var recolored = original.Recolor(color);

            var hIcon = recolored.GetHicon();
            using var tempIcon = Icon.FromHandle(hIcon);
            using var ms = new MemoryStream();
            tempIcon.Save(ms);
            ms.Position = 0;
            var result = new Icon(ms);
            DestroyIcon(hIcon);
            return result;
        }
    }

    internal static class BitmapExtension
    {
        internal static Bitmap Recolor(this Bitmap icon, Color color)
        {
            var newIcon = new Bitmap(icon.Width, icon.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(newIcon)) g.DrawImage(icon, 0, 0);

            var data = newIcon.LockBits
            (
                new Rectangle(0, 0, newIcon.Width, newIcon.Height),
                ImageLockMode.ReadWrite,
                PixelFormat.Format32bppArgb
            );

            unsafe
            {
                byte* ptr = (byte*)data.Scan0;

                for (int y = 0; y < newIcon.Height; y++)
                {
                    byte* row = ptr + (y * data.Stride);

                    for (int x = 0; x < newIcon.Width; x++)
                    {
                        byte* pixel = row + (x * 4);

                        var a = pixel[3];

                        if (a > 0)
                        {
                            pixel[0] = color.B;
                            pixel[1] = color.G;
                            pixel[2] = color.R;
                        }
                    }
                }
            }

            newIcon.UnlockBits(data);
            return newIcon;
        }
    }
}
