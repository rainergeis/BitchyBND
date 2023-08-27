using System;

namespace SoulsFormats;

/// <summary>
///     Indicates whether the alpha component of a Color is used.
/// </summary>
[AttributeUsage(AttributeTargets.All)]
public class SupportsAlphaAttribute : Attribute
{

    /// <summary>
    ///     Creates an attribute with the given value.
    /// </summary>
    public SupportsAlphaAttribute(bool supports)
    {
        Supports = supports;
    }

    /// <summary>
    ///     If true, alpha is used; if false, alpha is ignored.
    /// </summary>
    public bool Supports { get; }
}