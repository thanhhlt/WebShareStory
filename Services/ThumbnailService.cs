using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.Fonts;

public interface IThumbnailService
{
    byte[] GenerateThumbnail(string title, int width = 350, int height = 200);
}

public class ThumbnailService : IThumbnailService
{
    public byte[] GenerateThumbnail(string title, int width = 150, int height = 150)
    {
        using var image = new Image<Rgba32>(width, height);

        var hash = title.GetHashCode();
        var random = new Random(hash);
        var backgroundColor = Color.FromRgb(
            (byte)random.Next(0, 100),
            (byte)random.Next(0, 100),
            (byte)random.Next(0, 100)
        );
        image.Mutate(ctx => ctx.Fill(backgroundColor));

        string text = title.Length > 0 ? title.Substring(0, 1).ToUpper() : "T";
        var font = SystemFonts.CreateFont("Arial", 72, FontStyle.Bold);
        var textColor = Color.White;

        var textOptions = new RichTextOptions(font)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Origin = new PointF(width / 2, height / 2)
        };
        image.Mutate(ctx => ctx.DrawText(textOptions, text, textColor));

        using var stream = new MemoryStream();
        image.SaveAsPng(stream);
        return stream.ToArray();
    }
}
