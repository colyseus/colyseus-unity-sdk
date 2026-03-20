using System;

namespace Colyseus
{
    /// <summary>
    /// Custom Preserve attribute that works on all platforms.
    /// Unity's IL2CPP linker recognizes any attribute named "Preserve" regardless of namespace.
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public sealed class PreserveAttribute : Attribute { }
}
