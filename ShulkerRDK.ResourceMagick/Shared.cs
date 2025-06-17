using System.Text.RegularExpressions;
using ImageMagick;
using ImageMagick.Formats;

namespace ShulkerRDK.ResourceMagick;

public static class Shared { 
    public static MagickImage? GetMergedLayers(string inputPsdPath, string regex) {
        using MagickImageCollection images = new MagickImageCollection();
        MagickReadSettings settings = new MagickReadSettings {
            Defines = new PsdReadDefines {
                PreserveOpacityMask = true
            }
        };
        images.Read(inputPsdPath,settings);
        List<IMagickImage<ushort>> capturedLayers = [];
        foreach (IMagickImage<ushort> image in images) {
            if (image.Label != null && new Regex(regex).IsMatch(image.Label)) {
                capturedLayers.Add(image);
            }
        }
        if (capturedLayers.Count == 0) {
            return null;
        }
        MagickImage result = new MagickImage(MagickColors.Transparent,images[0].Width,images[0].Height);
        foreach (IMagickImage<ushort> layer in capturedLayers) {
            result.Composite(layer,layer.Page.X,layer.Page.Y,CompositeOperator.Over);
            layer.Dispose();
        }
        return result;
    }
}