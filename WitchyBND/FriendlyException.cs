using System;

namespace WitchyBND;

internal class FriendlyException : Exception
{
    public FriendlyException(string message) : base(message)
    {
    }
}