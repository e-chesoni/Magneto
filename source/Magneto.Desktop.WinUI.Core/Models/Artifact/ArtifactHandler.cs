using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magneto.Desktop.WinUI.Core.Models.Artifact;

/// <summary>
/// Class to handle image processing (i.e. slicing an image)
/// </summary>
public static class ArtifactHandler
{
    /// <summary>
    /// Slices image
    /// </summary>
    /// <returns></returns> Returns a list of sliced images
    public static Stack<Slice> SliceArtifact(ArtifactModel im)
    {
        var msg = "I don't really know how to handle this image model yet...";
        MagnetoLogger.Log(msg, Contracts.Services.LogFactoryLogLevel.LogLevel.WARN);

        Stack<Slice> imageSlices = new Stack<Slice>();

        // TODO: Slice image and add slices to image_slices
        msg = "Making fake slices to test functionality...";
        MagnetoLogger.Log(msg, Contracts.Services.LogFactoryLogLevel.LogLevel.WARN);

        // TODO: Get number of slices from image model (instead of using default from config)
        for (int i = 0; i <= MagnetoConfig.GetDefaultNumSlices(); i++)
        {
            imageSlices.Push(new Slice());
        }

        return imageSlices;
    }
}
