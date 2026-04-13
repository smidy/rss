using SkiaSharp;

namespace Rss.Services;

public static class ImageService
{
    private const int MaxDimension = 1024;

    private static readonly HashSet<string> ImageExtensions =
        [".jpg", ".jpeg", ".png", ".gif", ".webp", ".avif", ".bmp"];

    /// <summary>
    /// Returns true if the URL path ends with a known image extension.
    /// </summary>
    public static bool IsImageUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        var ext = Path.GetExtension(uri.AbsolutePath).ToLowerInvariant();
        return ImageExtensions.Contains(ext);
    }

    /// <summary>
    /// Downloads an image from a URL, resizes it, and returns the bytes + media type.
    /// </summary>
    public static async Task<(byte[] Bytes, string MediaType)> FetchAndPrepareAsync(string url)
    {
        using var http = new HttpClient();
        var raw = await http.GetByteArrayAsync(url);
        return ResizeIfNeeded(raw);
    }

    /// <summary>
    /// Reads an image file and resizes it so the longest side is at most 1024px.
    /// </summary>
    public static (byte[] Bytes, string MediaType) PrepareImage(string path)
    {
        var raw = File.ReadAllBytes(path);
        return ResizeIfNeeded(raw);
    }

    /// <summary>
    /// Resizes raw image bytes so the longest side is at most 1024px.
    /// Re-encodes as JPEG regardless, for consistent media type.
    /// </summary>
    public static (byte[] Bytes, string MediaType) ResizeIfNeeded(byte[] input)
    {
        using var bitmap = SKBitmap.Decode(input)
            ?? throw new InvalidOperationException("Could not decode image.");

        SKBitmap target;

        if (bitmap.Width <= MaxDimension && bitmap.Height <= MaxDimension)
        {
            target = bitmap;
        }
        else
        {
            float scale = Math.Min((float)MaxDimension / bitmap.Width,
                                   (float)MaxDimension / bitmap.Height);

            var info = new SKImageInfo(
                (int)(bitmap.Width * scale),
                (int)(bitmap.Height * scale));

            target = bitmap.Resize(info, SKSamplingOptions.Default)
                ?? throw new InvalidOperationException("Image resize failed.");
        }

        using var image = SKImage.FromBitmap(target);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, quality: 85);

        if (!ReferenceEquals(target, bitmap))
            target.Dispose();

        return (data.ToArray(), "image/jpeg");
    }
}
