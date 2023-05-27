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
    #region Public Variables
    /// <summary>
    /// Stack of slices associated with an image
    /// </summary>
    public Stack<Slice> sliceStack;

    /// <summary>
    /// Desired thickness for the printing of this image model
    /// </summary>
    public double thickness = 0;

    /// <summary>
    /// Path to the image associated with this image model
    /// (Stored at time of construction)
    /// </summary>
    public string path_to_image { get; set; }

    #endregion

    #region Constructor

    /// <summary>
    /// Image model constructor
    /// </summary>
    public ImageModel()
    {
        // TODO: Get image from file path
        // TODO: Store image (not sure what file format to use)
    }

    #endregion
}
