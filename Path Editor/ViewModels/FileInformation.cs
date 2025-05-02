using System.IO;

namespace NobleTech.Products.PathEditor.ViewModels;

/// <summary>
/// The combination of a file path and a save function that either is about to be or was last used to save this file.
/// </summary>
/// <param name="Path">The path to the file.</param>
/// <param name="Save">The function to save drawn paths to a stream.</param>
internal record FileInformation(string Path, Action<DrawnPaths, Stream, string> Save);
