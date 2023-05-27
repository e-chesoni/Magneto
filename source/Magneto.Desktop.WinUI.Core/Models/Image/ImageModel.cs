using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magneto.Desktop.WinUI.Core.Models.Image;

/// <summary>
/// Wrapper for an image file
/// </summary>
public class ImageModel
{
    public Stack<Slice> sliceStack;

    public double thickness = 0;

    public string path_to_image { get; set; }

    public ImageModel()
    {
        // TODO: Get image from file path
        // TODO: Store image (not sure what file format to use)
    }
}
