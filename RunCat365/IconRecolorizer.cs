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

namespace RunCat365
{
    internal static class BitmapExtension
    {
        internal static Bitmap Recolor(this Bitmap bitmap, Color color)
        {
            var newBitmap = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);

            var srcData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb
            );

            try
            {
                var dstData = newBitmap.LockBits(
                    new Rectangle(0, 0, newBitmap.Width, newBitmap.Height),
                    ImageLockMode.WriteOnly,
                    PixelFormat.Format32bppArgb
                );

                try
                {
                    unsafe
                    {
                        byte* srcPtr = (byte*)srcData.Scan0;
                        byte* dstPtr = (byte*)dstData.Scan0;

                        for (int y = 0; y < bitmap.Height; y++)
                        {
                            byte* srcRow = srcPtr + (y * srcData.Stride);
                            byte* dstRow = dstPtr + (y * dstData.Stride);

                            for (int x = 0; x < bitmap.Width; x++)
                            {
                                byte* srcPixel = srcRow + (x * 4);
                                byte* dstPixel = dstRow + (x * 4);

                                dstPixel[0] = color.B;
                                dstPixel[1] = color.G;
                                dstPixel[2] = color.R;
                                dstPixel[3] = srcPixel[3];
                            }
                        }
                    }
                }
                finally
                {
                    newBitmap.UnlockBits(dstData);
                }
            }
            finally
            {
                bitmap.UnlockBits(srcData);
            }

            return newBitmap;
        }

        internal static Icon ToIcon(this Bitmap bitmap)
        {
            using var pngStream = new MemoryStream();
            bitmap.Save(pngStream, ImageFormat.Png);
            var pngData = pngStream.ToArray();

            using var icoStream = new MemoryStream();
            using var bw = new BinaryWriter(icoStream);

            bw.Write((short)0);
            bw.Write((short)1);
            bw.Write((short)1);

            bw.Write((byte)(bitmap.Width >= 256 ? 0 : bitmap.Width));
            bw.Write((byte)(bitmap.Height >= 256 ? 0 : bitmap.Height));
            bw.Write((byte)0);
            bw.Write((byte)0);
            bw.Write((short)1);
            bw.Write((short)32);
            bw.Write(pngData.Length);
            bw.Write(22);

            bw.Write(pngData);

            icoStream.Position = 0;
            return new Icon(icoStream);
        }
    }
}
