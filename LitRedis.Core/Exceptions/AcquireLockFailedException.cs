using System;

namespace LitRedis.Core.Exceptions
{
    public class AcquireLockFailedException : Exception
    {
        public AcquireLockFailedException(TimeSpan waitTime)
            : base($"Failed to acquire lock after waiting {waitTime}") { }
    }
}
