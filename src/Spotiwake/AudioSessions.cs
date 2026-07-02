using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Spotiwake;

/// <summary>
/// Consulta as sessões de áudio do Windows (WASAPI) para verificar se um
/// processo específico está reproduzindo som no dispositivo de saída padrão.
/// </summary>
internal static class AudioSessions
{
    private const float DefaultPeakThreshold = 0.001f;

    public static bool IsProcessPlayingAudio(string processName, float peakThreshold = DefaultPeakThreshold)
    {
        try
        {
            var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
            enumerator.GetDefaultAudioEndpoint(EDataFlow.Render, ERole.Multimedia, out IMMDevice device);

            var managerIid = typeof(IAudioSessionManager2).GUID;
            device.Activate(ref managerIid, ClsCtx.All, IntPtr.Zero, out object managerObj);
            var manager = (IAudioSessionManager2)managerObj;

            manager.GetSessionEnumerator(out IAudioSessionEnumerator sessions);
            sessions.GetCount(out int count);

            for (int i = 0; i < count; i++)
            {
                sessions.GetSession(i, out IAudioSessionControl session);

                if (session is not IAudioSessionControl2 session2)
                {
                    continue;
                }

                session2.GetState(out AudioSessionState state);
                if (state != AudioSessionState.Active)
                {
                    continue;
                }

                session2.GetProcessId(out uint pid);
                if (pid == 0 || !ProcessNameMatches(pid, processName))
                {
                    continue;
                }

                if (session is IAudioMeterInformation meter)
                {
                    meter.GetPeakValue(out float peak);
                    if (peak > peakThreshold)
                    {
                        return true;
                    }
                }
            }
        }
        catch
        {
            // Sem dispositivo de áudio, acesso negado ou falha de COM:
            // trata como "sem áudio" e deixa a detecção por título decidir.
        }

        return false;
    }

    private static bool ProcessNameMatches(uint pid, string processName)
    {
        try
        {
            using var process = Process.GetProcessById((int)pid);
            return string.Equals(process.ProcessName, processName, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    #region Interop WASAPI

    private enum EDataFlow
    {
        Render = 0,
        Capture = 1,
        All = 2,
    }

    private enum ERole
    {
        Console = 0,
        Multimedia = 1,
        Communications = 2,
    }

    private enum AudioSessionState
    {
        Inactive = 0,
        Active = 1,
        Expired = 2,
    }

    private static class ClsCtx
    {
        public const uint All = 0x17;
    }

    [ComImport]
    [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    private class MMDeviceEnumeratorComObject
    {
    }

    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        int EnumAudioEndpoints(EDataFlow dataFlow, uint stateMask, out IntPtr devices);

        void GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice device);

        // Demais métodos (GetDevice, Register/UnregisterEndpointNotificationCallback) omitidos:
        // não são usados e vêm depois na vtable.
    }

    [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        void Activate(ref Guid iid, uint clsCtx, IntPtr activationParams,
            [MarshalAs(UnmanagedType.IUnknown)] out object iface);

        // Demais métodos (OpenPropertyStore, GetId, GetState) omitidos.
    }

    [Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioSessionManager2
    {
        // IAudioSessionManager
        int GetAudioSessionControl(ref Guid audioSessionGuid, uint streamFlags, out IntPtr sessionControl);

        int GetSimpleAudioVolume(ref Guid audioSessionGuid, uint streamFlags, out IntPtr audioVolume);

        // IAudioSessionManager2
        void GetSessionEnumerator(out IAudioSessionEnumerator sessionEnum);

        int RegisterSessionNotification(IntPtr sessionNotification);

        int UnregisterSessionNotification(IntPtr sessionNotification);

        int RegisterDuckNotification([MarshalAs(UnmanagedType.LPWStr)] string sessionId, IntPtr duckNotification);

        int UnregisterDuckNotification(IntPtr duckNotification);
    }

    [Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioSessionEnumerator
    {
        void GetCount(out int sessionCount);

        void GetSession(int sessionIndex, out IAudioSessionControl session);
    }

    [Guid("F4B1A599-7266-4319-A8CA-E70ACB11E8CD")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioSessionControl
    {
        void GetState(out AudioSessionState state);

        int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string displayName);

        int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)] string displayName, ref Guid eventContext);

        int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string iconPath);

        int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string iconPath, ref Guid eventContext);

        int GetGroupingParam(out Guid groupingParam);

        int SetGroupingParam(ref Guid groupingParam, ref Guid eventContext);

        int RegisterAudioSessionNotification(IntPtr notifications);

        int UnregisterAudioSessionNotification(IntPtr notifications);
    }

    [Guid("BFB7FF88-7239-4FC9-8FA2-07C950BE9C6D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioSessionControl2
    {
        // IAudioSessionControl (a vtable exige redeclarar os métodos da base)
        void GetState(out AudioSessionState state);

        int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string displayName);

        int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)] string displayName, ref Guid eventContext);

        int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string iconPath);

        int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string iconPath, ref Guid eventContext);

        int GetGroupingParam(out Guid groupingParam);

        int SetGroupingParam(ref Guid groupingParam, ref Guid eventContext);

        int RegisterAudioSessionNotification(IntPtr notifications);

        int UnregisterAudioSessionNotification(IntPtr notifications);

        // IAudioSessionControl2
        int GetSessionIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string sessionId);

        int GetSessionInstanceIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string sessionInstanceId);

        void GetProcessId(out uint processId);

        [PreserveSig]
        int IsSystemSoundsSession();

        int SetDuckingPreference(bool optOut);
    }

    [Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioMeterInformation
    {
        void GetPeakValue(out float peak);

        // Demais métodos (GetMeteringChannelCount, GetChannelsPeakValues,
        // QueryHardwareSupport) omitidos.
    }

    #endregion
}
