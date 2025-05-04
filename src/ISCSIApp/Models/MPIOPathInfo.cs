using System;

namespace ISCSIApp.Models
{
    public class MPIOPathInfo
    {
        public ulong PathId { get; set; }
        public string ConnectionId { get; set; } // Storing as hex string for consistency
        public PathStatus State { get; set; }
        public uint BusNumber { get; set; }
        public uint TargetId { get; set; }
        public uint Lun { get; set; }
    }

    // Enum to represent MPIO Path Status flags (mirroring MPIO_PATH_STATUS)
    [Flags]
    public enum PathStatus : uint
    {
        Invalid = 0x00000000,
        Valid = 0x00000001,
        Active = 0x00000002,
        Standby = 0x00000004,
        Failed = 0x00000008,
        // Add other relevant flags from mpio.h if needed
    }
}