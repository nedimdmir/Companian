// Generates assets/companian.ico (multi-size) and assets/logo.png from vector drawing code.
// Usage: IconGen.exe <out.ico> [out.png]
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

internal static class IconGen
{
    private static int Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.Error.WriteLine("usage: IconGen <out.ico> [out.png]");
            return 1;
        }

        int[] sizes = { 16, 20, 24, 32, 40, 48, 64, 128, 256 };
        List<byte[]> images = new List<byte[]>();
        foreach (int s in sizes)
        {
            using (Bitmap b = Draw(s))
            {
                images.Add(s >= 256 ? Png(b) : Dib(b));
                if (s == 256 && args.Length > 1) b.Save(args[1], ImageFormat.Png);
            }
        }

        using (BinaryWriter w = new BinaryWriter(File.Create(args[0])))
        {
            w.Write((short)0);            // reserved
            w.Write((short)1);            // type: icon
            w.Write((short)sizes.Length);
            int offset = 6 + 16 * sizes.Length;
            for (int i = 0; i < sizes.Length; i++)
            {
                w.Write((byte)(sizes[i] >= 256 ? 0 : sizes[i]));
                w.Write((byte)(sizes[i] >= 256 ? 0 : sizes[i]));
                w.Write((byte)0);         // palette
                w.Write((byte)0);         // reserved
                w.Write((short)1);        // planes
                w.Write((short)32);       // bpp
                w.Write(images[i].Length);
                w.Write(offset);
                offset += images[i].Length;
            }
            foreach (byte[] img in images) w.Write(img);
        }
        return 0;
    }

    private static Bitmap Draw(int s)
    {
        Bitmap bmp = new Bitmap(s, s, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.Clear(Color.Transparent);

            float pad = s * 0.04f;
            RectangleF r = new RectangleF(pad, pad, s - 2 * pad, s - 2 * pad);
            using (GraphicsPath path = RoundRect(r, s * 0.22f))
            using (LinearGradientBrush br = new LinearGradientBrush(new PointF(0, 0), new PointF(s, s),
                       Color.FromArgb(79, 140, 255), Color.FromArgb(124, 77, 255)))
            {
                g.FillPath(br, path);
            }

            float[,] arrow =
            {
                { 0.36f, 0.25f }, { 0.36f, 0.79f }, { 0.48f, 0.67f }, { 0.58f, 0.87f },
                { 0.66f, 0.83f }, { 0.56f, 0.63f }, { 0.73f, 0.63f }
            };
            PointF[] pts = new PointF[arrow.GetLength(0)];
            for (int i = 0; i < pts.Length; i++) pts[i] = new PointF(arrow[i, 0] * s, arrow[i, 1] * s);
            using (SolidBrush white = new SolidBrush(Color.White))
                g.FillPolygon(white, pts);

            if (s >= 32)
            {
                using (Pen pen = new Pen(Color.FromArgb(235, 255, 255, 255), s * 0.045f))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    g.DrawLine(pen, 0.36f * s, 0.17f * s, 0.36f * s, 0.11f * s);
                    g.DrawLine(pen, 0.28f * s, 0.25f * s, 0.21f * s, 0.25f * s);
                    g.DrawLine(pen, 0.30f * s, 0.19f * s, 0.25f * s, 0.14f * s);
                }
            }
        }
        return bmp;
    }

    private static GraphicsPath RoundRect(RectangleF r, float radius)
    {
        float d = radius * 2;
        GraphicsPath p = new GraphicsPath();
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    private static byte[] Png(Bitmap b)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            b.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        }
    }

    /// <summary>32-bit BGRA DIB with an empty AND mask, as stored inside .ico files.</summary>
    private static byte[] Dib(Bitmap b)
    {
        int w = b.Width, h = b.Height;
        BitmapData data = b.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        byte[] pixels = new byte[w * h * 4];
        for (int y = 0; y < h; y++)
            Marshal.Copy(new IntPtr(data.Scan0.ToInt64() + (long)y * data.Stride), pixels, y * w * 4, w * 4);
        b.UnlockBits(data);

        int maskStride = ((w + 31) / 32) * 4;
        using (MemoryStream ms = new MemoryStream())
        using (BinaryWriter bw = new BinaryWriter(ms))
        {
            bw.Write(40);                 // BITMAPINFOHEADER size
            bw.Write(w);
            bw.Write(h * 2);              // XOR + AND masks
            bw.Write((short)1);
            bw.Write((short)32);
            bw.Write(0);                  // BI_RGB
            bw.Write(w * h * 4 + maskStride * h);
            bw.Write(0); bw.Write(0); bw.Write(0); bw.Write(0);
            for (int y = h - 1; y >= 0; y--) bw.Write(pixels, y * w * 4, w * 4); // bottom-up
            bw.Write(new byte[maskStride * h]);
            return ms.ToArray();
        }
    }
}
