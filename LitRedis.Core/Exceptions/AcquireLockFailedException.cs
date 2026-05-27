using System;

namespace LitRedis.Core.Exceptions;

public class AcquireLockFailedException : Exception
{
    public AcquireLockFailedException(string key, TimeSpan waitTime)
        : base($"Failed to acquire lock for key '{key}' after waiting {waitTime}") { }
}
