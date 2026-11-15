// Port of ResourcePackConverter (MIT, agentdid127 and Hypixel).
using System;
using System.Buffers;
using System.IO;
using ImageMagick;

namespace ShulkerRDK.ResourceMagick.VersionConverter;

internal sealed class Raster : IDisposable {
    private IMagickImage<ushort> source;
    private IMagickImage<ushort> canvas;
    private readonly string location;
    private int defaultWidth;
    private int defaultHeight;
    private bool replacePixels;

    public Raster(int? defaultWidth, int? defaultHeight, string path) {
        source = new MagickImage(path);
        try {
            Normalize(source);
            this.defaultWidth = defaultWidth ?? checked((int)source.Width);
            this.defaultHeight = defaultHeight ?? checked((int)source.Height);
            if (this.defaultWidth <= 0 || this.defaultHeight <= 0)
                throw new InvalidDataException($"Invalid default image dimensions for '{path}'.");
        } catch {
            source.Dispose();
            throw;
        }
        canvas = source;
        location = path;
    }

    public int Width => checked((int)source.Width);
    public int Height => checked((int)source.Height);
    public double WidthMultiplier => (double)Width / defaultWidth;
    public double HeightMultiplier => (double)Height / defaultHeight;
    public bool IsPowerOfTwo => PowerOfTwo(Width) && PowerOfTwo(Height);
    public bool IsSquare => Width == Height;
    public int ScaleX(int x) => (int)(x * WidthMultiplier);
    public int ScaleY(int y) => (int)(y * HeightMultiplier);
    public int ScaleWidth(int width) => (int)(width * WidthMultiplier);
    public int ScaleHeight(int height) => (int)(height * HeightMultiplier);

    public void NewImage(int width, int height) {
        int w = ScaleWidth(width), h = ScaleHeight(height);
        if (width <= 0 || height <= 0 || w <= 0 || h <= 0)
            throw new InvalidDataException($"Invalid scaled canvas {width}x{height} for '{location}'.");
        ReplaceCanvas(new MagickImage(MagickColors.Transparent, (uint)w, (uint)h));
        replacePixels = false;
    }

    public void SubImage(int x, int y, int x2, int y2, int destinationX = 0, int destinationY = 0) =>
        CopyRegion(x, y, x2, y2, destinationX, destinationY, false, false);

    public void SubImage(int x, int y, int x2, int y2, int destinationX, int destinationY, bool flip) =>
        CopyRegion(x, y, x2, y2, destinationX, destinationY, flip, !flip);

    public void SubImage(int x, int y, int x2, int y2, int destinationX, int destinationY, int flip) =>
        CopyRegion(x, y, x2, y2, destinationX, destinationY, flip == 1, flip == 1);

    public void SubImageSized(int x, int y, int width, int height, int destinationX = 0, int destinationY = 0) =>
        SubImage(x, y, checked(x + width), checked(y + height), destinationX, destinationY);

    private void CopyRegion(int x, int y, int x2, int y2, int destinationX, int destinationY, bool vertical, bool horizontal) {
        int sx = ScaleX(x), sy = ScaleY(y);
        int width = (int)(x2 * WidthMultiplier - x * WidthMultiplier);
        int height = (int)(y2 * HeightMultiplier - y * HeightMultiplier);
        ValidateRegion(sx, sy, width, height);
        using var part = source.Clone(sx, sy, (uint)width, (uint)height);
        part.ResetPage();
        if (vertical) part.Flip();
        if (horizontal) part.Flop();
        // Java drawImage rounds destination offsets, while its scale helpers truncate.
        int dx = checked((int)Math.Floor((float)(destinationX * WidthMultiplier) + 0.5f));
        int dy = checked((int)Math.Floor((float)(destinationY * HeightMultiplier) + 0.5f));
        if (replacePixels) CopyClipped(part, dx, dy);
        else canvas.Composite(part, dx, dy, CompositeOperator.Over);
    }

    private void CopyClipped(IMagickImage<ushort> image, int x, int y) {
        int sx = Math.Max(0, -x), sy = Math.Max(0, -y);
        int width = Math.Min((int)image.Width - sx, (int)canvas.Width - Math.Max(0, x));
        int height = Math.Min((int)image.Height - sy, (int)canvas.Height - Math.Max(0, y));
        if (width <= 0 || height <= 0) return;
        canvas.CopyPixels(image, new MagickGeometry(sx, sy, (uint)width, (uint)height), Math.Max(0, x), Math.Max(0, y));
    }

    public void SaveSlice(int x, int y, int width, int height, string path) {
        var saved = canvas;
        bool savedReplace = replacePixels;
        // Keep the previous canvas alive while the temporary canvas is in use.
        canvas = source;
        try {
            NewImage(width, height);
            SubImageSized(x, y, width, height);
            Store(path);
        } finally {
            if (!ReferenceEquals(canvas, source) && !ReferenceEquals(canvas, saved)) canvas.Dispose();
            canvas = saved;
            replacePixels = savedReplace;
        }
    }

    public void FillEmpty(int x, int y, int width, int height) {
        if (!ReferenceEquals(canvas, source)) ReplaceCanvas(source.Clone());
        Normalize(canvas);
        int sx = Math.Max(0, ScaleX(x)), sy = Math.Max(0, ScaleY(y));
        int right = Math.Min((int)canvas.Width, checked(ScaleX(x) + ScaleWidth(width)));
        int bottom = Math.Min((int)canvas.Height, checked(ScaleY(y) + ScaleHeight(height)));
        if (right > sx && bottom > sy) {
            int count = checked((right - sx) * 4);
            ushort[] empty = ArrayPool<ushort>.Shared.Rent(count);
            try {
                empty.AsSpan(0, count).Clear();
                using var pixels = canvas.GetPixels();
                for (int row = sy; row < bottom; row++)
                    pixels.SetArea(sx, row, (uint)(right - sx), 1, empty.AsSpan(0, count));
            } finally { ArrayPool<ushort>.Shared.Return(empty); }
        }
        CommitSource();
        replacePixels = true;
    }

    public void SetImage(int defaultWidth, int defaultHeight) {
        if (defaultWidth <= 0 || defaultHeight <= 0) throw new ArgumentOutOfRangeException(nameof(defaultWidth));
        CommitSource();
        this.defaultWidth = defaultWidth;
        this.defaultHeight = defaultHeight;
    }

    private void CommitSource() {
        if (!ReferenceEquals(source, canvas)) source.Dispose();
        source = canvas;
    }

    public void AddImage(string path, int x, int y) {
        if (!File.Exists(path)) return;
        using var image = new MagickImage(path);
        Normalize(image);
        if (replacePixels) CopyClipped(image, ScaleX(x), ScaleY(y));
        else canvas.Composite(image, ScaleX(x), ScaleY(y), CompositeOperator.Over);
    }

    public void BackgroundImage(byte[] pngBytes, int alpha) {
        if ((uint)alpha > 255) throw new ArgumentOutOfRangeException(nameof(alpha));
        NewImage(defaultWidth, defaultHeight);
        Normalize(canvas);
        using var background = new MagickImage(pngBytes);
        Normalize(background);
        using var output = canvas.GetPixels();
        ushort[] values = output.GetValues();
        ReplaceNonempty(background, values, alpha);
        ReplaceNonempty(source, values, null);
        output.SetPixels(values);
    }

    private void ReplaceNonempty(IMagickImage<ushort> image, ushort[] output, int? alpha) {
        using var pixels = image.GetPixels();
        ushort[] input = pixels.GetValues();
        int width = Math.Min((int)canvas.Width, (int)image.Width);
        int height = Math.Min((int)canvas.Height, (int)image.Height);
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                int i = checked((y * (int)image.Width + x) * 4);
                if ((input[i] | input[i + 1] | input[i + 2] | input[i + 3]) == 0) continue;
                int o = checked((y * (int)canvas.Width + x) * 4);
                output[o] = input[i]; output[o + 1] = input[i + 1]; output[o + 2] = input[i + 2];
                output[o + 3] = alpha.HasValue ? (ushort)(alpha.Value * 257) : input[i + 3];
            }
        }
    }

    public void Grayscale() {
        // Both operations begin with the original source, not an earlier crop canvas.
        ReplaceCanvas(source.Clone());
        Normalize(canvas);
        using var pixels = canvas.GetPixels();
        ushort[] values = pixels.GetValues();
        for (int i = 0; i < values.Length; i += 4) {
            int gray = (int)(0.299 * (values[i] / 257) + 0.587 * (values[i + 1] / 257) + 0.114 * (values[i + 2] / 257));
            values[i] = values[i + 1] = values[i + 2] = (ushort)(gray * 257);
        }
        pixels.SetPixels(values);
    }

    public void ColorizeGrayscale(int r, int g, int b) {
        if ((uint)r > 255 || (uint)g > 255 || (uint)b > 255) throw new ArgumentOutOfRangeException(nameof(r));
        ReplaceCanvas(source.Clone());
        Normalize(canvas);
        using var pixels = canvas.GetPixels();
        ushort[] values = pixels.GetValues();
        for (int i = 0; i < values.Length; i += 4) {
            if (values[i + 3] == 0) continue;
            int gray = values[i] / 257;
            values[i] = (ushort)(gray * r / 255 * 257);
            values[i + 1] = (ushort)(gray * g / 255 * 257);
            values[i + 2] = (ushort)(gray * b / 255 * 257);
        }
        pixels.SetPixels(values);
    }

    public void Store(string path) {
        string? parent = Path.GetDirectoryName(Path.GetFullPath(path));
        if (parent != null) Directory.CreateDirectory(parent);
        // A failed encoder must never truncate the original texture.
        string temporary = Path.Combine(parent!, ".rpc-" + Guid.NewGuid().ToString("N") + ".png");
        try {
            canvas.Write(temporary, MagickFormat.Png);
            File.Move(temporary, path, true);
        } finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    public void Store() => Store(location);

    private void ValidateRegion(int x, int y, int width, int height) {
        if (x < 0 || y < 0 || width <= 0 || height <= 0 || (long)x + width > Width || (long)y + height > Height)
            throw new InvalidDataException($"Crop ({x},{y},{width},{height}) is outside '{location}' ({Width}x{Height}).");
    }
    private static bool PowerOfTwo(int value) => value > 0 && (value & (value - 1)) == 0;
    private static void Normalize(IMagickImage<ushort> image) {
        image.ColorSpace = ColorSpace.sRGB;
        image.ColorType = ColorType.TrueColorAlpha;
        image.Alpha(AlphaOption.On);
    }
    private void ReplaceCanvas(IMagickImage<ushort> replacement) {
        if (!ReferenceEquals(canvas, source)) canvas.Dispose();
        canvas = replacement;
    }
    public void Dispose() {
        if (!ReferenceEquals(canvas, source)) canvas.Dispose();
        source.Dispose();
    }
}
