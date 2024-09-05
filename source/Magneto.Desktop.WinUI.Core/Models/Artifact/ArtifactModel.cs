using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magneto.Desktop.WinUI.Core.Models.Artifact;

/// <summary>
/// Wrapper for an image file
/// </summary>
public class ArtifactModel
{
    #region Public Variables
    /// <summary>
    /// Stack of slices associated with an image
    /// </summary>
    public Stack<Slice> sliceStack;

    public double defaultThickness;

    /// <summary>
    /// Path to the image associated with this image model
    /// (Stored at time of construction)
    /// </summary>
    public string pathToArtifact { get; set; }

    #endregion

    #region Constructors

    /// <summary>
    /// Artifact model constructor
    /// </summary>
    public ArtifactModel()
    {
        // TODO: Get image from file path
        // TODO: Store image (not sure what file format to use)
        defaultThickness = MagnetoConfig.GetDefaultPrintThickness();
    }

    /// <summary>
    /// Artifact model constructor that accepts a path to the artifact file
    /// </summary>
    /// <param name="path"> file path to the artifact </param>
    public ArtifactModel(string filePath)
    {
        // TODO: Get image from file path
        pathToArtifact = filePath;

        // TODO: Store image (not sure what file format to use)
        defaultThickness = MagnetoConfig.GetDefaultPrintThickness();
    }

    #endregion
}
