using System;

namespace LitRedis.Core.Models
{
    public class RequestLockModel
    {
        /// <summary>
        /// Gets or sets a value indicating the key to lock on.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets a value indicating how long to wait to grab the lock. specify null to wait forever.
        /// </summary>
        public TimeSpan? WaitTimeout { get; set; }

        /// <summary>
        /// Gets or sets a value indicating how often to renew the lock.
        /// </summary>
        public TimeSpan RenewLockInterval { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Gets or sets a value indicating how long to extend the lock each time it is renewed.
        /// </summary>
        public TimeSpan LockIncrease { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        ///  Sets a value indicating how long to extend the lock each time it is renewed.
        /// </summary>
        /// <param name="lockIncrease">How long to increase the lock for.</param>
        /// <returns></returns>
        public RequestLockModel WithLockIncrease(TimeSpan lockIncrease)
        {
            LockIncrease = lockIncrease;
            return this;
        }


        /// <summary>
        /// Sets value indicating how long to wait to grab the lock. specify null to wait forever.
        /// </summary>
        /// <param name="renewLockInterval">How long to wait to get the lock.</param>
        /// <returns>The request lock model</returns>
        public RequestLockModel WithLockWaitTimeout(TimeSpan waitTimeout)
        {
            WaitTimeout = waitTimeout;
            return this;
        }

        /// <summary>
        /// Sets a value indicating how often to renew the lock.
        /// </summary>
        /// <param name="renewLockInterval">How long to renew the lock for</param>
        /// <returns>The request lock model</returns>
        public RequestLockModel WithRenewLockInterval(TimeSpan renewLockInterval)
        {
            RenewLockInterval = renewLockInterval;
            return this;
        }

        /// <summary>
        /// Sets a value indicating to wait forever to try and get the lock.
        /// Use this if your logic on the lock must run.
        /// </summary>
        /// <returns>
        /// The request lock model.
        /// </returns>
        public RequestLockModel WaitForever()
        {
            WaitTimeout = null;
            return this;
        }

        /// <summary>
        /// Sets a value indicating to not wait for the lock. i.e. if the lock wasn't grabbed immediately, break out.
        /// Use this if your logic on the logic needs to move on if the lock is already acquired.
        /// </summary>
        /// <returns>
        /// The request lock model.
        /// </returns>
        public RequestLockModel NoWait()
        {
            WaitTimeout = TimeSpan.Zero;
            return this;
        }

        public static RequestLockModel WithKey(string key) => new() {Key = key};
    }
}
