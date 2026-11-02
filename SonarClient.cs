using System.Globalization;
using System.Text.Json;

namespace SonarFlyout;

public record Device(string Id, string Name, string Glyph);

public record ChannelVolume(double Volume, bool Muted);

public sealed class SonarState
{
    public required string Mode { get; init; }
    public required Dictionary<string, ChannelVolume> Volumes { get; init; }
    public required Dictionary<string, string> Routes { get; init; }
    public required List<Device> Outputs { get; init; }
    public required List<Device> Inputs { get; init; }
    public required HashSet<string> EnabledChannels { get; init; }
    public required bool OutputsLinked { get; init; }
    public required HashSet<string> SonarOutputs { get; init; }
    public string? GameOutput { get; init; }
    public string? ChatOutput { get; init; }
    public string? SonarMic { get; init; }
}

public sealed class SonarClient
{
    public const string MasterChannel = "Master";

    // GG serves its local API over HTTPS with a self-signed certificate generated at startup.
    static readonly HttpClient s_gg = new(new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, _, _, _) => true })
    {
        Timeout = TimeSpan.FromSeconds(3),
    };

    readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(3) };
    Uri? _baseUri;

    static async Task<Uri> DiscoverAsync()
    {
        var corePropsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "SteelSeries", "SteelSeries Engine 3", "coreProps.json");
        using var coreProps = JsonDocument.Parse(await File.ReadAllTextAsync(corePropsPath));
        var ggAddress = coreProps.RootElement.GetProperty("ggEncryptedAddress").GetString();

        using var subApps = JsonDocument.Parse(await s_gg.GetStringAsync($"https://{ggAddress}/subApps"));
        var sonar = subApps.RootElement.GetProperty("subApps").GetProperty("sonar");
        var address = sonar.GetProperty("metadata").GetProperty("webServerAddress").GetString();
        if (!sonar.GetProperty("isRunning").GetBoolean() || string.IsNullOrEmpty(address))
            throw new InvalidOperationException(Strings.Get("ErrorUnreachable"));
        return new Uri(address + "/");
    }

    async Task<string> SendAsync(HttpMethod method, string path)
    {
        // Sonar picks a new port whenever GG restarts, so rediscover once on failure.
        for (int attempt = 0; ; attempt++)
        {
            try
            {
                _baseUri ??= await DiscoverAsync();
                using var response = await _http.SendAsync(new HttpRequestMessage(method, new Uri(_baseUri, path)));
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch when (attempt == 0)
            {
                _baseUri = null;
            }
        }
    }

    public async Task<SonarState> GetStateAsync()
    {
        var mode = SendAsync(HttpMethod.Get, "mode");
        var volumes = GetVolumesAsync();
        var routes = SendAsync(HttpMethod.Get, "classicRedirections");
        var devices = SendAsync(HttpMethod.Get, "audioDevices");
        var linked = SendAsync(HttpMethod.Get, "linkAll/isRenderLinkAllEnabled");

        using var modeJson = JsonDocument.Parse(await mode);
        using var routesJson = JsonDocument.Parse(await routes);
        using var devicesJson = JsonDocument.Parse(await devices);

        var physical = devicesJson.RootElement.EnumerateArray()
            .Where(d => !d.GetProperty("isVad").GetBoolean() && d.GetProperty("state").GetString() == "active")
            .ToList();
        var virtualDevices = devicesJson.RootElement.EnumerateArray()
            .Where(d => d.GetProperty("isVad").GetBoolean() && d.GetProperty("state").GetString() == "active")
            .Select(d => (Id: d.GetProperty("id").GetString()!, Flow: d.GetProperty("dataFlow").GetString(), Role: d.GetProperty("role").GetString()))
            .ToList();
        // Sonar also exposes its microphone as a render endpoint (the monitor), which isn't a playback target.
        var sonarOutputs = virtualDevices.Where(d => d.Flow == "render" && d.Role != "chatCapture").ToList();

        return new SonarState
        {
            Mode = modeJson.RootElement.GetString() ?? "",
            Volumes = await volumes,
            Routes = routesJson.RootElement.EnumerateArray()
                .ToDictionary(r => r.GetProperty("id").GetString()!, r => r.GetProperty("deviceId").GetString()!),
            Outputs = DevicesOfFlow(physical, "render"),
            Inputs = DevicesOfFlow(physical, "capture"),
            // Sonar only creates a virtual device for channels that are turned on (Aux is opt-in).
            EnabledChannels = devicesJson.RootElement.EnumerateArray()
                .Where(d => d.GetProperty("isVad").GetBoolean())
                .Select(d => d.GetProperty("role").GetString()!)
                .ToHashSet(),
            OutputsLinked = await linked == "true",
            SonarOutputs = sonarOutputs.Select(d => d.Id).ToHashSet(),
            GameOutput = sonarOutputs.FirstOrDefault(d => d.Role == "game").Id,
            ChatOutput = sonarOutputs.FirstOrDefault(d => d.Role == "chatRender").Id,
            SonarMic = virtualDevices.FirstOrDefault(d => d.Flow == "capture" && d.Role == "chatCapture").Id,
        };
    }

    public async Task<Dictionary<string, ChannelVolume>> GetVolumesAsync()
    {
        using var json = JsonDocument.Parse(await SendAsync(HttpMethod.Get, "volumeSettings/classic"));
        var volumes = json.RootElement.GetProperty("devices").EnumerateObject()
            .ToDictionary(p => p.Name, p => ReadVolume(p.Value));
        volumes[MasterChannel] = ReadVolume(json.RootElement.GetProperty("masters"));
        return volumes;
    }

    static ChannelVolume ReadVolume(JsonElement element)
    {
        var classic = element.GetProperty("classic");
        return new(classic.GetProperty("volume").GetDouble(), classic.GetProperty("muted").GetBoolean());
    }

    static List<Device> DevicesOfFlow(List<JsonElement> devices, string flow) => devices
        .Where(d => d.GetProperty("dataFlow").GetString() == flow)
        .Select(d =>
        {
            var id = d.GetProperty("id").GetString()!;
            var glyph = flow == "capture" ? "\uE720" : IsHeadphones(id) ? "\uE7F6" : "\uE7F5";
            return new Device(id, DeviceName(d.GetProperty("friendlyName").GetString()!), glyph);
        })
        .OrderBy(d => d.Name, StringComparer.CurrentCulture)
        .ToList();

    // Windows friendly names read "<endpoint kind> (<device name>)" in every language, e.g. "Headphones (Hades Wireless Headset)".
    static string DeviceName(string friendlyName)
    {
        int open = friendlyName.IndexOf('(');
        return open > 0 && friendlyName.EndsWith(')') ? friendlyName[(open + 1)..^1] : friendlyName;
    }

    // Sonar's device ids are Windows audio endpoint ids ("{0.0.0.00000000}.{guid}"). The endpoint's form factor
    // (PKEY_AudioEndpoint_FormFactor) tells headphones from speakers without relying on the localized name; it isn't
    // exposed through DeviceInformation, but the audio service mirrors the endpoint's property store here.
    static bool IsHeadphones(string endpointId)
    {
        var guid = endpointId[(endpointId.LastIndexOf('.') + 1)..];
        var formFactor = Microsoft.Win32.Registry.GetValue(
            $@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\Render\{guid}\Properties",
            "{1da5d803-d492-4edd-8c23-e0c0ffee7f0e},0", null);
        // EndpointFormFactor: 3 Headphones, 5 Headset, 6 Handset.
        return formFactor is int and (3 or 5 or 6);
    }

    public Task SetVolumeAsync(string channel, double volume) =>
        SendAsync(HttpMethod.Put, $"volumeSettings/classic/{channel}/Volume/{volume.ToString("0.00", CultureInfo.InvariantCulture)}");

    public Task SetMutedAsync(string channel, bool muted) =>
        SendAsync(HttpMethod.Put, $"volumeSettings/classic/{channel}/Mute/{(muted ? "true" : "false")}");

    public static void OpenInGG() =>
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("ssgg://gg/sonar") { UseShellExecute = true });

    public Task SetOutputsLinkedAsync(bool linked, string sourceChannel) =>
        SendAsync(HttpMethod.Put, linked ? $"linkAll/link/{sourceChannel}" : "linkAll/unlink");

    public Task SetOutputAsync(string route, string deviceId) =>
        SendAsync(HttpMethod.Put, $"classicRedirections/{route}/deviceId/{Uri.EscapeDataString(deviceId)}");
}
