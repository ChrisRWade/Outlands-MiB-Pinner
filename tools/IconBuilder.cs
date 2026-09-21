using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

internal static class IconBuilder
{
    private static readonly int[] IconSizes = { 16, 24, 32, 48, 64, 128, 256 };

    private static int Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.Error.WriteLine("Usage: IconBuilder.exe <output.ico> <preview.png>");
            return 2;
        }

        string iconDirectory = Path.GetDirectoryName(Path.GetFullPath(args[0]));
        string previewDirectory = Path.GetDirectoryName(Path.GetFullPath(args[1]));
        Directory.CreateDirectory(iconDirectory);
        Directory.CreateDirectory(previewDirectory);

        using (Bitmap source = DrawBoat())
        {
            WriteIcon(source, args[0]);
            using (Bitmap preview = ScalePixelArt(source, 256))
            {
                preview.Save(args[1], ImageFormat.Png);
            }
        }

        return 0;
    }

    private static Bitmap DrawBoat()
    {
        Bitmap bitmap = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
        using (Graphics graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.Transparent);
            graphics.SmoothingMode = SmoothingMode.None;
            graphics.PixelOffsetMode = PixelOffsetMode.Half;

            Color outline = Color.FromArgb(255, 12, 31, 39);
            Color deepWater = Color.FromArgb(255, 24, 57, 68);
            Color sea = Color.FromArgb(255, 43, 105, 116);
            Color foam = Color.FromArgb(255, 188, 220, 213);
            Color sail = Color.FromArgb(255, 244, 235, 207);
            Color sailShade = Color.FromArgb(255, 211, 196, 156);
            Color brass = Color.FromArgb(255, 190, 139, 59);
            Color hull = Color.FromArgb(255, 115, 68, 42);

            using (SolidBrush brush = new SolidBrush(deepWater))
            {
                graphics.FillRectangle(brush, 2, 1, 28, 30);
                graphics.FillRectangle(brush, 1, 2, 30, 28);
            }
            SetPixels(graphics, outline, new[] { 2, 1, 29, 1, 1, 2, 30, 2, 1, 29, 30, 29, 2, 30, 29, 30 });

            using (SolidBrush brush = new SolidBrush(sea))
            {
                graphics.FillRectangle(brush, 2, 23, 28, 7);
            }

            using (Pen mast = new Pen(outline, 3F))
            {
                graphics.DrawLine(mast, 16, 5, 16, 22);
            }
            using (Pen mast = new Pen(brass, 1F))
            {
                graphics.DrawLine(mast, 16, 5, 16, 22);
            }

            using (SolidBrush brush = new SolidBrush(brass))
            {
                graphics.FillRectangle(brush, 18, 5, 6, 3);
                graphics.FillRectangle(brush, 18, 8, 3, 1);
            }

            using (SolidBrush brush = new SolidBrush(sail))
            {
                graphics.FillPolygon(brush, new[] { new Point(14, 7), new Point(14, 20), new Point(5, 20) });
                graphics.FillPolygon(brush, new[] { new Point(18, 9), new Point(18, 20), new Point(27, 20) });
            }
            using (SolidBrush brush = new SolidBrush(sailShade))
            {
                graphics.FillPolygon(brush, new[] { new Point(12, 12), new Point(12, 19), new Point(7, 19) });
                graphics.FillRectangle(brush, 19, 18, 6, 2);
            }

            using (Pen sailOutline = new Pen(outline, 1F))
            {
                graphics.DrawLine(sailOutline, 14, 7, 14, 20);
                graphics.DrawLine(sailOutline, 14, 7, 5, 20);
                graphics.DrawLine(sailOutline, 5, 20, 14, 20);
                graphics.DrawLine(sailOutline, 18, 9, 18, 20);
                graphics.DrawLine(sailOutline, 18, 9, 27, 20);
                graphics.DrawLine(sailOutline, 18, 20, 27, 20);
            }

            using (SolidBrush brush = new SolidBrush(outline))
            {
                graphics.FillPolygon(brush, new[] { new Point(4, 20), new Point(29, 20), new Point(25, 26), new Point(9, 26) });
            }
            using (SolidBrush brush = new SolidBrush(hull))
            {
                graphics.FillPolygon(brush, new[] { new Point(6, 21), new Point(27, 21), new Point(24, 24), new Point(9, 24) });
            }
            using (SolidBrush brush = new SolidBrush(brass))
            {
                graphics.FillRectangle(brush, 9, 21, 15, 1);
            }

            SetPixels(graphics, foam, new[] { 3, 27, 4, 27, 9, 28, 10, 28, 15, 27, 16, 27, 22, 28, 23, 28, 27, 26, 28, 26 });
        }

        return bitmap;
    }

    private static void SetPixels(Graphics graphics, Color color, int[] coordinates)
    {
        using (SolidBrush brush = new SolidBrush(color))
        {
            for (int index = 0; index < coordinates.Length; index += 2)
            {
                graphics.FillRectangle(brush, coordinates[index], coordinates[index + 1], 1, 1);
            }
        }
    }

    private static Bitmap ScalePixelArt(Bitmap source, int size)
    {
        Bitmap scaled = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (Graphics graphics = Graphics.FromImage(scaled))
        {
            graphics.Clear(Color.Transparent);
            graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            graphics.PixelOffsetMode = PixelOffsetMode.Half;
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.DrawImage(source, new Rectangle(0, 0, size, size), 0, 0, source.Width, source.Height, GraphicsUnit.Pixel);
        }
        return scaled;
    }

    private static void WriteIcon(Bitmap source, string outputPath)
    {
        List<byte[]> frames = new List<byte[]>();
        foreach (int size in IconSizes)
        {
            using (Bitmap frame = ScalePixelArt(source, size))
            using (MemoryStream stream = new MemoryStream())
            {
                frame.Save(stream, ImageFormat.Png);
                frames.Add(stream.ToArray());
            }
        }

        using (FileStream stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            writer.Write((ushort)0);
            writer.Write((ushort)1);
            writer.Write((ushort)frames.Count);

            int offset = 6 + (16 * frames.Count);
            for (int index = 0; index < frames.Count; index++)
            {
                int size = IconSizes[index];
                writer.Write((byte)(size == 256 ? 0 : size));
                writer.Write((byte)(size == 256 ? 0 : size));
                writer.Write((byte)0);
                writer.Write((byte)0);
                writer.Write((ushort)1);
                writer.Write((ushort)32);
                writer.Write((uint)frames[index].Length);
                writer.Write((uint)offset);
                offset += frames[index].Length;
            }

            foreach (byte[] frame in frames)
            {
                writer.Write(frame);
            }
        }
    }
}

