using System;
using System.Collections.Generic;

namespace ISCSIApp.Models
{
    public class ISCSITarget
    {
        /// <summary>
        /// The iSCSI Qualified Name (IQN) of the target
        /// </summary>
        public string TargetName { get; set; }
        
        /// <summary>
        /// The IP address or hostname of the target portal
        /// </summary>
        public string PortalAddress { get; set; }
        
        /// <summary>
        /// The port number of the target portal (default is 3260)
        /// </summary>
        public int PortalPort { get; set; } = 3260;
        
        /// <summary>
        /// Indicates whether this target is currently connected
        /// </summary>
        public bool IsConnected { get; set; }
        
        /// <summary>
        /// The unique adapter part of the session ID when connected (0 when disconnected)
        /// </summary>
        public ulong SessionAdapterUnique { get; set; }

        /// <summary>
        /// The adapter-specific part of the session ID when connected (0 when disconnected)
        /// </summary>
        public ulong SessionAdapterSpecific { get; set; }

        /// <summary>
        /// The connection ID when connected (null when disconnected)
        /// </summary>
        public string ConnectionId { get; set; } // Keep ConnectionId as string for now, or change to ulong pair if needed
        
        /// <summary>
        /// The CHAP username for authentication (if required)
        /// </summary>
        public string ChapUsername { get; set; }
        
        /// <summary>
        /// Indicates whether this connection is persistent across reboots
        /// </summary>
        public bool IsPersistent { get; set; }
        
        /// <summary>
        /// The date and time when the target was last connected
        /// </summary>
        public DateTime? LastConnected { get; set; }
        
        /// <summary>
        /// Any additional properties or metadata for the target
        /// </summary>

        // --- MPIO Related Properties ---

        /// <summary>
        /// Indicates if MPIO is potentially enabled or configured for this target's LUNs.
        /// This might be set based on successful path retrieval.
        /// </summary>
        public bool IsMPIOEnabled { get; set; }

        /// <summary>
        /// The MPIO Disk ID associated with the LUNs presented by this target.
        /// This is often 0 when using the Microsoft DSM but can vary.
        /// Null if not determined or not applicable.
        /// </summary>
        public ulong? MpioDiskId { get; set; }

        /// <summary>
        /// List of MPIO paths discovered for this target's connection.
        /// Populated after a successful connection if MPIO is detected.
        /// </summary>
        // public List<MPIOPathInfo> Paths { get; set; } = new List<MPIOPathInfo>(); // Use ISCSIPath below
        public string Properties { get; set; }

        /// <summary>
        /// List of paths for Multi-Path I/O support
        /// </summary>
        public List<ISCSIPath> Paths { get; set; } = new List<ISCSIPath>();

        // Removed duplicate IsMPIOEnabled, the one under MPIO Related Properties is kept.
        
        /// <summary>
        /// The load balancing policy for Multi-Path I/O
        /// </summary>
        public MPIOPolicy LoadBalancePolicy { get; set; } = MPIOPolicy.RoundRobin;
        
        /// <summary>
        /// Indicates whether this is a target we created (vs. one we're connecting to)
        /// </summary>
        public bool IsLocalTarget { get; set; }
        
        /// <summary>
        /// The size of the target in GB (only applicable for local targets)
        /// </summary>
        public int SizeGB { get; set; }
        
        /// <summary>
        /// The backing storage path (only applicable for local targets)
        /// </summary>
        public string BackingStoragePath { get; set; }
    }
    
    public class ISCSIPath
    {
        /// <summary>
        /// The IP address or hostname of this path
        /// </summary>
        public string Address { get; set; }
        
        /// <summary>
        /// The port number for this path
        /// </summary>
        public int Port { get; set; } = 3260;
        
        /// <summary>
        /// The status of this path
        /// </summary>
        public PathStatus Status { get; set; } = PathStatus.Active;
        
        /// <summary>
        /// The session ID for this path when connected
        /// </summary>
        public string PathSessionId { get; set; }
    }
    
    public enum PathStatus
    {
        Active,
        Standby,
        Failed,
        Unavailable
    }
    
    public enum MPIOPolicy
    {
        RoundRobin,
        FailOver,
        LeastQueueDepth,
        WeightedPaths,
        LeastBlocks
    }
}