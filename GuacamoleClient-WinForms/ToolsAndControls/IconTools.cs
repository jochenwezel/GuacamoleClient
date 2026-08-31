using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace GuacamoleClient.WinForms
{
    public static class IconTools
    {
        /// <summary>
        /// Create a windows icon from a PNG file (website favicon)
        /// </summary>
        /// <param name="pngStream"></param>
        /// <returns></returns>
        public static Icon? CreateIconFromPngStream(Stream pngStream)
        {
            try
            {
                using var pngBitmap = new Bitmap(pngStream);
                return CreateIconFromBitmap(pngBitmap);
            }
            catch
            {
                return null;
            }
        }

        internal static Icon? CreateBadgedIconFromPngStream(Stream pngStream, Icon badgeSource)
        {
            try
            {
                const int iconSize = 64;
                using var favicon = new Bitmap(pngStream);
                using Bitmap badgeBitmap = badgeSource.ToBitmap();
                using var composedBitmap = new Bitmap(iconSize, iconSize);
                using Graphics graphics = Graphics.FromImage(composedBitmap);

                graphics.Clear(Color.Transparent);
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.DrawImage(favicon, 0, 0, iconSize, iconSize);

                int sourceSize = Math.Min(badgeBitmap.Width, badgeBitmap.Height);
                int sourceBadgeSize = Math.Max(7, (int)Math.Round(sourceSize * 0.44));
                int targetBadgeSize = (int)Math.Round(iconSize * 0.44);
                var sourceBounds = new Rectangle(
                    badgeBitmap.Width - sourceBadgeSize,
                    badgeBitmap.Height - sourceBadgeSize,
                    sourceBadgeSize,
                    sourceBadgeSize);
                var targetBounds = new Rectangle(
                    iconSize - targetBadgeSize,
                    iconSize - targetBadgeSize,
                    targetBadgeSize,
                    targetBadgeSize);

                graphics.DrawImage(badgeBitmap, targetBounds, sourceBounds, GraphicsUnit.Pixel);
                return CreateIconFromBitmap(composedBitmap);
            }
            catch
            {
                return null;
            }
        }

        private static Icon CreateIconFromBitmap(Bitmap bitmap)
        {
            using var pngStream = new MemoryStream();
            bitmap.Save(pngStream, System.Drawing.Imaging.ImageFormat.Png);
            byte[] pngBytes = pngStream.ToArray();

            using var icoStream = new MemoryStream();
            icoStream.Write(new byte[] { 0, 0, 1, 0, 1, 0 }, 0, 6);

            byte width = (byte)(bitmap.Width >= 256 ? 0 : bitmap.Width);
            byte height = (byte)(bitmap.Height >= 256 ? 0 : bitmap.Height);

            icoStream.WriteByte(width);
            icoStream.WriteByte(height);
            icoStream.WriteByte(0);
            icoStream.WriteByte(0);
            icoStream.Write(BitConverter.GetBytes((short)1), 0, 2);
            icoStream.Write(BitConverter.GetBytes((short)32), 0, 2);
            icoStream.Write(BitConverter.GetBytes(pngBytes.Length), 0, 4);
            icoStream.Write(BitConverter.GetBytes(22), 0, 4);
            icoStream.Write(pngBytes, 0, pngBytes.Length);

            icoStream.Position = 0;
            using var icon = new Icon(icoStream);
            return (Icon)icon.Clone();
        }
    }
}
