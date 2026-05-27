using System;

namespace LitRedis.Core.Exceptions;

public class LockLostException : Exception
{
    public LockLostException()
        : base("The distributed lock was lost before the operation completed.") { }

    public LockLostException(string key)
        : base($"The distributed lock for key '{key}' was lost before the operation completed.") { }

    public LockLostException(string message, Exception innerException) : base(message, innerException) { }
}
