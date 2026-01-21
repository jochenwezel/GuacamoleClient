using System;
using System.IO;
using System.Drawing;

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
                // PNG vollständig in Bytearray kopieren
                using var msPng = new MemoryStream();
                pngStream.CopyTo(msPng);
                byte[] pngBytes = msPng.ToArray();

                using var pngBitmap = new Bitmap(msPng);

                using var icoStream = new MemoryStream();

                // ICO HEADER (6 bytes)
                icoStream.Write(new byte[] { 0, 0, 1, 0, 1, 0 }, 0, 6);

                // ICON DIRECTORY ENTRY (16 bytes)
                byte width = (byte)(pngBitmap.Width >= 256 ? 0 : pngBitmap.Width);
                byte height = (byte)(pngBitmap.Height >= 256 ? 0 : pngBitmap.Height);

                icoStream.WriteByte(width);        // width
                icoStream.WriteByte(height);       // height
                icoStream.WriteByte(0);            // colors
                icoStream.WriteByte(0);            // reserved
                icoStream.Write(BitConverter.GetBytes((short)1), 0, 2);   // planes = 1
                icoStream.Write(BitConverter.GetBytes((short)32), 0, 2);  // bit depth = 32
                icoStream.Write(BitConverter.GetBytes(pngBytes.Length), 0, 4); // bytes in PNG
                icoStream.Write(BitConverter.GetBytes(22), 0, 4); // offset to PNG data

                // PNG-Daten anhängen
                icoStream.Write(pngBytes, 0, pngBytes.Length);

                icoStream.Position = 0;
                return new Icon(icoStream);
            }
            catch
            {
                return null;
            }
        }
    }
}
