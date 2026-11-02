using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Media.Devices;
using static SonarFlyout.Native;

namespace SonarFlyout;

// Undocumented, but it's how the Sound control panel and Sonar itself change the default device; Windows has no
// public API for it. Only the vtable position of SetDefaultEndpoint matters, so the methods before it take no arguments.
[GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)]
[Guid("f8679f50-850a-41cf-9c72-430f290290c8")]
partial interface IPolicyConfig
{
    void GetMixFormat();
    void GetDeviceFormat();
    void ResetDeviceFormat();
    void SetDeviceFormat();
    void GetProcessingPeriod();
    void SetProcessingPeriod();
    void GetShareMode();
    void SetShareMode();
    void GetPropertyValue();
    void SetPropertyValue();
    void SetDefaultEndpoint(string deviceId, int role);
}

static class DefaultDevices
{
    const int Console = 0, Multimedia = 1, Communications = 2;
    static readonly Guid s_policyConfigClient = new("870af99c-171d-4f9e-af0d-e63df40c2bc9");

    public static (bool Sound, bool Mic) AreSonar(SonarState state) => (
        IsOneOf(MediaDevice.GetDefaultAudioRenderId(AudioDeviceRole.Default), state.SonarOutputs),
        state.SonarMic is null || IsOneOf(MediaDevice.GetDefaultAudioCaptureId(AudioDeviceRole.Default), [state.SonarMic]));

    // MediaDevice returns device interface paths that embed the endpoint id Sonar reports.
    static bool IsOneOf(string defaultId, IEnumerable<string> endpointIds) =>
        endpointIds.Any(id => defaultId.Contains(id, StringComparison.OrdinalIgnoreCase));

    public static void UseSonar(SonarState state)
    {
        if (state.GameOutput is null || state.ChatOutput is null)
            throw new InvalidOperationException(Strings.Get("DefaultDevicesFailed"));

        Marshal.ThrowExceptionForHR(CoCreateInstance(s_policyConfigClient, 0, CLSCTX_ALL, typeof(IPolicyConfig).GUID, out nint instance));
        IPolicyConfig policy;
        try
        {
            policy = (IPolicyConfig)new StrategyBasedComWrappers().GetOrCreateObjectForComInstance(instance, CreateObjectFlags.None);
        }
        finally
        {
            Marshal.Release(instance);
        }

        policy.SetDefaultEndpoint(state.GameOutput, Console);
        policy.SetDefaultEndpoint(state.GameOutput, Multimedia);
        policy.SetDefaultEndpoint(state.ChatOutput, Communications);
        if (state.SonarMic is not null)
            foreach (int role in (int[])[Console, Multimedia, Communications])
                policy.SetDefaultEndpoint(state.SonarMic, role);
    }
}
