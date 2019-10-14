﻿namespace RedisTribute.Types
{
    /// <summary>
    /// Options for setting a key value pair.
    /// </summary>
    /// <remarks>
    /// EX seconds -- Set the specified expire time, in seconds.
    /// PX milliseconds -- Set the specified expire time, in milliseconds.
    /// NX -- Only set the key if it does not already exist.
    /// XX -- Only set the key if it already exist.
    /// </remarks>
    public readonly struct SetOptions
    {
        public SetOptions(Expiry expiry, SetCondition condition)
        {
            Expiry = expiry;
            Condition = condition;
        }

        public Expiry Expiry { get; }

        public SetCondition Condition { get; }

        public static implicit operator SetOptions(Expiry x) => new SetOptions(x, SetCondition.Default);

        public static implicit operator SetOptions(SetCondition x) => new SetOptions(Expiry.Infinite, x);
    }

    public enum SetCondition
    {
        Default = 0,
        NX_SetKeyIfNotExists = 1,
        XX_SetKeyOnlyIfExists = 2
    }
}
