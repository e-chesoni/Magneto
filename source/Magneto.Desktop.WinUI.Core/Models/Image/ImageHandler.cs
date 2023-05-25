using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Services;

namespace Magneto.Desktop.WinUI.Core.Models.Image;

/// <summary>
/// Class to handle image processing (i.e. slicing an image)
/// </summary>
public static class ImageHandler
{
    /// <summary>
    /// Slices image
    /// </summary>
    /// <returns></returns> Returns a list of sliced images
    public static Stack<Slice> SliceImage(ImageModel im)
    {
        MagnetoLogger.Log("ImageHander::SliceImage -- I don't really know how to handle this image model yet...",
            Contracts.Services.LogFactoryLogLevel.LogLevel.WARN);

        Stack<Slice> image_slices = new Stack<Slice>();

        // TODO: Slice image and add slices to image_slices
        MagnetoLogger.Log("ImageHander::SliceImage -- Making fake slices to test functionality...",
            Contracts.Services.LogFactoryLogLevel.LogLevel.WARN);
        image_slices.Push(new Slice());
        image_slices.Push(new Slice());
        image_slices.Push(new Slice());

        return image_slices;
    }
}
