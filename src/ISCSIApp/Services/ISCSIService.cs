using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ISCSIApp.Models;
using System.ComponentModel;

namespace ISCSIApp.Services
{
    public class ISCSIService
    {
        // Constants for API calls
        private const int ERROR_SUCCESS = 0;
        private const int ERROR_INSUFFICIENT_BUFFER = 122;

        // --- P/Invoke Signatures for iscsidsc.dll ---

        [DllImport("iscsidsc.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint GetIScsiInitiatorNodeName(StringBuilder InitiatorNodeName, ref uint InitiatorNodeNameLength);

        [DllImport("iscsidsc.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint ReportIScsiTargetsW(
            [MarshalAs(UnmanagedType.Bool)] bool ForceUpdate,
            ref uint BufferSize,
            StringBuilder Buffer);

        [DllImport("iscsidsc.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint LoginIScsiTargetW(
            string TargetName,
            [MarshalAs(UnmanagedType.Bool)] bool IsInformationalSession,
            string InitiatorInstance, // Optional: Can be null
            uint InitiatorPortNumber, // Optional: Use ISCSI_ANY_INITIATOR_PORT (0xFFFFFFFF)
            ref ISCSI_TARGET_PORTALW TargetPortal, // Optional: Can pass IntPtr.Zero if null
            ISCSI_SECURITY_FLAGS SecurityFlags, // Optional: Use 0 if not needed
            IntPtr Mappings, // Optional: Use IntPtr.Zero
            ref ISCSI_LOGIN_OPTIONS LoginOptions, // Optional: Can pass IntPtr.Zero if null
            uint KeySize, // Optional: Use 0
            string Key, // Optional: Use null
            [MarshalAs(UnmanagedType.Bool)] bool IsPersistent,
            out ISCSI_UNIQUE_SESSION_ID UniqueSessionId,
            out ISCSI_UNIQUE_CONNECTION_ID UniqueConnectionId);

        [DllImport("iscsidsc.dll", SetLastError = true)]
        private static extern uint LogoutIScsiTarget(ref ISCSI_UNIQUE_SESSION_ID UniqueSessionId);

        [DllImport("iscsidsc.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint GetIScsiSessionListW(
            ref uint BufferSize,
            ref uint SessionCount,
            IntPtr SessionInfo); // Pointer to array of ISCSI_SESSION_INFOW

        // Placeholder for MPIO APIs
        // [DllImport("mpio.dll")] ...

        // --- P/Invoke Signatures for MPIO (mpio.dll) ---

        // Constants for MPIO Load Balance Policies
        public const uint MPIO_DSM_INVALID = 0;
        public const uint MPIO_DSM_FAIL_OVER = 1;
        public const uint MPIO_DSM_ROUND_ROBIN = 2;
        public const uint MPIO_DSM_ROUND_ROBIN_WITH_SUBSET = 3;
        public const uint MPIO_DSM_DYN_LEAST_QUEUE_DEPTH = 4;
        public const uint MPIO_DSM_WEIGHTED_PATHS = 5;
        public const uint MPIO_DSM_LEAST_BLOCKS = 6;
        public const uint MPIO_DSM_VENDOR_SPECIFIC = 7;

        [DllImport("mpio.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern uint MpioRegisterDevice(
            ulong MpioDiskId, // Often 0 for iSCSI LUNs managed by MS DSM
            string DeviceName // The device path (e.g., \\?\PhysicalDriveX)
        );

        [DllImport("mpio.dll", SetLastError = true)]
        private static extern uint MpioGetDeviceList(
            out uint NumberDevices,
            out IntPtr Devices // Pointer to array of MPIO_DEVINSTANCE_INFO
        );

        [DllImport("mpio.dll", SetLastError = true)]
        private static extern uint MpioGetPathInfo(
            ulong MpioDiskId, // Often 0
            ref uint NumberPaths,
            out IntPtr Paths // Pointer to array of MPIO_PATH_INFO
        );

        [DllImport("mpio.dll", SetLastError = true)]
        private static extern uint MpioSetLoadBalancePolicy(
            ulong MpioDiskId, // Often 0
            ref DSM_LOAD_BALANCE_POLICY LoadBalancePolicy
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr LocalFree(IntPtr hMem);

        // Placeholder for LoginIScsiTarget - Requires complex structures
        // [DllImport("iscsidsc.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        // private static extern uint LoginIScsiTarget(...);

        // Placeholder for LogoutIScsiTarget - Requires SessionId structure
        // [DllImport("iscsidsc.dll", SetLastError = true)]
        // private static extern uint LogoutIScsiTarget(...);

        // Placeholder for GetConnectedTargets - Need API like ReportIScsiSessions
        // [DllImport("iscsidsc.dll", SetLastError = true)]
        // private static extern uint ReportIScsiSessions(...);

        // --- P/Invoke Signatures for MPIO (mpio.dll) - Need Verification ---
        // [DllImport("mpio.dll")]
        // private static extern uint GetMPIOPathCount(IntPtr deviceName, out uint pathCount);
        // [DllImport("mpio.dll")]
        // private static extern uint GetMPIOPathInfo(IntPtr deviceName, uint pathIndex, out IntPtr pathInfo);
        // [DllImport("mpio.dll")]
        // private static extern uint SetMPIOLoadBalancePolicy(IntPtr deviceName, uint policy);

        // --- P/Invoke Signatures for Target Creation (iscsitgt.dll) - Requires iSCSI Target Server Role ---
        // --- P/Invoke Signatures for Target Creation (iscsitgt.dll) - Requires iSCSI Target Server Role ---
        // Note: These require the iSCSI Target Server role to be installed and the service running.
        // The exact signatures might vary slightly based on the Windows version and SDK.
        // Using IntPtr for target handles is common, but verify documentation.

        [DllImport("iscsitgt.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint CreateIScsiTarget(string TargetName, string TargetDescription, IntPtr LunMappings, uint LunCount, out IntPtr UniqueTargetId);

        [DllImport("iscsitgt.dll", SetLastError = true)]
        private static extern uint DeleteIScsiTarget(IntPtr UniqueTargetId);

        // Simplified example; actual security configuration might be more complex
        [DllImport("iscsitgt.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint SetIScsiTargetCHAPInfo(IntPtr UniqueTargetId, bool EnableCHAP, string CHAPUserName, string CHAPSecret);

        // --- Structures for P/Invoke ---

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct ISCSI_TARGET_PORTALW
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256 + 1)]
            public string Address;
            public ushort Socket;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ISCSI_LOGIN_OPTIONS
        {
            public uint Version; // Should be 0
            public ISCSI_LOGIN_OPTIONS_INFO_SPECIFIED InformationSpecified;
            public ISCSI_LOGIN_AUTH_TYPES AuthType;
            public ISCSI_LOGIN_FLAGS HeaderDigest;
            public ISCSI_LOGIN_FLAGS DataDigest;
            public uint MaximumConnections;
            public uint DefaultTime2Wait;
            public uint DefaultTime2Retain;
            public uint LoginFlags;
            public uint Reserved3;
        }

        [Flags]
        private enum ISCSI_LOGIN_OPTIONS_INFO_SPECIFIED : uint
        {
            AuthType = 0x00000001,
            HeaderDigest = 0x00000002,
            DataDigest = 0x00000004,
            MaximumConnections = 0x00000008,
            DefaultTime2Wait = 0x00000010,
            DefaultTime2Retain = 0x00000020,
            LoginFlags = 0x00000040
        }

        private enum ISCSI_LOGIN_AUTH_TYPES : uint
        {
            None = 0,
            OneWayCHAP = 1,
            MutualCHAP = 2
        }

        private enum ISCSI_LOGIN_FLAGS : uint
        {
            None = 0,
            Require = 1,
            Prefer = 2
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ISCSI_UNIQUE_SESSION_ID
        {
            public ulong AdapterUnique;
            public ulong AdapterSpecific;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ISCSI_UNIQUE_CONNECTION_ID
        {
            public ulong AdapterUnique;
            public ulong AdapterSpecific;
        }

        [Flags]
        private enum ISCSI_SECURITY_FLAGS : ulong
        {
            // Define flags as needed, e.g., for IPsec. Use 0 if not using IPsec.
            None = 0
        }

        private const uint ISCSI_ANY_INITIATOR_PORT = 0xFFFFFFFF;

        // --- MPIO Structures ---

        [StructLayout(LayoutKind.Sequential)]
        private struct MPIO_DEVINSTANCE_INFO
        {
            public uint NumberPaths;
            public ulong MpioDiskId; // Use this ID for other MPIO calls
            // Other fields might exist depending on the specific Windows version/SDK
            // Add them here if needed based on documentation
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MPIO_PATH_INFO
        {
            public ulong PathId;
            public ulong ConnectionId; // Correlates to ISCSI_UNIQUE_SESSION_ID.AdapterSpecific
            public byte ScsiAddressTargetId;
            public byte ScsiAddressBusNumber;
            public byte ScsiAddressLun;
            // Other fields like PathState, Weight, etc.
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DSM_LOAD_BALANCE_POLICY
        {
            public uint Version; // Should be 1
            public uint LoadBalancePolicy; // Use MPIO_DSM_ constants
            public uint Reserved1; // Must be 0
            public ulong Reserved2; // Must be 0
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct ISCSI_SESSION_INFOW
        {
            public ISCSI_UNIQUE_SESSION_ID SessionId;
            public IntPtr InitiatorName; // PWSTR
            public IntPtr TargetNodeName; // PWSTR
            public IntPtr TargetName; // PWSTR
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public byte[] ISID; // UCHAR[6]
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public byte[] TSID; // UCHAR[2]
            public uint ConnectionCount;
            public IntPtr Connections; // PISCSI_CONNECTION_INFOW
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct ISCSI_CONNECTION_INFOW
        {
            public ISCSI_UNIQUE_CONNECTION_ID ConnectionId;
            public IntPtr InitiatorAddress; // PWSTR
            public IntPtr TargetAddress; // PWSTR
            public ushort InitiatorSocket;
            public ushort TargetSocket;
            public byte CID; // UCHAR
            // Add other fields if needed based on documentation
        }

        // --- Structures for MPIO ---

        [StructLayout(LayoutKind.Sequential)]
        private struct MPIO_DEVINSTANCE_INFO
        {
            public uint NumberPaths;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 63 + 1)]
            public string DeviceName; // Instance name
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 63 + 1)]
            public string TargetName; // iSCSI Target Name
            public ulong MpioDiskId; // Identifier used in other MPIO calls
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MPIO_PATH_INFO
        {
            public ulong PathId;
            public uint Reserved1;
            public uint PathState; // Defined by MPIO_PATH_STATUS flags
            public uint BusNumber;
            public uint TargetId;
            public uint Lun;
            public uint Reserved2;
            public ulong ConnectionId; // Matches ISCSI_UNIQUE_CONNECTION_ID.AdapterSpecific
            // Add other fields if needed based on documentation
        }

        // MPIO_PATH_STATUS flags (example, need full list)
        private const uint MPIO_PATH_VALID = 0x00000001;
        private const uint MPIO_PATH_ACTIVE = 0x00000002;
        private const uint MPIO_PATH_STANDBY = 0x00000004;
        private const uint MPIO_PATH_FAILED = 0x00000008;


        [StructLayout(LayoutKind.Sequential)]
        private struct DSM_LOAD_BALANCE_POLICY
        {
            public uint Version; // Should be 1
            public uint LoadBalancePolicy; // Use MPIO_DSM_ constants
            public uint Reserved1;
            public uint Reserved2;
            public ulong Reserved3;
        }


        // --- Service Methods ---

        public async Task<string> GetInitiatorNameAsync()
        {
            return await Task.Run(() =>
            {
                uint bufferSize = 0;
                // First call to get the required buffer size
                uint result = GetIScsiInitiatorNodeName(null, ref bufferSize);

                if (result == ERROR_INSUFFICIENT_BUFFER)
                {
                    StringBuilder initiatorName = new StringBuilder((int)bufferSize);
                    // Second call with the allocated buffer
                    result = GetIScsiInitiatorNodeName(initiatorName, ref bufferSize);

                    if (result == ERROR_SUCCESS)
                    {
                        return initiatorName.ToString();
                    }
                    else
                    {
                        // Throw detailed exception
                        throw new Win32Exception((int)result, $"Failed to get iSCSI Initiator Name. Error code: {result}");
                    }
                }
                else if (result == ERROR_SUCCESS)
                {
                    // This case should ideally not happen if the API behaves as expected
                    // (returning ERROR_INSUFFICIENT_BUFFER for null buffer), but handle it just in case.
                    return string.Empty; 
                }
                else
                {
                    // Throw detailed exception for other errors on the first call
                    throw new Win32Exception((int)result, $"Failed to get buffer size for iSCSI Initiator Name. Error code: {result}");
                }
            });
        }

        // --- MPIO Service Methods ---

        public async Task<List<MPIOPathInfo>> GetMPIOPathsAsync(ulong mpioDiskId = 0) // Default to 0, common for MS DSM
        {
            return await Task.Run(() =>
            {
                uint numPaths = 0;
                IntPtr pathsPtr = IntPtr.Zero;
                uint result = MpioGetPathInfo(mpioDiskId, ref numPaths, out pathsPtr);

                if (result != ERROR_SUCCESS)
                {
                    if (pathsPtr != IntPtr.Zero) LocalFree(pathsPtr); // Clean up on error
                    throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Failed to get MPIO path info. Error code: {result}");
                }

                if (numPaths == 0 || pathsPtr == IntPtr.Zero)
                {
                    return new List<MPIOPathInfo>(); // No paths found
                }

                List<MPIOPathInfo> paths = new List<MPIOPathInfo>();
                IntPtr currentPtr = pathsPtr;
                int pathInfoSize = Marshal.SizeOf(typeof(MPIO_PATH_INFO));

                try
                {
                    for (int i = 0; i < numPaths; i++)
                    {
                        MPIO_PATH_INFO rawPathInfo = (MPIO_PATH_INFO)Marshal.PtrToStructure(currentPtr, typeof(MPIO_PATH_INFO));
                        paths.Add(new MPIOPathInfo
                        {
                            PathId = rawPathInfo.PathId,
                            ConnectionId = rawPathInfo.ConnectionId.ToString("X"), // Match format used elsewhere
                            State = (PathStatus)rawPathInfo.PathState, // Assuming PathStatus enum maps correctly
                            BusNumber = rawPathInfo.BusNumber,
                            TargetId = rawPathInfo.TargetId,
                            Lun = rawPathInfo.Lun
                        });
                        currentPtr = IntPtr.Add(currentPtr, pathInfoSize);
                    }
                }
                finally
                {
                    LocalFree(pathsPtr); // MUST free the allocated buffer
                }

                return paths;
            });
        }

        public async Task<bool> SetMPIOLoadBalancePolicyAsync(uint policy, ulong mpioDiskId = 0)
        {
            return await Task.Run(() =>
            {
                DSM_LOAD_BALANCE_POLICY lbPolicy = new DSM_LOAD_BALANCE_POLICY
                {
                    Version = 1,
                    LoadBalancePolicy = policy
                };

                uint result = MpioSetLoadBalancePolicy(mpioDiskId, ref lbPolicy);

                if (result == ERROR_SUCCESS)
                {
                    return true;
                }
                else
                {
                    throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Failed to set MPIO load balance policy. Error code: {result}");
                }
            });
        }

        // Note: MpioRegisterDevice is typically used for non-PNP devices or specific scenarios.
        // For standard iSCSI LUNs managed by MS DSM, registration might not be needed explicitly.
        // This method is provided for completeness but might require specific device paths.
        public async Task<bool> RegisterMPIODeviceAsync(string devicePath, ulong mpioDiskId = 0)
        {
             return await Task.Run(() =>
             {
                 uint result = MpioRegisterDevice(mpioDiskId, devicePath);
                 if (result == ERROR_SUCCESS)
                 {
                     return true;
                 }
                 else
                 {
                     // Consider specific errors like ERROR_DEVICE_ALREADY_REGISTERED
                     throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Failed to register MPIO device '{devicePath}'. Error code: {result}");
                 }
             });
        }

        // Helper method to potentially map iSCSI sessions to MPIO devices (complex)
        // This is a simplified example and might need refinement based on how MPIO Disk IDs are assigned.
        public async Task<ulong?> FindMpioDiskIdForSessionAsync(ISCSI_UNIQUE_SESSION_ID sessionId)
        {
            // This requires correlating iSCSI session/connection info with MPIO device info.
            // MpioGetDeviceList might be needed here to iterate through MPIO devices and check
            // properties or associated paths to find a match with the session's connections.
            // This is non-trivial and depends heavily on the system's MPIO configuration.
            // Returning null for now as a placeholder.
            await Task.Delay(10); // Simulate async work
            // Implementation using MpioGetDeviceList
            return await Task.Run(() =>
            {
                uint numDevices = 0;
                IntPtr devicesPtr = IntPtr.Zero;
                uint result = MpioGetDeviceList(out numDevices, out devicesPtr);

                if (result != ERROR_SUCCESS)
                {
                    if (devicesPtr != IntPtr.Zero) LocalFree(devicesPtr);
                    throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Failed to get MPIO device list. Error code: {result}");
                }

                if (numDevices == 0 || devicesPtr == IntPtr.Zero)
                {
                    return (ulong?)null; // No MPIO devices found
                }

                ulong? foundDiskId = null;
                IntPtr currentPtr = devicesPtr;
                int devInfoSize = Marshal.SizeOf(typeof(MPIO_DEVINSTANCE_INFO));

                try
                {
                    for (int i = 0; i < numDevices; i++)
                    {
                        MPIO_DEVINSTANCE_INFO devInfo = (MPIO_DEVINSTANCE_INFO)Marshal.PtrToStructure(currentPtr, typeof(MPIO_DEVINSTANCE_INFO));

                        // Now, get paths for this device to find connection IDs
                        uint numPaths = 0;
                        IntPtr pathsPtr = IntPtr.Zero;
                        result = MpioGetPathInfo(devInfo.MpioDiskId, ref numPaths, out pathsPtr);

                        if (result == ERROR_SUCCESS && numPaths > 0 && pathsPtr != IntPtr.Zero)
                        {
                            IntPtr currentPathPtr = pathsPtr;
                            int pathInfoSize = Marshal.SizeOf(typeof(MPIO_PATH_INFO));
                            try
                            {
                                for (int j = 0; j < numPaths; j++)
                                {
                                    MPIO_PATH_INFO pathInfo = (MPIO_PATH_INFO)Marshal.PtrToStructure(currentPathPtr, typeof(MPIO_PATH_INFO));
                                    // Compare the AdapterSpecific part of the session ID with the ConnectionId from MPIO path
                                    // This assumes a direct correlation, which might need adjustment based on specific DSM behavior.
                                    if (pathInfo.ConnectionId == sessionId.AdapterSpecific)
                                    {
                                        foundDiskId = devInfo.MpioDiskId;
                                        break; // Found a match
                                    }
                                    currentPathPtr = IntPtr.Add(currentPathPtr, pathInfoSize);
                                }
                            }
                            finally
                            {
                                LocalFree(pathsPtr);
                            }
                        }
                        else if (result != ERROR_SUCCESS && pathsPtr != IntPtr.Zero)
                        {
                             LocalFree(pathsPtr); // Clean up even on error
                        }

                        if (foundDiskId.HasValue)
                        {
                            break; // Found the disk ID, exit outer loop
                        }

                        currentPtr = IntPtr.Add(currentPtr, devInfoSize);
                    }
                }
                finally
                {
                    LocalFree(devicesPtr); // MUST free the device list buffer
                }

                return foundDiskId;
            });
        }

        // --- iSCSI Target Management Methods (Requires iscsitgt.dll and iSCSI Target Server Role) ---

        public async Task<IntPtr> CreateIScsiTargetAsync(string targetName, string description = null)
        {
            // Note: Creating LUN mappings (LunMappings) is complex and requires additional structures/APIs.
            // This example assumes no LUNs are mapped initially.
            return await Task.Run(() =>
            {
                IntPtr targetId = IntPtr.Zero;
                // Passing IntPtr.Zero for LUN mappings and 0 for count
                uint result = CreateIScsiTarget(targetName, description ?? string.Empty, IntPtr.Zero, 0, out targetId);

                if (result == ERROR_SUCCESS)
                {
                    return targetId;
                }
                else
                {
                    throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Failed to create iSCSI target '{targetName}'. Error code: {result}");
                }
            });
        }

        public async Task<bool> DeleteIScsiTargetAsync(IntPtr targetId)
        {
            return await Task.Run(() =>
            {
                uint result = DeleteIScsiTarget(targetId);
                if (result == ERROR_SUCCESS)
                {
                    return true;
                }
                else
                {
                    // Consider specific error codes if needed
                    throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Failed to delete iSCSI target with ID '{targetId}'. Error code: {result}");
                }
            });
        }

        public async Task<bool> ConfigureIScsiTargetChapAsync(IntPtr targetId, bool enableChap, string chapUsername, string chapSecret)
        {
             return await Task.Run(() =>
             {
                 uint result = SetIScsiTargetCHAPInfo(targetId, enableChap, chapUsername, chapSecret);
                 if (result == ERROR_SUCCESS)
                 {
                     return true;
                 }
                 else
                 {
                     throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Failed to configure CHAP for iSCSI target ID '{targetId}'. Error code: {result}");
                 }
             });
        }

        public async Task<bool> SetMpioLoadBalancePolicyAsync(ulong mpioDiskId, uint policy)
        {
            return await Task.Run(() =>
            {
                DSM_LOAD_BALANCE_POLICY lbPolicy = new DSM_LOAD_BALANCE_POLICY
                {
                    Version = 1,
                    LoadBalancePolicy = policy,
                    Reserved1 = 0,
                    Reserved2 = 0
                };

                uint result = MpioSetLoadBalancePolicy(mpioDiskId, ref lbPolicy);

                if (result == ERROR_SUCCESS)
                {
                    return true;
                }
                else
                {
                    throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Failed to set MPIO load balance policy for Disk ID {mpioDiskId}. Error code: {result}");
                }
            });
        }

    }
}

        public async Task<List<ISCSITarget>> DiscoverTargetsAsync(string portalAddress, bool forceUpdate = false)
        {
            // Note: ReportIScsiTargets doesn't filter by portalAddress. It returns all discovered targets.
            // The portalAddress parameter might be used later if we implement AddIScsiSendTargetPortal.
            return await Task.Run(() =>
            {
                uint bufferSize = 0;
                uint result = ReportIScsiTargetsW(forceUpdate, ref bufferSize, null);

                if (result == ERROR_INSUFFICIENT_BUFFER)
                {
                    StringBuilder buffer = new StringBuilder((int)bufferSize);
                    result = ReportIScsiTargetsW(forceUpdate, ref bufferSize, buffer);

                    if (result == ERROR_SUCCESS)
                    {
                        List<ISCSITarget> targets = new List<ISCSITarget>();
                        string allTargets = buffer.ToString();
                        // The buffer contains a list of null-terminated strings, ending with a double null terminator.
                        string[] targetNames = allTargets.Split(new[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (string targetName in targetNames)
                        {
                            // We need more API calls (like ReportIScsiSendTargetPortals) to get the portal address for each target.
                            // For now, we'll create a basic target object.
                            targets.Add(new ISCSITarget
                            {
                                TargetName = targetName,
                                // PortalAddress and PortalPort would need to be fetched separately
                                PortalAddress = portalAddress, // Using input for now, but this is likely incorrect
                                PortalPort = 3260, // Default port
                                IsConnected = false // Need to check connection status separately
                            });
                        }
                        return targets;
                    }
                    else
                    {
                        throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Failed to report iSCSI targets. Error code: {result}");
                    }
                }
                else if (result == ERROR_SUCCESS)
                {
                    // No targets found or buffer size was unexpectedly 0.
                    return new List<ISCSITarget>();
                }
                else
                {
                    throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Failed to get buffer size for iSCSI targets. Error code: {result}");
                }
            });
        }

        // --- MPIO Service Methods ---

        public async Task<List<MPIOPathInfo>> GetMPIOPathsAsync(ulong mpioDiskId = 0) // Default to 0, common for MS DSM
        {
            return await Task.Run(() =>
            {
                uint numPaths = 0;
                IntPtr pathsPtr = IntPtr.Zero;
                uint result = MpioGetPathInfo(mpioDiskId, ref numPaths, out pathsPtr);

                if (result != ERROR_SUCCESS)
                {
                    if (pathsPtr != IntPtr.Zero) LocalFree(pathsPtr); // Clean up on error
                    throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Failed to get MPIO path info. Error code: {result}");
                }

                if (numPaths == 0 || pathsPtr == IntPtr.Zero)
                {
                    return new List<MPIOPathInfo>(); // No paths found
                }

                List<MPIOPathInfo> paths = new List<MPIOPathInfo>();
                IntPtr currentPtr = pathsPtr;
                int pathInfoSize = Marshal.SizeOf(typeof(MPIO_PATH_INFO));

                try
                {
                    for (int i = 0; i < numPaths; i++)
                    {
                        MPIO_PATH_INFO rawPathInfo = (MPIO_PATH_INFO)Marshal.PtrToStructure(currentPtr, typeof(MPIO_PATH_INFO));
                        paths.Add(new MPIOPathInfo
                        {
                            PathId = rawPathInfo.PathId,
                            ConnectionId = rawPathInfo.ConnectionId.ToString("X"), // Match format used elsewhere
                            State = (PathStatus)rawPathInfo.PathState, // Assuming PathStatus enum maps correctly
                            BusNumber = rawPathInfo.BusNumber,
                            TargetId = rawPathInfo.TargetId,
                            Lun = rawPathInfo.Lun
                        });
                        currentPtr = IntPtr.Add(currentPtr, pathInfoSize);
                    }
                }
                finally
                {
                    LocalFree(pathsPtr); // MUST free the allocated buffer
                }

                return paths;
            });
        }

        public async Task<bool> SetMPIOLoadBalancePolicyAsync(uint policy, ulong mpioDiskId = 0)
        {
            return await Task.Run(() =>
            {
                DSM_LOAD_BALANCE_POLICY lbPolicy = new DSM_LOAD_BALANCE_POLICY
                {
                    Version = 1,
                    LoadBalancePolicy = policy
                };

                uint result = MpioSetLoadBalancePolicy(mpioDiskId, ref lbPolicy);

                if (result == ERROR_SUCCESS)
                {
                    return true;
                }
                else
                {
                    throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Failed to set MPIO load balance policy. Error code: {result}");
                }
            });
        }

        // Note: MpioRegisterDevice is typically used for non-PNP devices or specific scenarios.
        // For standard iSCSI LUNs managed by MS DSM, registration might not be needed explicitly.
        // This method is provided for completeness but might require specific device paths.
        public async Task<bool> RegisterMPIODeviceAsync(string devicePath, ulong mpioDiskId = 0)
        {
             return await Task.Run(() =>
             {
                 uint result = MpioRegisterDevice(mpioDiskId, devicePath);
                 if (result == ERROR_SUCCESS)
                 {
                     return true;
                 }
                 else
                 {
                     // Consider specific errors like ERROR_DEVICE_ALREADY_REGISTERED
                     throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Failed to register MPIO device '{devicePath}'. Error code: {result}");
                 }
             });
        }

        // Helper method to potentially map iSCSI sessions to MPIO devices (complex)
        // This is a simplified example and might need refinement based on how MPIO Disk IDs are assigned.
        public async Task<ulong?> FindMpioDiskIdForSessionAsync(ISCSI_UNIQUE_SESSION_ID sessionId)
        {
            // This requires correlating iSCSI session/connection info with MPIO device info.
            // MpioGetDeviceList might be needed here to iterate through MPIO devices and check
            // properties or associated paths to find a match with the session's connections.
            // This is non-trivial and depends heavily on the system's MPIO configuration.
            // Returning null for now as a placeholder.
            await Task.Delay(10); // Simulate async work
            // Implementation using MpioGetDeviceList
            return await Task.Run(() =>
            {
                uint numDevices = 0;
                IntPtr devicesPtr = IntPtr.Zero;
                uint result = MpioGetDeviceList(out numDevices, out devicesPtr);

                if (result != ERROR_SUCCESS)
                {
                    if (devicesPtr != IntPtr.Zero) LocalFree(devicesPtr);
                    throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Failed to get MPIO device list. Error code: {result}");
                }

                if (numDevices == 0 || devicesPtr == IntPtr.Zero)
                {
                    return (ulong?)null; // No MPIO devices found
                }

                ulong? foundDiskId = null;
                IntPtr currentPtr = devicesPtr;
                int devInfoSize = Marshal.SizeOf(typeof(MPIO_DEVINSTANCE_INFO));

                try
                {
                    for (int i = 0; i < numDevices; i++)
                    {
                        MPIO_DEVINSTANCE_INFO devInfo = (MPIO_DEVINSTANCE_INFO)Marshal.PtrToStructure(currentPtr, typeof(MPIO_DEVINSTANCE_INFO));

                        // Now, get paths for this device to find connection IDs
                        uint numPaths = 0;
                        IntPtr pathsPtr = IntPtr.Zero;
                        result = MpioGetPathInfo(devInfo.MpioDiskId, ref numPaths, out pathsPtr);

                        if (result == ERROR_SUCCESS && numPaths > 0 && pathsPtr != IntPtr.Zero)
                        {
                            IntPtr currentPathPtr = pathsPtr;
                            int pathInfoSize = Marshal.SizeOf(typeof(MPIO_PATH_INFO));
                            try
                            {
                                for (int j = 0; j < numPaths; j++)
                                {
                                    MPIO_PATH_INFO pathInfo = (MPIO_PATH_INFO)Marshal.PtrToStructure(currentPathPtr, typeof(MPIO_PATH_INFO));
                                    // Compare the AdapterSpecific part of the session ID with the ConnectionId from MPIO path
                                    // This assumes a direct correlation, which might need adjustment based on specific DSM behavior.
                                    if (pathInfo.ConnectionId == sessionId.AdapterSpecific)
                                    {
                                        foundDiskId = devInfo.MpioDiskId;
                                        break; // Found a match
                                    }
                                    currentPathPtr = IntPtr.Add(currentPathPtr, pathInfoSize);
                                }
                            }
                            finally
                            {
                                LocalFree(pathsPtr);
                            }
                        }
                        else if (result != ERROR_SUCCESS && pathsPtr != IntPtr.Zero)
                        {
                             LocalFree(pathsPtr); // Clean up even on error
                        }

                        if (foundDiskId.HasValue)
                        {
                            break; // Found the disk ID, exit outer loop
                        }

                        currentPtr = IntPtr.Add(currentPtr, devInfoSize);
                    }
                }
                finally
                {
                    LocalFree(devicesPtr); // MUST free the device list buffer
                }

                return foundDiskId;
            });
        }

        // --- iSCSI Target Management Methods (Requires iscsitgt.dll and iSCSI Target Server Role) ---

        public async Task<IntPtr> CreateIScsiTargetAsync(string targetName, string description = null)
        {
            // Note: Creating LUN mappings (LunMappings) is complex and requires additional structures/APIs.
            // This example assumes no LUNs are mapped initially.
            return await Task.Run(() =>
            {
                IntPtr targetId = IntPtr.Zero;
                // Passing IntPtr.Zero for LUN mappings and 0 for count
                uint result = CreateIScsiTarget(targetName, description ?? string.Empty, IntPtr.Zero, 0, out targetId);

                if (result == ERROR_SUCCESS)
                {
                    return targetId;
                }
                else
                {
                    throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Failed to create iSCSI target '{targetName}'. Error code: {result}");
                }
            });
        }

        public async Task<bool> DeleteIScsiTargetAsync(IntPtr targetId)
        {
            return await Task.Run(() =>
            {
                uint result = DeleteIScsiTarget(targetId);
                if (result == ERROR_SUCCESS)
                {
                    return true;
                }
                else
                {
                    // Consider specific error codes if needed
                    throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Failed to delete iSCSI target with ID '{targetId}'. Error code: {result}");
                }
            });
        }

        public async Task<bool> ConfigureIScsiTargetChapAsync(IntPtr targetId, bool enableChap, string chapUsername, string chapSecret)
        {
             return await Task.Run(() =>
             {
                 uint result = SetIScsiTargetCHAPInfo(targetId, enableChap, chapUsername, chapSecret);
                 if (result == ERROR_SUCCESS)
                 {
                     return true;
                 }
                 else
                 {
                     throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Failed to configure CHAP for iSCSI target ID '{targetId}'. Error code: {result}");
                 }
             });
        }

        public async Task<bool> SetMpioLoadBalancePolicyAsync(ulong mpioDiskId, uint policy)
        {
            return await Task.Run(() =>
            {
                DSM_LOAD_BALANCE_POLICY lbPolicy = new DSM_LOAD_BALANCE_POLICY
                {
                    Version = 1,
                    LoadBalancePolicy = policy,
                    Reserved1 = 0,
                    Reserved2 = 0
                };

                uint result = MpioSetLoadBalancePolicy(mpioDiskId, ref lbPolicy);

                if (result == ERROR_SUCCESS)
                {
                    return true;
                }
                else
                {
                    throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Failed to set MPIO load balance policy for Disk ID {mpioDiskId}. Error code: {result}");
                }
            });
        }

    }
}

        public async Task<bool> ConnectToTargetAsync(ISCSITarget target, bool isPersistent, string username = null, string password = null)
        {
            return await Task.Run(() =>
            {
                ISCSI_TARGET_PORTALW targetPortal = new ISCSI_TARGET_PORTALW
                {
                    Address = target.PortalAddress,
                    Socket = (ushort)target.PortalPort
                };

                ISCSI_LOGIN_OPTIONS loginOptions = new ISCSI_LOGIN_OPTIONS { Version = 0 }; // Initialize version
                ISCSI_LOGIN_OPTIONS_INFO_SPECIFIED specified = 0;

                // Configure CHAP if credentials are provided
                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                {
                    // Note: Actual CHAP secret passing requires the 'Key' parameter and potentially different AuthType logic.
                    // LoginIScsiTarget documentation is complex regarding CHAP. This is a simplified approach.
                    // A more robust implementation might need SetIScsiTunnelModeOuterModeAddress or SetIScsiChapSharedSecret.
                    loginOptions.AuthType = ISCSI_LOGIN_AUTH_TYPES.OneWayCHAP; // Or MutualCHAP if supported/required
                    specified |= ISCSI_LOGIN_OPTIONS_INFO_SPECIFIED.AuthType;
                    // TODO: Need to figure out how to pass username/password correctly. The 'Key' parameter is likely involved.
                    // For now, this setup might fail CHAP authentication.
                }
                else
                {
                    loginOptions.AuthType = ISCSI_LOGIN_AUTH_TYPES.None;
                    specified |= ISCSI_LOGIN_OPTIONS_INFO_SPECIFIED.AuthType;
                }

                loginOptions.InformationSpecified = specified;

                // Call LoginIScsiTargetW
                uint result = LoginIScsiTargetW(
                    target.TargetName,
                    false, // IsInformationalSession - Set to false for a standard login
                    null, // InitiatorInstance - Let the service choose
                    ISCSI_ANY_INITIATOR_PORT, // InitiatorPortNumber - Let the service choose
                    ref targetPortal, // TargetPortal
                    ISCSI_SECURITY_FLAGS.None, // SecurityFlags - Assuming no IPsec
                    IntPtr.Zero, // Mappings - Not used
                    ref loginOptions, // LoginOptions
                    0, // KeySize - For CHAP/IPsec keys, needs correct size
                    null, // Key - For CHAP/IPsec keys, needs pointer to key data
                    isPersistent,
                    out ISCSI_UNIQUE_SESSION_ID sessionId,
                    out ISCSI_UNIQUE_CONNECTION_ID connectionId
                );

                if (result == ERROR_SUCCESS)
                {
                    // Store the session ID components
                    target.SessionAdapterUnique = sessionId.AdapterUnique;
                    target.SessionAdapterSpecific = sessionId.AdapterSpecific;
                    target.ConnectionId = connectionId.AdapterSpecific.ToString("X") + connectionId.AdapterUnique.ToString("X"); // Keep ConnectionId as string for now
                    target.IsConnected = true;
                    target.IsPersistent = isPersistent;
                    target.ChapUsername = username; // Store for reference, actual auth might differ
                    target.LastConnected = DateTime.Now;

                    // Attempt to find MPIO Disk ID and retrieve paths after successful login
                    try
                    {
                        // Find the MPIO Disk ID associated with this session (placeholder implementation)
                        target.MpioDiskId = await FindMpioDiskIdForSessionAsync(sessionId);

                        if (target.MpioDiskId.HasValue)
                        {
                            // Retrieve MPIO paths using the found Disk ID
                            target.Paths = await GetMPIOPathsAsync(target.MpioDiskId.Value);
                            target.IsMPIOEnabled = target.Paths.Count > 0;
                            // Log successful MPIO path retrieval or if none were found
                            Console.WriteLine($"MPIO Check for {target.TargetName}: Found {target.Paths.Count} paths for Disk ID {target.MpioDiskId.Value}.");
                        }
                        else
                        {
                            // Attempt with default Disk ID 0 if specific ID wasn't found
                            target.Paths = await GetMPIOPathsAsync(0);
                            if (target.Paths.Count > 0)
                            {
                                target.IsMPIOEnabled = true;
                                target.MpioDiskId = 0; // Assume default ID worked
                                Console.WriteLine($"MPIO Check for {target.TargetName}: Found {target.Paths.Count} paths using default Disk ID 0.");
                            }
                            else
                            {
                                target.IsMPIOEnabled = false;
                                Console.WriteLine($"MPIO Check for {target.TargetName}: No MPIO paths found (tried specific and default Disk ID).");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log MPIO retrieval errors but don't fail the connection
                        Console.WriteLine($"Warning: Failed to retrieve MPIO paths for {target.TargetName} after connection. Error: {ex.Message}");
                        target.IsMPIOEnabled = false;
                        target.Paths.Clear();
                    }

                    return true;
                }
                else
                {
                    // Consider specific error codes (e.g., authentication failure)
                    throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Failed to login to iSCSI target '{target.TargetName}'. Error code: {result}");
                }
            });
        }

        // --- MPIO Service Methods ---

        public async Task<List<MPIOPathInfo>> GetMPIOPathsAsync(ulong mpioDiskId = 0) // Default to 0, common for MS DSM
        {
            return await Task.Run(() =>
            {
                uint numPaths = 0;
                IntPtr pathsPtr = IntPtr.Zero;
                uint result = MpioGetPathInfo(mpioDiskId, ref numPaths, out pathsPtr);

                if (result != ERROR_SUCCESS)
                {
                    if (pathsPtr != IntPtr.Zero) LocalFree(pathsPtr); // Clean up on error
                    throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Failed to get MPIO path info. Error code: {result}");
                }

                if (numPaths == 0 || pathsPtr == IntPtr.Zero)
                {
                    return new List<MPIOPathInfo>(); // No paths found
                }

                List<MPIOPathInfo> paths = new List<MPIOPathInfo>();
                IntPtr currentPtr = pathsPtr;
                int pathInfoSize = Marshal.SizeOf(typeof(MPIO_PATH_INFO));

                try
                {
                    for (int i = 0; i < numPaths; i++)
                    {
                        MPIO_PATH_INFO rawPathInfo = (MPIO_PATH_INFO)Marshal.PtrToStructure(currentPtr, typeof(MPIO_PATH_INFO));
                        paths.Add(new MPIOPathInfo
                        {
                            PathId = rawPathInfo.PathId,
                            ConnectionId = rawPathInfo.ConnectionId.ToString("X"), // Match format used elsewhere
                            State = (PathStatus)rawPathInfo.PathState, // Assuming PathStatus enum maps correctly
                            BusNumber = rawPathInfo.BusNumber,
                            TargetId = rawPathInfo.TargetId,
                            Lun = rawPathInfo.Lun
                        });
                        currentPtr = IntPtr.Add(currentPtr, pathInfoSize);
                    }
                }
                finally
                {
                    LocalFree(pathsPtr); // MUST free the allocated buffer
                }

                return paths;
            });
        }

        public async Task<bool> SetMPIOLoadBalancePolicyAsync(uint policy, ulong mpioDiskId = 0)
        {
            return await Task.Run(() =>
            {
                DSM_LOAD_BALANCE_POLICY lbPolicy = new DSM_LOAD_BALANCE_POLICY
                {
                    Version = 1,
                    LoadBalancePolicy = policy
                };

                uint result = MpioSetLoadBalancePolicy(mpioDiskId, ref lbPolicy);

                if (result == ERROR_SUCCESS)
                {
                    return true;
                }
                else
                {
                    throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Failed to set MPIO load balance policy. Error code: {result}");
                }
            });
        }

        // Note: MpioRegisterDevice is typically used for non-PNP devices or specific scenarios.
        // For standard iSCSI LUNs managed by MS DSM, registration might not be needed explicitly.
        // This method is provided for completeness but might require specific device paths.
        public async Task<bool> RegisterMPIODeviceAsync(string devicePath, ulong mpioDiskId = 0)
        {
             return await Task.Run(() =>
             {
                 uint result = MpioRegisterDevice(mpioDiskId, devicePath);
                 if (result == ERROR_SUCCESS)
                 {
                     return true;
                 }
                 else
                 {
                     // Consider specific errors like ERROR_DEVICE_ALREADY_REGISTERED
                     throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Failed to register MPIO device '{devicePath}'. Error code: {result}");
                 }
             });
        }

        // Helper method to potentially map iSCSI sessions to MPIO devices (complex)
        // This is a simplified example and might need refinement based on how MPIO Disk IDs are assigned.
        public async Task<ulong?> FindMpioDiskIdForSessionAsync(ISCSI_UNIQUE_SESSION_ID sessionId)
        {
            // This requires correlating iSCSI session/connection info with MPIO device info.
            // MpioGetDeviceList might be needed here to iterate through MPIO devices and check
            // properties or associated paths to find a match with the session's connections.
            // This is non-trivial and depends heavily on the system's MPIO configuration.
            // Returning null for now as a placeholder.
            await Task.Delay(10); // Simulate async work
            // Implementation using MpioGetDeviceList
            return await Task.Run(() =>
            {
                uint numDevices = 0;
                IntPtr devicesPtr = IntPtr.Zero;
                uint result = MpioGetDeviceList(out numDevices, out devicesPtr);

                if (result != ERROR_SUCCESS)
                {
                    if (devicesPtr != IntPtr.Zero) LocalFree(devicesPtr);
                    throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Failed to get MPIO device list. Error code: {result}");
                }

                if (numDevices == 0 || devicesPtr == IntPtr.Zero)
                {
                    return (ulong?)null; // No MPIO devices found
                }

                ulong? foundDiskId = null;
                IntPtr currentPtr = devicesPtr;
                int devInfoSize = Marshal.SizeOf(typeof(MPIO_DEVINSTANCE_INFO));

                try
                {
                    for (int i = 0; i < numDevices; i++)
                    {
                        MPIO_DEVINSTANCE_INFO devInfo = (MPIO_DEVINSTANCE_INFO)Marshal.PtrToStructure(currentPtr, typeof(MPIO_DEVINSTANCE_INFO));

                        // Now, get paths for this device to find connection IDs
                        uint numPaths = 0;
                        IntPtr pathsPtr = IntPtr.Zero;
                        result = MpioGetPathInfo(devInfo.MpioDiskId, ref numPaths, out pathsPtr);

                        if (result == ERROR_SUCCESS && numPaths > 0 && pathsPtr != IntPtr.Zero)
                        {
                            IntPtr currentPathPtr = pathsPtr;
                            int pathInfoSize = Marshal.SizeOf(typeof(MPIO_PATH_INFO));
                            try
                            {
                                for (int j = 0; j < numPaths; j++)
                                {
                                    MPIO_PATH_INFO pathInfo = (MPIO_PATH_INFO)Marshal.PtrToStructure(currentPathPtr, typeof(MPIO_PATH_INFO));
                                    // Compare the AdapterSpecific part of the session ID with the ConnectionId from MPIO path
                                    // This assumes a direct correlation, which might need adjustment based on specific DSM behavior.
                                    if (pathInfo.ConnectionId == sessionId.AdapterSpecific)
                                    {
                                        foundDiskId = devInfo.MpioDiskId;
                                        break; // Found a match
                                    }
                                    currentPathPtr = IntPtr.Add(currentPathPtr, pathInfoSize);
                                }
                            }
                            finally
                            {
                                LocalFree(pathsPtr);
                            }
                        }
                        else if (result != ERROR_SUCCESS && pathsPtr != IntPtr.Zero)
                        {
                             LocalFree(pathsPtr); // Clean up even on error
                        }

                        if (foundDiskId.HasValue)
                        {
                            break; // Found the disk ID, exit outer loop
                        }

                        currentPtr = IntPtr.Add(currentPtr, devInfoSize);
                    }
                }
                finally
                {
                    LocalFree(devicesPtr); // MUST free the device list buffer
                }

                return foundDiskId;
            });
        }

        // --- iSCSI Target Management Methods (Requires iscsitgt.dll and iSCSI Target Server Role) ---

        public async Task<IntPtr> CreateIScsiTargetAsync(string targetName, string description = null)
        {
            // Note: Creating LUN mappings (LunMappings) is complex and requires additional structures/APIs.
            // This example assumes no LUNs are mapped initially.
            return await Task.Run(() =>
            {
                IntPtr targetId = IntPtr.Zero;
                // Passing IntPtr.Zero for LUN mappings and 0 for count
                uint result = CreateIScsiTarget(targetName, description ?? string.Empty, IntPtr.Zero, 0, out targetId);

                if (result == ERROR_SUCCESS)
                {
                    return targetId;
                }
                else
                {
                    throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Failed to create iSCSI target '{targetName}'. Error code: {result}");
                }
            });
        }

        public async Task<bool> DeleteIScsiTargetAsync(IntPtr targetId)
        {
            return await Task.Run(() =>
            {
                uint result = DeleteIScsiTarget(targetId);
                if (result == ERROR_SUCCESS)
                {
                    return true;
                }
                else
                {
                    // Consider specific error codes if needed
                    throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Failed to delete iSCSI target with ID '{targetId}'. Error code: {result}");
                }
            });
        }

        public async Task<bool> ConfigureIScsiTargetChapAsync(IntPtr targetId, bool enableChap, string chapUsername, string chapSecret)
        {
             return await Task.Run(() =>
             {
                 uint result = SetIScsiTargetCHAPInfo(targetId, enableChap, chapUsername, chapSecret);
                 if (result == ERROR_SUCCESS)
                 {
                     return true;
                 }
                 else
                 {
                     throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Failed to configure CHAP for iSCSI target ID '{targetId}'. Error code: {result}");
                 }
             });
        }

        public async Task<bool> SetMpioLoadBalancePolicyAsync(ulong mpioDiskId, uint policy)
        {
            return await Task.Run(() =>
            {
                DSM_LOAD_BALANCE_POLICY lbPolicy = new DSM_LOAD_BALANCE_POLICY
                {
                    Version = 1,
                    LoadBalancePolicy = policy,
                    Reserved1 = 0,
                    Reserved2 = 0
                };

                uint result = MpioSetLoadBalancePolicy(mpioDiskId, ref lbPolicy);

                if (result == ERROR_SUCCESS)
                {
                    return true;
                }
                else
                {
                    throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Failed to set MPIO load balance policy for Disk ID {mpioDiskId}. Error code: {result}");
                }
            });
        }

    }
}

        public async Task<bool> DisconnectFromTargetAsync(ISCSITarget target)
        {
            // Check if the target is connected and has valid session ID components
            if (!target.IsConnected || target.SessionAdapterUnique == 0 || target.SessionAdapterSpecific == 0)
            {
                // Target not connected or session ID missing/invalid
                return false;
            }

            return await Task.Run(() =>
            {
                // Use the stored ulong components directly
                ISCSI_UNIQUE_SESSION_ID sessionId = new ISCSI_UNIQUE_SESSION_ID
                {
                    AdapterUnique = target.SessionAdapterUnique,
                    AdapterSpecific = target.SessionAdapterSpecific
                };

                uint result = LogoutIScsiTarget(ref sessionId);

                if (result == ERROR_SUCCESS)
                {
                    target.IsConnected = false;
                    target.SessionAdapterUnique = 0;
                    target.SessionAdapterSpecific = 0;
                    target.ConnectionId = null;
                    // Clear MPIO paths if necessary (though they might become invalid automatically)
                    if (target.IsMPIOEnabled)
                    {
                        target.Paths.Clear(); // Clear the paths list on disconnect
                        target.IsMPIOEnabled = false; // Reset MPIO status
                        foreach (var path in target.Paths)
                        {
                            path.PathSessionId = null;
                            path.Status = PathStatus.Unavailable;
                        }
                    }
                    return true;
                }
                else
                {
                    // Check for specific errors, e.g., device in use (ERROR_DEVICE_IN_USE 21)
                    int errorCode = Marshal.GetLastPInvokeError();
                    string sessionIdStr = target.SessionAdapterSpecific.ToString("X") + target.SessionAdapterUnique.ToString("X"); // Reconstruct for error message if needed
                    throw new Win32Exception(errorCode, $"Failed to logout from iSCSI target '{target.TargetName}'. Session ID components: {target.SessionAdapterSpecific:X}-{target.SessionAdapterUnique:X}. Error code: {result} (Win32: {errorCode})");
                }
            });
        }

        // --- MPIO Service Methods ---

        public async Task<List<MPIOPathInfo>> GetMPIOPathsAsync(ulong mpioDiskId = 0) // Default to 0, common for MS DSM
        {
            return await Task.Run(() =>
            {
                uint numPaths = 0;
                IntPtr pathsPtr = IntPtr.Zero;
                uint result = MpioGetPathInfo(mpioDiskId, ref numPaths, out pathsPtr);

                if (result != ERROR_SUCCESS)
                {
                    if (pathsPtr != IntPtr.Zero) LocalFree(pathsPtr); // Clean up on error
                    throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Failed to get MPIO path info. Error code: {result}");
                }

                if (numPaths == 0 || pathsPtr == IntPtr.Zero)
                {
                    return new List<MPIOPathInfo>(); // No paths found
                }

                List<MPIOPathInfo> paths = new List<MPIOPathInfo>();
                IntPtr currentPtr = pathsPtr;
                int pathInfoSize = Marshal.SizeOf(typeof(MPIO_PATH_INFO));

                try
                {
                    for (int i = 0; i < numPaths; i++)
                    {
                        MPIO_PATH_INFO rawPathInfo = (MPIO_PATH_INFO)Marshal.PtrToStructure(currentPtr, typeof(MPIO_PATH_INFO));
                        paths.Add(new MPIOPathInfo
                        {
                            PathId = rawPathInfo.PathId,
                            ConnectionId = rawPathInfo.ConnectionId.ToString("X"), // Match format used elsewhere
                            State = (PathStatus)rawPathInfo.PathState, // Assuming PathStatus enum maps correctly
                            BusNumber = rawPathInfo.BusNumber,
                            TargetId = rawPathInfo.TargetId,
                            Lun = rawPathInfo.Lun
                        });
                        currentPtr = IntPtr.Add(currentPtr, pathInfoSize);
                    }
                }
                finally
                {
                    LocalFree(pathsPtr); // MUST free the allocated buffer
                }

                return paths;
            });
        }

        public async Task<bool> SetMPIOLoadBalancePolicyAsync(uint policy, ulong mpioDiskId = 0)
        {
            return await Task.Run(() =>
            {
                DSM_LOAD_BALANCE_POLICY lbPolicy = new DSM_LOAD_BALANCE_POLICY
                {
                    Version = 1,
                    LoadBalancePolicy = policy
                };

                uint result = MpioSetLoadBalancePolicy(mpioDiskId, ref lbPolicy);

                if (result == ERROR_SUCCESS)
                {
                    return true;
                }
                else
                {
                    throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Failed to set MPIO load balance policy. Error code: {result}");
                }
            });
        }

        // Note: MpioRegisterDevice is typically used for non-PNP devices or specific scenarios.
        // For standard iSCSI LUNs managed by MS DSM, registration might not be needed explicitly.
        // This method is provided for completeness but might require specific device paths.
        public async Task<bool> RegisterMPIODeviceAsync(string devicePath, ulong mpioDiskId = 0)
        {
             return await Task.Run(() =>
             {
                 uint result = MpioRegisterDevice(mpioDiskId, devicePath);
                 if (result == ERROR_SUCCESS)
                 {
                     return true;
                 }
                 else
                 {
                     // Consider specific errors like ERROR_DEVICE_ALREADY_REGISTERED
                     throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Failed to register MPIO device '{devicePath}'. Error code: {result}");
                 }
             });
        }

        // Helper method to potentially map iSCSI sessions to MPIO devices (complex)
        // This is a simplified example and might need refinement based on how MPIO Disk IDs are assigned.
        public async Task<ulong?> FindMpioDiskIdForSessionAsync(ISCSI_UNIQUE_SESSION_ID sessionId)
        {
            // This requires correlating iSCSI session/connection info with MPIO device info.
            // MpioGetDeviceList might be needed here to iterate through MPIO devices and check
            // properties or associated paths to find a match with the session's connections.
            // This is non-trivial and depends heavily on the system's MPIO configuration.
            // Returning null for now as a placeholder.
            await Task.Delay(10); // Simulate async work
            // Implementation using MpioGetDeviceList
            return await Task.Run(() =>
            {
                uint numDevices = 0;
                IntPtr devicesPtr = IntPtr.Zero;
                uint result = MpioGetDeviceList(out numDevices, out devicesPtr);

                if (result != ERROR_SUCCESS)
                {
                    if (devicesPtr != IntPtr.Zero) LocalFree(devicesPtr);
                    throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Failed to get MPIO device list. Error code: {result}");
                }

                if (numDevices == 0 || devicesPtr == IntPtr.Zero)
                {
                    return (ulong?)null; // No MPIO devices found
                }

                ulong? foundDiskId = null;
                IntPtr currentPtr = devicesPtr;
                int devInfoSize = Marshal.SizeOf(typeof(MPIO_DEVINSTANCE_INFO));

                try
                {
                    for (int i = 0; i < numDevices; i++)
                    {
                        MPIO_DEVINSTANCE_INFO devInfo = (MPIO_DEVINSTANCE_INFO)Marshal.PtrToStructure(currentPtr, typeof(MPIO_DEVINSTANCE_INFO));

                        // Now, get paths for this device to find connection IDs
                        uint numPaths = 0;
                        IntPtr pathsPtr = IntPtr.Zero;
                        result = MpioGetPathInfo(devInfo.MpioDiskId, ref numPaths, out pathsPtr);

                        if (result == ERROR_SUCCESS && numPaths > 0 && pathsPtr != IntPtr.Zero)
                        {
                            IntPtr currentPathPtr = pathsPtr;
                            int pathInfoSize = Marshal.SizeOf(typeof(MPIO_PATH_INFO));
                            try
                            {
                                for (int j = 0; j < numPaths; j++)
                                {
                                    MPIO_PATH_INFO pathInfo = (MPIO_PATH_INFO)Marshal.PtrToStructure(currentPathPtr, typeof(MPIO_PATH_INFO));
                                    // Compare the AdapterSpecific part of the session ID with the ConnectionId from MPIO path
                                    // This assumes a direct correlation, which might need adjustment based on specific DSM behavior.
                                    if (pathInfo.ConnectionId == sessionId.AdapterSpecific)
                                    {
                                        foundDiskId = devInfo.MpioDiskId;
                                        break; // Found a match
                                    }
                                    currentPathPtr = IntPtr.Add(currentPathPtr, pathInfoSize);
                                }
                            }
                            finally
                            {
                                LocalFree(pathsPtr);
                            }
                        }
                        else if (result != ERROR_SUCCESS && pathsPtr != IntPtr.Zero)
                        {
                             LocalFree(pathsPtr); // Clean up even on error
                        }

                        if (foundDiskId.HasValue)
                        {
                            break; // Found the disk ID, exit outer loop
                        }

                        currentPtr = IntPtr.Add(currentPtr, devInfoSize);
                    }
                }
                finally
                {
                    LocalFree(devicesPtr); // MUST free the device list buffer
                }

                return foundDiskId;
            });
        }

        // --- iSCSI Target Management Methods (Requires iscsitgt.dll and iSCSI Target Server Role) ---

        public async Task<IntPtr> CreateIScsiTargetAsync(string targetName, string description = null)
        {
            // Note: Creating LUN mappings (LunMappings) is complex and requires additional structures/APIs.
            // This example assumes no LUNs are mapped initially.
            return await Task.Run(() =>
            {
                IntPtr targetId = IntPtr.Zero;
                // Passing IntPtr.Zero for LUN mappings and 0 for count
                uint result = CreateIScsiTarget(targetName, description ?? string.Empty, IntPtr.Zero, 0, out targetId);

                if (result == ERROR_SUCCESS)
                {
                    return targetId;
                }
                else
                {
                    throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Failed to create iSCSI target '{targetName}'. Error code: {result}");
                }
            });
        }

        public async Task<bool> DeleteIScsiTargetAsync(IntPtr targetId)
        {
            return await Task.Run(() =>
            {
                uint result = DeleteIScsiTarget(targetId);
                if (result == ERROR_SUCCESS)
                {
                    return true;
                }
                else
                {
                    // Consider specific error codes if needed
                    throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Failed to delete iSCSI target with ID '{targetId}'. Error code: {result}");
                }
            });
        }

        public async Task<bool> ConfigureIScsiTargetChapAsync(IntPtr targetId, bool enableChap, string chapUsername, string chapSecret)
        {
             return await Task.Run(() =>
             {
                 uint result = SetIScsiTargetCHAPInfo(targetId, enableChap, chapUsername, chapSecret);
                 if (result == ERROR_SUCCESS)
                 {
                     return true;
                 }
                 else
                 {
                     throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Failed to configure CHAP for iSCSI target ID '{targetId}'. Error code: {result}");
                 }
             });
        }

        public async Task<bool> SetMpioLoadBalancePolicyAsync(ulong mpioDiskId, uint policy)
        {
            return await Task.Run(() =>
            {
                DSM_LOAD_BALANCE_POLICY lbPolicy = new DSM_LOAD_BALANCE_POLICY
                {
                    Version = 1,
                    LoadBalancePolicy = policy,
                    Reserved1 = 0,
                    Reserved2 = 0
                };

                uint result = MpioSetLoadBalancePolicy(mpioDiskId, ref lbPolicy);

                if (result == ERROR_SUCCESS)
                {
                    return true;
                }
                else
                {
                    throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Failed to set MPIO load balance policy for Disk ID {mpioDiskId}. Error code: {result}");
                }
            });
        }

    }
}

        public async Task<List<ISCSITarget>> GetConnectedTargetsAsync()
        {
            return await Task.Run(() =>
            {
                uint bufferSize = 0;
                uint sessionCount = 0;
                // First call to get buffer size and session count
                uint result = GetIScsiSessionListW(ref bufferSize, ref sessionCount, IntPtr.Zero);

                if (result == ERROR_INSUFFICIENT_BUFFER)
                {
                    IntPtr buffer = Marshal.AllocHGlobal((int)bufferSize);
                    try
                    {
                        // Second call to get the actual session data
                        result = GetIScsiSessionListW(ref bufferSize, ref sessionCount, buffer);

                        if (result == ERROR_SUCCESS)
                        {
                            List<ISCSITarget> connectedTargets = new List<ISCSITarget>();
                            IntPtr currentPtr = buffer;
                            int sessionInfoSize = Marshal.SizeOf(typeof(ISCSI_SESSION_INFOW));

                            for (int i = 0; i < sessionCount; i++)
                            {
                                ISCSI_SESSION_INFOW sessionInfo = (ISCSI_SESSION_INFOW)Marshal.PtrToStructure(currentPtr, typeof(ISCSI_SESSION_INFOW));

                                string targetName = Marshal.PtrToStringUni(sessionInfo.TargetName);
                                // Store session ID components directly
                                ulong sessionAdapterUnique = sessionInfo.SessionId.AdapterUnique;
                                ulong sessionAdapterSpecific = sessionInfo.SessionId.AdapterSpecific;

                                // Find the primary connection to get portal info (simplification)
                                string portalAddress = "Unknown";
                                ushort portalPort = 3260; // Default
                                if (sessionInfo.ConnectionCount > 0 && sessionInfo.Connections != IntPtr.Zero)
                                {
                                    // Assuming the first connection is representative
                                    ISCSI_CONNECTION_INFOW connInfo = (ISCSI_CONNECTION_INFOW)Marshal.PtrToStructure(sessionInfo.Connections, typeof(ISCSI_CONNECTION_INFOW));
                                    portalAddress = Marshal.PtrToStringUni(connInfo.TargetAddress);
                                    portalPort = connInfo.TargetSocket;
                                    // Note: A session can have multiple connections. This only takes the first.
                                }

                                var target = new ISCSITarget
                                {
                                    TargetName = targetName,
                                    PortalAddress = portalAddress,
                                    PortalPort = portalPort,
                                    IsConnected = true, // By definition, these are active sessions
                                    SessionAdapterUnique = sessionAdapterUnique,
                                    SessionAdapterSpecific = sessionAdapterSpecific,
                                    // TODO: Need to call ReportIScsiPersistentLogins to check IsPersistent
                                    // TODO: Need MPIO APIs to check IsMPIOEnabled and Paths
                                    LastConnected = DateTime.MinValue // API doesn't provide this
                                };

                                // TODO: Check if this session is persistent using ReportIScsiPersistentLogins
                                // TODO: Check MPIO status using MPIO APIs

                                connectedTargets.Add(target);

                                // Move pointer to the next session info structure
                                currentPtr = IntPtr.Add(currentPtr, sessionInfoSize);
                            }
                            return connectedTargets;
                        }
                        else
                        {
                            throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Failed to get iSCSI session list. Error code: {result}");
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(buffer);
                    }
                }
                else if (result == ERROR_SUCCESS)
                {
                    // No active sessions found
                    return new List<ISCSITarget>();
                }
                else
                {
                    throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Failed to get buffer size for iSCSI session list. Error code: {result}");
                }
            });
        }

        // --- MPIO Service Methods ---

        public async Task<List<MPIOPathInfo>> GetMPIOPathsAsync(ulong mpioDiskId = 0) // Default to 0, common for MS DSM
        {
            return await Task.Run(() =>
            {
                uint numPaths = 0;
                IntPtr pathsPtr = IntPtr.Zero;
                uint result = MpioGetPathInfo(mpioDiskId, ref numPaths, out pathsPtr);

                if (result != ERROR_SUCCESS)
                {
                    if (pathsPtr != IntPtr.Zero) LocalFree(pathsPtr); // Clean up on error
                    throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Failed to get MPIO path info. Error code: {result}");
                }

                if (numPaths == 0 || pathsPtr == IntPtr.Zero)
                {
                    return new List<MPIOPathInfo>(); // No paths found
                }

                List<MPIOPathInfo> paths = new List<MPIOPathInfo>();
                IntPtr currentPtr = pathsPtr;
                int pathInfoSize = Marshal.SizeOf(typeof(MPIO_PATH_INFO));

                try
                {
                    for (int i = 0; i < numPaths; i++)
                    {
                        MPIO_PATH_INFO rawPathInfo = (MPIO_PATH_INFO)Marshal.PtrToStructure(currentPtr, typeof(MPIO_PATH_INFO));
                        paths.Add(new MPIOPathInfo
                        {
                            PathId = rawPathInfo.PathId,
                            ConnectionId = rawPathInfo.ConnectionId.ToString("X"), // Match format used elsewhere
                            State = (PathStatus)rawPathInfo.PathState, // Assuming PathStatus enum maps correctly
                            BusNumber = rawPathInfo.BusNumber,
                            TargetId = rawPathInfo.TargetId,
                            Lun = rawPathInfo.Lun
                        });
                        currentPtr = IntPtr.Add(currentPtr, pathInfoSize);
                    }
                }
                finally
                {
                    LocalFree(pathsPtr); // MUST free the allocated buffer
                }

                return paths;
            });
        }

        public async Task<bool> SetMPIOLoadBalancePolicyAsync(uint policy, ulong mpioDiskId = 0)
        {
            return await Task.Run(() =>
            {
                DSM_LOAD_BALANCE_POLICY lbPolicy = new DSM_LOAD_BALANCE_POLICY
                {
                    Version = 1,
                    LoadBalancePolicy = policy
                };

                uint result = MpioSetLoadBalancePolicy(mpioDiskId, ref lbPolicy);

                if (result == ERROR_SUCCESS)
                {
                    return true;
                }
                else
                {
                    throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Failed to set MPIO load balance policy. Error code: {result}");
                }
            });
        }

        // Note: MpioRegisterDevice is typically used for non-PNP devices or specific scenarios.
        // For standard iSCSI LUNs managed by MS DSM, registration might not be needed explicitly.
        // This method is provided for completeness but might require specific device paths.
        public async Task<bool> RegisterMPIODeviceAsync(string devicePath, ulong mpioDiskId = 0)
        {
             return await Task.Run(() =>
             {
                 uint result = MpioRegisterDevice(mpioDiskId, devicePath);
                 if (result == ERROR_SUCCESS)
                 {
                     return true;
                 }
                 else
                 {
                     // Consider specific errors like ERROR_DEVICE_ALREADY_REGISTERED
                     throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Failed to register MPIO device '{devicePath}'. Error code: {result}");
                 }
             });
        }

        // Helper method to potentially map iSCSI sessions to MPIO devices (complex)
        // This is a simplified example and might need refinement based on how MPIO Disk IDs are assigned.
        public async Task<ulong?> FindMpioDiskIdForSessionAsync(ISCSI_UNIQUE_SESSION_ID sessionId)
        {
            // This requires correlating iSCSI session/connection info with MPIO device info.
            // MpioGetDeviceList might be needed here to iterate through MPIO devices and check
            // properties or associated paths to find a match with the session's connections.
            // This is non-trivial and depends heavily on the system's MPIO configuration.
            // Returning null for now as a placeholder.
            await Task.Delay(10); // Simulate async work
            // Implementation using MpioGetDeviceList
            return await Task.Run(() =>
            {
                uint numDevices = 0;
                IntPtr devicesPtr = IntPtr.Zero;
                uint result = MpioGetDeviceList(out numDevices, out devicesPtr);

                if (result != ERROR_SUCCESS)
                {
                    if (devicesPtr != IntPtr.Zero) LocalFree(devicesPtr);
                    throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Failed to get MPIO device list. Error code: {result}");
                }

                if (numDevices == 0 || devicesPtr == IntPtr.Zero)
                {
                    return (ulong?)null; // No MPIO devices found
                }

                ulong? foundDiskId = null;
                IntPtr currentPtr = devicesPtr;
                int devInfoSize = Marshal.SizeOf(typeof(MPIO_DEVINSTANCE_INFO));

                try
                {
                    for (int i = 0; i < numDevices; i++)
                    {
                        MPIO_DEVINSTANCE_INFO devInfo = (MPIO_DEVINSTANCE_INFO)Marshal.PtrToStructure(currentPtr, typeof(MPIO_DEVINSTANCE_INFO));

                        // Now, get paths for this device to find connection IDs
                        uint numPaths = 0;
                        IntPtr pathsPtr = IntPtr.Zero;
                        result = MpioGetPathInfo(devInfo.MpioDiskId, ref numPaths, out pathsPtr);

                        if (result == ERROR_SUCCESS && numPaths > 0 && pathsPtr != IntPtr.Zero)
                        {
                            IntPtr currentPathPtr = pathsPtr;
                            int pathInfoSize = Marshal.SizeOf(typeof(MPIO_PATH_INFO));
                            try
                            {
                                for (int j = 0; j < numPaths; j++)
                                {
                                    MPIO_PATH_INFO pathInfo = (MPIO_PATH_INFO)Marshal.PtrToStructure(currentPathPtr, typeof(MPIO_PATH_INFO));
                                    // Compare the AdapterSpecific part of the session ID with the ConnectionId from MPIO path
                                    // This assumes a direct correlation, which might need adjustment based on specific DSM behavior.
                                    if (pathInfo.ConnectionId == sessionId.AdapterSpecific)
                                    {
                                        foundDiskId = devInfo.MpioDiskId;
                                        break; // Found a match
                                    }
                                    currentPathPtr = IntPtr.Add(currentPathPtr, pathInfoSize);
                                }
                            }
                            finally
                            {
                                LocalFree(pathsPtr);
                            }
                        }
                        else if (result != ERROR_SUCCESS && pathsPtr != IntPtr.Zero)
                        {
                             LocalFree(pathsPtr); // Clean up even on error
                        }

                        if (foundDiskId.HasValue)
                        {
                            break; // Found the disk ID, exit outer loop
                        }

                        currentPtr = IntPtr.Add(currentPtr, devInfoSize);
                    }
                }
                finally
                {
                    LocalFree(devicesPtr); // MUST free the device list buffer
                }

                return foundDiskId;
            });
        }

        // --- iSCSI Target Management Methods (Requires iscsitgt.dll and iSCSI Target Server Role) ---

        public async Task<IntPtr> CreateIScsiTargetAsync(string targetName, string description = null)
        {
            // Note: Creating LUN mappings (LunMappings) is complex and requires additional structures/APIs.
            // This example assumes no LUNs are mapped initially.
            return await Task.Run(() =>
            {
                IntPtr targetId = IntPtr.Zero;
                // Passing IntPtr.Zero for LUN mappings and 0 for count
                uint result = CreateIScsiTarget(targetName, description ?? string.Empty, IntPtr.Zero, 0, out targetId);

                if (result == ERROR_SUCCESS)
                {
                    return targetId;
                }
                else
                {
                    throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Failed to create iSCSI target '{targetName}'. Error code: {result}");
                }
            });
        }

        public async Task<bool> DeleteIScsiTargetAsync(IntPtr targetId)
        {
            return await Task.Run(() =>
            {
                uint result = DeleteIScsiTarget(targetId);
                if (result == ERROR_SUCCESS)
                {
                    return true;
                }
                else
                {
                    // Consider specific error codes if needed
                    throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Failed to delete iSCSI target with ID '{targetId}'. Error code: {result}");
                }
            });
        }

        public async Task<bool> ConfigureIScsiTargetChapAsync(IntPtr targetId, bool enableChap, string chapUsername, string chapSecret)
        {
             return await Task.Run(() =>
             {
                 uint result = SetIScsiTargetCHAPInfo(targetId, enableChap, chapUsername, chapSecret);
                 if (result == ERROR_SUCCESS)
                 {
                     return true;
                 }
                 else
                 {
                     throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Failed to configure CHAP for iSCSI target ID '{targetId}'. Error code: {result}");
                 }
             });
        }

        public async Task<bool> SetMpioLoadBalancePolicyAsync(ulong mpioDiskId, uint policy)
        {
            return await Task.Run(() =>
            {
                DSM_LOAD_BALANCE_POLICY lbPolicy = new DSM_LOAD_BALANCE_POLICY
                {
                    Version = 1,
                    LoadBalancePolicy = policy,
                    Reserved1 = 0,
                    Reserved2 = 0
                };

                uint result = MpioSetLoadBalancePolicy(mpioDiskId, ref lbPolicy);

                if (result == ERROR_SUCCESS)
                {
                    return true;
                }
                else
                {
                    throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Failed to set MPIO load balance policy for Disk ID {mpioDiskId}. Error code: {result}");
                }
            });
        }

    }
}

        // Placeholder - Needs implementation with actual Target API calls (iscsitgt.dll)
        public async Task<List<ISCSITarget>> GetLocalTargetsAsync()
        {
            // Simulate async work
            await Task.Delay(800);

            // In a real implementation, this would call the Windows iSCSI Target API
            // For demonstration, we'll return sample local targets
            List<ISCSITarget> localTargets = new List<ISCSITarget>();
            localTargets.Add(new ISCSITarget
            {
                TargetName = "iqn.1991-05.com.microsoft:local-target-1-simulated",
                PortalAddress = "127.0.0.1", // Typically localhost for local targets
                PortalPort = 3260,
                IsLocalTarget = true,
                SizeGB = 100,
                BackingStoragePath = "C:\\iSCSIStorage\\target1.vhdx"
            });
            return localTargets;
        }

        // Placeholder - Needs implementation with actual Target API calls (CreateIScsiTarget)
        public async Task<bool> CreateTargetAsync(ISCSITarget target, string chapUsername = null, string chapPassword = null)
        {
            // Simulate async work
            await Task.Delay(2000);

            // In a real implementation, this would call the Windows iSCSI Target API
            // For demonstration, we'll simulate a successful target creation
            target.IsLocalTarget = true;
            // Need to validate BackingStoragePath and SizeGB
            // Call CreateIScsiTarget and ConfigureIScsiTargetSecurity if CHAP is needed
            Console.WriteLine($"Simulating creation of target: {target.TargetName}");
            return true;
        }

        // Placeholder - Needs implementation with actual Target API calls (DeleteIScsiTarget)
        public async Task<bool> DeleteTargetAsync(ISCSITarget target)
        {
            // Simulate async work
            await Task.Delay(1500);

            // In a real implementation, this would call the Windows iSCSI Target API
            // For demonstration, we'll simulate a successful target deletion
            Console.WriteLine($"Simulating deletion of target: {target.TargetName}");
            return true;
        }

        // Placeholder - Needs implementation with MPIO API calls
        public async Task<bool> EnableMPIOAsync(ISCSITarget target, List<ISCSIPath> additionalPaths, MPIOPolicy policy)
        {
            // Simulate async work
            await Task.Delay(1500);

            // In a real implementation, this would configure MPIO for the target
            // For demonstration, we'll simulate a successful MPIO configuration
            target.IsMPIOEnabled = true;
            target.LoadBalancePolicy = policy;
            foreach (var path in additionalPaths)
            {
                path.PathSessionId = Guid.NewGuid().ToString().Substring(0, 8); // Simulate path connection
                path.Status = PathStatus.Active;
                target.Paths.Add(path);
            }
            Console.WriteLine($"Simulating enabling MPIO for target: {target.TargetName}");
            return true;
        }

        // Placeholder - Needs implementation with MPIO API calls
        public async Task<bool> DisableMPIOAsync(ISCSITarget target)
        {
            // Simulate async work
            await Task.Delay(1000);

            // In a real implementation, this would disable MPIO for the target
            // For demonstration, we'll simulate a successful MPIO disabling
            target.IsMPIOEnabled = false;
            target.Paths.Clear();
            Console.WriteLine($"Simulating disabling MPIO for target: {target.TargetName}");
            return true;
        }

        // Placeholder - Needs implementation with MPIO API calls
        public async Task<List<PathStatus>> GetPathStatusesAsync(ISCSITarget target)
        {
            // Simulate async work
            await Task.Delay(300);

            // In a real implementation, this would get the status of each path via API
            // For demonstration, we'll return the current path statuses
            List<PathStatus> statuses = new List<PathStatus>();
            foreach (var path in target.Paths)
            {
                // Simulate potential status changes (e.g., one path fails randomly)
                // if (new Random().Next(10) > 8) path.Status = PathStatus.Failed;
                statuses.Add(path.Status);
            }
            return statuses;
        }

        // Placeholder - Needs implementation with MPIO API calls (SetMPIOLoadBalancePolicy)
        public async Task<bool> SetLoadBalancePolicyAsync(ISCSITarget target, MPIOPolicy policy)
        {
            // Simulate async work
            await Task.Delay(500);

            // In a real implementation, this would set the load balance policy via API
            // For demonstration, we'll simulate a successful policy change
            target.LoadBalancePolicy = policy;
            Console.WriteLine($"Simulating setting load balance policy to {policy} for target: {target.TargetName}");
            return true;
        }
    }
}