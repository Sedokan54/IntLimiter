using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Net;
using NetLimiterClone.Models;

namespace NetLimiterClone.Services
{
    public class WFPService : IDisposable
    {
        private IntPtr _engineHandle = IntPtr.Zero;
        private readonly List<ulong> _filterIds = new();
        private readonly object _lockObject = new();
        private bool _disposed = false;

        public WFPService()
        {
            InitializeWFP();
        }

        private void InitializeWFP()
        {
            try
            {
                var session = new FWPM_SESSION0
                {
                    displayData = new FWPM_DISPLAY_DATA0
                    {
                        name = "NetLimiter Clone WFP Session",
                        description = "Network traffic control session"
                    },
                    flags = FWPM_SESSION_FLAG_DYNAMIC
                };

                var result = FwpmEngineOpen0(null, RPC_C_AUTHN_WINNT, IntPtr.Zero, ref session, out _engineHandle);
                if (result != 0)
                {
                    throw new Exception($"Failed to open WFP engine: {result:X}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WFP initialization failed: {ex.Message}");
                throw;
            }
        }

        public bool SetProcessBandwidthLimit(int processId, long downloadLimitBps, long uploadLimitBps)
        {
            lock (_lockObject)
            {
                try
                {
                    // Remove existing filters for this process
                    RemoveProcessFilters(processId);

                    // Add download limit filter
                    if (downloadLimitBps > 0)
                    {
                        var downloadFilterId = AddBandwidthFilter(processId, downloadLimitBps, true);
                        if (downloadFilterId != 0)
                        {
                            _filterIds.Add(downloadFilterId);
                        }
                    }

                    // Add upload limit filter
                    if (uploadLimitBps > 0)
                    {
                        var uploadFilterId = AddBandwidthFilter(processId, uploadLimitBps, false);
                        if (uploadFilterId != 0)
                        {
                            _filterIds.Add(uploadFilterId);
                        }
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to set bandwidth limit for process {processId}: {ex.Message}");
                    return false;
                }
            }
        }

        public bool BlockProcess(int processId)
        {
            lock (_lockObject)
            {
                try
                {
                    // Remove existing filters
                    RemoveProcessFilters(processId);

                    // Add blocking filter
                    var blockFilterId = AddBlockingFilter(processId);
                    if (blockFilterId != 0)
                    {
                        _filterIds.Add(blockFilterId);
                        return true;
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to block process {processId}: {ex.Message}");
                    return false;
                }
            }
        }

        public bool UnblockProcess(int processId)
        {
            return RemoveProcessFilters(processId);
        }

        public bool RemoveProcessFilters(int processId)
        {
            lock (_lockObject)
            {
                try
                {
                    var filtersToRemove = new List<ulong>();
                    
                    foreach (var filterId in _filterIds)
                    {
                        // Check if this filter belongs to the process
                        if (IsFilterForProcess(filterId, processId))
                        {
                            var result = FwpmFilterDeleteById0(_engineHandle, filterId);
                            if (result == 0)
                            {
                                filtersToRemove.Add(filterId);
                            }
                        }
                    }

                    // Remove from our tracking list
                    foreach (var filterId in filtersToRemove)
                    {
                        _filterIds.Remove(filterId);
                    }

                    return filtersToRemove.Count > 0;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to remove filters for process {processId}: {ex.Message}");
                    return false;
                }
            }
        }

        private ulong AddBandwidthFilter(int processId, long limitBps, bool isInbound)
        {
            try
            {
                var filter = new FWPM_FILTER0
                {
                    layerKey = isInbound ? FWPM_LAYER_INBOUND_TRANSPORT_V4 : FWPM_LAYER_OUTBOUND_TRANSPORT_V4,
                    displayData = new FWPM_DISPLAY_DATA0
                    {
                        name = $"NetLimiter {(isInbound ? "Download" : "Upload")} Limit PID:{processId}",
                        description = $"Bandwidth limit for process {processId}"
                    },
                    action = new FWPM_ACTION0
                    {
                        type = FWP_ACTION_CALLOUT_INSPECTION
                    },
                    weight = new FWP_VALUE0
                    {
                        type = FWP_UINT64,
                        value = new FWP_VALUE0_UNION { uint64 = 0x8000000000000000UL }
                    },
                    numFilterConditions = 1,
                    filterCondition = new FWPM_FILTER_CONDITION0[]
                    {
                        new FWPM_FILTER_CONDITION0
                        {
                            fieldKey = FWPM_CONDITION_ALE_APP_ID,
                            matchType = FWP_MATCH_EQUAL,
                            conditionValue = new FWP_CONDITION_VALUE0
                            {
                                type = FWP_UINT32,
                                value = new FWP_VALUE0_UNION { uint32 = (uint)processId }
                            }
                        }
                    }
                };

                var result = FwpmFilterAdd0(_engineHandle, ref filter, IntPtr.Zero, out var filterId);
                if (result != 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to add bandwidth filter: {result:X}");
                    return 0;
                }

                return filterId;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception in AddBandwidthFilter: {ex.Message}");
                return 0;
            }
        }

        private ulong AddBlockingFilter(int processId)
        {
            try
            {
                var filter = new FWPM_FILTER0
                {
                    layerKey = FWPM_LAYER_ALE_AUTH_CONNECT_V4,
                    displayData = new FWPM_DISPLAY_DATA0
                    {
                        name = $"NetLimiter Block PID:{processId}",
                        description = $"Block network access for process {processId}"
                    },
                    action = new FWPM_ACTION0
                    {
                        type = FWP_ACTION_BLOCK
                    },
                    weight = new FWP_VALUE0
                    {
                        type = FWP_UINT64,
                        value = new FWP_VALUE0_UNION { uint64 = 0x8000000000000000UL }
                    },
                    numFilterConditions = 1,
                    filterCondition = new FWPM_FILTER_CONDITION0[]
                    {
                        new FWPM_FILTER_CONDITION0
                        {
                            fieldKey = FWPM_CONDITION_ALE_APP_ID,
                            matchType = FWP_MATCH_EQUAL,
                            conditionValue = new FWP_CONDITION_VALUE0
                            {
                                type = FWP_UINT32,
                                value = new FWP_VALUE0_UNION { uint32 = (uint)processId }
                            }
                        }
                    }
                };

                var result = FwpmFilterAdd0(_engineHandle, ref filter, IntPtr.Zero, out var filterId);
                if (result != 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to add blocking filter: {result:X}");
                    return 0;
                }

                return filterId;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception in AddBlockingFilter: {ex.Message}");
                return 0;
            }
        }

        private bool IsFilterForProcess(ulong filterId, int processId)
        {
            // This is a simplified check - in a real implementation,
            // you'd query the filter details and check if it matches the process
            return true;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            lock (_lockObject)
            {
                try
                {
                    // Remove all filters
                    foreach (var filterId in _filterIds)
                    {
                        FwpmFilterDeleteById0(_engineHandle, filterId);
                    }
                    _filterIds.Clear();

                    // Close WFP engine
                    if (_engineHandle != IntPtr.Zero)
                    {
                        FwpmEngineClose0(_engineHandle);
                        _engineHandle = IntPtr.Zero;
                    }

                    _disposed = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in WFP dispose: {ex.Message}");
                }
            }
        }

        #region WFP API Declarations

        [DllImport("fwpuclnt.dll", CharSet = CharSet.Unicode)]
        private static extern uint FwpmEngineOpen0(
            [MarshalAs(UnmanagedType.LPWStr)] string serverName,
            uint authnService,
            IntPtr authIdentity,
            ref FWPM_SESSION0 session,
            out IntPtr engineHandle);

        [DllImport("fwpuclnt.dll")]
        private static extern uint FwpmEngineClose0(IntPtr engineHandle);

        [DllImport("fwpuclnt.dll")]
        private static extern uint FwpmFilterAdd0(
            IntPtr engineHandle,
            ref FWPM_FILTER0 filter,
            IntPtr sd,
            out ulong id);

        [DllImport("fwpuclnt.dll")]
        private static extern uint FwpmFilterDeleteById0(
            IntPtr engineHandle,
            ulong id);

        #endregion

        #region WFP Structures and Constants

        private const uint RPC_C_AUTHN_WINNT = 10;
        private const uint FWPM_SESSION_FLAG_DYNAMIC = 0x00000001;
        private const uint FWP_ACTION_BLOCK = 0x00000001;
        private const uint FWP_ACTION_CALLOUT_INSPECTION = 0x00000003;
        private const uint FWP_MATCH_EQUAL = 0;
        private const uint FWP_UINT32 = 0;
        private const uint FWP_UINT64 = 1;

        private static readonly Guid FWPM_LAYER_INBOUND_TRANSPORT_V4 = new("5926dfc8-e3cf-4426-a283-dc393f5d0f9d");
        private static readonly Guid FWPM_LAYER_OUTBOUND_TRANSPORT_V4 = new("09e61aea-d214-46e2-9b21-b26b0b2f28c8");
        private static readonly Guid FWPM_LAYER_ALE_AUTH_CONNECT_V4 = new("c38d57d1-05a7-4c33-904f-7fbceee60e82");
        private static readonly Guid FWPM_CONDITION_ALE_APP_ID = new("d78e1e87-8644-4ea5-9437-d809ecefc971");

        [StructLayout(LayoutKind.Sequential)]
        private struct FWPM_SESSION0
        {
            public Guid sessionKey;
            public FWPM_DISPLAY_DATA0 displayData;
            public uint flags;
            public uint txnWaitTimeoutInMSec;
            public uint processId;
            public IntPtr sid;
            public IntPtr username;
            public bool kernelMode;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct FWPM_DISPLAY_DATA0
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string name;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string description;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct FWPM_FILTER0
        {
            public Guid filterKey;
            public FWPM_DISPLAY_DATA0 displayData;
            public uint flags;
            public IntPtr providerKey;
            public IntPtr providerData;
            public Guid layerKey;
            public Guid subLayerKey;
            public FWP_VALUE0 weight;
            public uint numFilterConditions;
            [MarshalAs(UnmanagedType.LPArray)]
            public FWPM_FILTER_CONDITION0[] filterCondition;
            public FWPM_ACTION0 action;
            public IntPtr providerContextKey;
            public Guid reserved;
            public ulong filterId;
            public FWP_VALUE0 effectiveWeight;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct FWPM_FILTER_CONDITION0
        {
            public Guid fieldKey;
            public uint matchType;
            public FWP_CONDITION_VALUE0 conditionValue;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct FWP_CONDITION_VALUE0
        {
            public uint type;
            public FWP_VALUE0_UNION value;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct FWPM_ACTION0
        {
            public uint type;
            public Guid filterType;
            public Guid calloutKey;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct FWP_VALUE0
        {
            public uint type;
            public FWP_VALUE0_UNION value;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct FWP_VALUE0_UNION
        {
            [FieldOffset(0)]
            public uint uint32;
            [FieldOffset(0)]
            public ulong uint64;
            [FieldOffset(0)]
            public IntPtr ptr;
        }

        #endregion
    }
}