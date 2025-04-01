using AncientMountain.Managed.Data;
using AncientMountain.Managed.Skia;
using SkiaSharp;
using SkiaSharp.Views.Blazor;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Drawing;
using System.IO.Compression;
using System.Numerics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AncientMountain.Managed.Services
{
    public class ESPUiConfig()
    {
        public int ScreenWidth { get; set; } = 2560;
        public int ScreenHeight { get; set; } = 1440;
        public float FOV { get; set; } = 70f;
    }

    public sealed class RadarService
    {
        /// <summary>
        /// All Map Names by their Map ID.
        /// </summary>
        public static readonly FrozenDictionary<string, string> MapNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "woods", "Woods" },
            { "shoreline", "Shoreline" },
            { "rezervbase", "Reserve" },
            { "laboratory", "Labs" },
            { "interchange", "Interchange" },
            { "factory4_day", "Factory" },
            { "factory4_night", "Factory" },
            { "bigmap", "Customs" },
            { "lighthouse", "Lighthouse" },
            { "tarkovstreets", "Streets" },
            { "Sandbox", "Ground Zero" },
            { "Sandbox_high", "Ground Zero" }
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

        public static ESPUiConfig espUiConfig { get; set; } = new ESPUiConfig();
        public static IEnumerable<WebRadarLoot> filteredLoot { get; set; }
        public static IEnumerable<string> playerNames { get; set; }
        public Map CurrentMap { get; set; }
        public Vector3 PlayerLocation { get; set; }

        private static float _scale = 1f;
        /// <summary>
        /// Radar UI Scale Value.
        /// </summary>
        public static float Scale
        {
            get => _scale;
            set
            {
                ScalePaints(value);
                _scale = value;
            }
        }
        /// <summary>
        /// Radar UI Zoom Value.
        /// *Be Sure to Invert this value before usage*
        /// </summary>
        private static float _zoom = 1f; // Default zoom level
        public static float Zoom
        {
            get => _zoom;
            set => _zoom = Math.Clamp(value, 0.5f, 3.5f); // Prevent extreme zoom
        }

        private readonly SignalRService _sr;
        private readonly FrozenDictionary<string, Map> _maps;

        public RadarService(SignalRService sr)
        {
            _sr = sr;
            LoadMaps(ref _maps);
        }
        private SKPoint _mousePosition = new SKPoint(0, 0);

        public void Render(SKPaintSurfaceEventArgs args, string localPlayerName, float panX, float panY,
            IEnumerable<WebRadarLoot> filteredLoot, LootFilterService lootFilter)
        {
            var info = args.Info;
            var canvas = args.Surface.Canvas;
            canvas.Clear(SKColors.Black);

            try
            {
                switch (_sr.ConnectionState)
                {
                    case Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Disconnected:
                        NotConnectedStatus(canvas, info);
                        break;
                    case Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connected:
                        var data = _sr.Data;
                        if (false) //Leaving for debug purposes
                        {
                            var corner = new SKPoint(info.Rect.Left, info.Rect.Top);
                            corner.Offset(0, 12 * RadarService.Scale);
                            canvas.DrawText($"{(DateTime.UtcNow - data?.SendTime)?.TotalMilliseconds:0.0}ms", corner, SKPaints.TextLoot);
                        }

                        if (data is not null &&
                            data.InGame &&
                            data.MapID is string mapID)
                        {                            
                            if (!_maps.TryGetValue(mapID, out var map))
                                map = _maps["default"];

                            CurrentMap = map;
                            var localPlayer = data.Players.FirstOrDefault(x => x.Name?.Equals(localPlayerName, StringComparison.OrdinalIgnoreCase) ?? false);
                            localPlayer ??= data.Players.FirstOrDefault();
                            if (localPlayer is null)
                                break;
                            var localPlayerPos = localPlayer.Position;
                            var localPlayerMapPos = localPlayerPos.ToMapPos(map);
                            var mapParams = GetMapParameters(info, map, localPlayerMapPos); // Map auto follow LocalPlayer
                            var mapCanvasBounds = new SKRect() // Drawing Destination
                            {
                                Left = info.Rect.Left,
                                Right = info.Rect.Right,
                                Top = info.Rect.Top,
                                Bottom = info.Rect.Bottom
                            };
                            // Draw Game Map
                            canvas.DrawImage(map.Image, mapParams.Bounds, mapCanvasBounds, SKPaints.PaintBitmap);
                            // Draw LocalPlayer
                            localPlayer.Draw(canvas, info, mapParams, localPlayer, _mousePosition);
                            PlayerLocation = localPlayer.Position;

                            // Draw other players
                            var allPlayers = data.Players.Where(x => !x.HasExfild && x != localPlayer);
                            foreach (var player in allPlayers)
                            {
                                player.Draw(canvas, info, mapParams, localPlayer, _mousePosition);
                            }

                            playerNames = data.Players.Where(x => x.Type == WebPlayerType.LocalPlayer || x.Type == WebPlayerType.Player || x.Type == WebPlayerType.Teammate).Select(x => x.Name);

                            if (filteredLoot is not null)
                            {
                                foreach (var lootItem in filteredLoot)
                                {
                                    if (lootItem.ShortName.StartsWith("Q_"))
                                        continue;
                                    lootItem.Draw(canvas, info, mapParams, localPlayer, lootFilter);
                                }
                            }
                        }
                        else
                        {
                            WaitingForRaidStatus(canvas, info);
                        }
                        break;
                    case Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connecting:
                        ConnectingStatus(canvas, info);
                        break;
                    case Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Reconnecting:
                        ReConnectingStatus(canvas, info);
                        break;
                }
            }
            catch { }

            canvas.Flush();
        }

        public void RenderESP(SKPaintSurfaceEventArgs args, string localPlayerName, IEnumerable<WebRadarLoot> filteredLoot)
        {
            var info = args.Info;
            var canvas = args.Surface.Canvas;
            canvas.Clear(SKColors.Black);

            try
            {
                switch (_sr.ConnectionState)
                {
                    case Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Disconnected:
                        NotConnectedStatus(canvas, info, "ESP");
                        break;
                    case Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connected:
                        var data = _sr.Data;
                        if (data is not null &&
                            data.InGame &&
                            data.MapID is string mapID)
                        {
                            if (!_maps.TryGetValue(mapID, out var map))
                                map = _maps["default"];
                            var localPlayer = data.Players.FirstOrDefault(x => x.Name?.Equals(localPlayerName, StringComparison.OrdinalIgnoreCase) ?? false);
                            localPlayer ??= data.Players.FirstOrDefault();
                            if (localPlayer is null)
                                break;

                            if (filteredLoot is not null)
                            {
                                foreach (var lootItem in filteredLoot)
                                {
                                    if (lootItem.ShortName.StartsWith("Q_"))
                                        continue;
                                    lootItem.DrawESP(canvas, localPlayer, espUiConfig);
                                }
                            }

                            var allPlayers = data.Players.Where(x => !x.HasExfild);
                            playerNames = allPlayers.Where(x => x.Type == WebPlayerType.LocalPlayer || x.Type == WebPlayerType.Player || x.Type == WebPlayerType.Teammate).Select(x => x.Name);

                            //Players and items show at weird height (if on the ground or laying down, shows weird)
                            DrawLoot(canvas, localPlayer, filteredLoot);
                            foreach (var p in allPlayers)
                            {
                                p.DrawESP(canvas, localPlayer, espUiConfig);
                            }
                        } else
                        {
                            WaitingForRaidStatus(canvas, info);
                        }
                        break;
                    case Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connecting:
                        ConnectingStatus(canvas, info);
                        break;
                    case Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Reconnecting:
                        ReConnectingStatus(canvas, info);
                        break;
                }
            }
            catch { }

            canvas.Flush();
        }

        private static void DrawLoot(SKCanvas canvas, WebRadarPlayer localPlayer, IEnumerable<WebRadarLoot> loot)
        {
            if (loot is not null)
            {
                foreach (var item in loot)
                {
                    //Add UILootConfig
                    item.DrawESP(canvas, localPlayer, espUiConfig);
                }
            }
        }

        /// <summary>
        /// Retrieves the currently active map from SignalR data.
        /// </summary>
        public Map GetCurrentMap()
        {
            if (_sr.Data != null && _sr.Data.InGame && _sr.Data.MapID is string mapID)
            {
                if (_maps.TryGetValue(mapID, out var map))
                {
                    return map;
                }
            }
            return null; // No valid map found
        }
        public void UpdateMousePosition(SKPoint position)
        {
            _mousePosition = position;
        }

        private readonly Stopwatch _statusSw = Stopwatch.StartNew();
        private int _statusOrder = 1;

        private void IncrementStatus()
        {
            if (_statusSw.Elapsed.TotalSeconds >= 1d)
            {
                if (_statusOrder == 3)
                    _statusOrder = 1;
                else
                    _statusOrder++;
                _statusSw.Restart();
            }
        }

        private void NotConnectedStatus(SKCanvas canvas, SKImageInfo info, string page = "")
        {
            var notConnected = "Not Connected!" + page;
            float textWidth = SKPaints.TextRadarStatus.MeasureText(notConnected);
            canvas.DrawText(notConnected, (info.Width / 2) - textWidth / 2f, info.Height / 2,
                SKPaints.TextRadarStatus);
            IncrementStatus();
        }

        private void WaitingForRaidStatus(SKCanvas canvas, SKImageInfo info)
        {
            const string waitingFor1 = "Waiting for Raid Start.";
            const string waitingFor2 = "Waiting for Raid Start..";
            const string waitingFor3 = "Waiting for Raid Start...";
            string status = _statusOrder == 1 ?
                waitingFor1 : _statusOrder == 2 ?
                waitingFor2 : waitingFor3;
            float textWidth = SKPaints.TextRadarStatus.MeasureText(waitingFor1);
            canvas.DrawText(status, (info.Width / 2) - textWidth / 2f, info.Height / 2,
                SKPaints.TextRadarStatus);
            IncrementStatus();
        }

        private void ConnectingStatus(SKCanvas canvas, SKImageInfo info)
        {
            const string connecting1 = "Connecting.";
            const string connecting2 = "Connecting..";
            const string connecting3 = "Connecting...";
            string status = _statusOrder == 1 ?
                connecting1 : _statusOrder == 2 ?
                connecting2 : connecting3;
            float textWidth = SKPaints.TextRadarStatus.MeasureText(connecting1);
            canvas.DrawText(status, (info.Width / 2) - textWidth / 2f, info.Height / 2,
                SKPaints.TextRadarStatus);
            IncrementStatus();
        }

        private void ReConnectingStatus(SKCanvas canvas, SKImageInfo info)
        {
            const string reconnecting1 = "Re-Connecting.";
            const string reconnecting2 = "Re-Connecting..";
            const string reconnecting3 = "Re-Connecting...";
            string status = _statusOrder == 1 ?
                reconnecting1 : _statusOrder == 2 ?
                reconnecting2 : reconnecting3;
            float textWidth = SKPaints.TextRadarStatus.MeasureText(reconnecting1);
            canvas.DrawText(status, (info.Width / 2) - textWidth / 2f, info.Height / 2,
                SKPaints.TextRadarStatus);
            IncrementStatus();
        }

        /// <summary>
        /// Provides miscellaneous map parameters used throughout the entire render.
        /// </summary>
        private static MapParameters GetMapParameters(SKImageInfo skInfo, Map currentMap, Vector3 localPlayerMapPos)
        {
            float zoom = 2.01f - Zoom; // Invert zoom value
            var zoomWidth = currentMap.Image.Width * zoom;
            var zoomHeight = currentMap.Image.Height * zoom;

            var bounds = new SKRect(localPlayerMapPos.X - zoomWidth / 2,
                    localPlayerMapPos.Y - zoomHeight / 2,
                    localPlayerMapPos.X + zoomWidth / 2,
                    localPlayerMapPos.Y + zoomHeight / 2)
                .AspectFill(skInfo.Size);

            return new MapParameters
            {
                Map = currentMap,
                Bounds = bounds,
                XScale = skInfo.Width / bounds.Width, // Set scale for this frame
                YScale = skInfo.Height / bounds.Height // Set scale for this frame
            };
        }

        /// <summary>
        /// Update the scaling value(s) for all SKPaints.
        /// </summary>
        /// <param name="newScale">New scale to set.</param>
        private static void ScalePaints(float newScale)
        {
            SKPaints.TextOutline.TextSize = 12f * newScale;
            SKPaints.TextOutline.StrokeWidth = 2f * newScale;
            // Shape Outline is computed before usage due to different stroke widths

            SKPaints.PaintLocalPlayer.StrokeWidth = 3 * newScale;
            SKPaints.TextLocalPlayer.TextSize = 12 * newScale;
            SKPaints.PaintTeammate.StrokeWidth = 3 * newScale;
            SKPaints.TextTeammate.TextSize = 12 * newScale;
            SKPaints.PaintPlayer.StrokeWidth = 3 * newScale;
            SKPaints.TextPlayer.TextSize = 12 * newScale;
            SKPaints.PaintPlayerScav.StrokeWidth = 3 * newScale;
            SKPaints.TextPlayerScav.TextSize = 12 * newScale;
            SKPaints.PaintBot.StrokeWidth = 3 * newScale;
            SKPaints.TextBot.TextSize = 12 * newScale;
            SKPaints.PaintDeathMarker.StrokeWidth = 3 * newScale;
            SKPaints.TextRadarStatus.TextSize = 48 * newScale;
        }

        /// <summary>
        /// Load Maps from the embedded resources.
        /// </summary>
        /// <param name="maps">Maps field to populate.</param>
        private static void LoadMaps(ref FrozenDictionary<string, Map> maps)
        {
            var mapsBuilder = new Dictionary<string, Map>(StringComparer.OrdinalIgnoreCase);
            using var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("AncientMountain.Resources.Maps.bin");
            var resourceBytes = new byte[resourceStream!.Length];
            resourceStream.Read(resourceBytes);
            using var ms = new MemoryStream(resourceBytes, false);
            using var zipFiles = new ZipArchive(ms, ZipArchiveMode.Read);
            var zipConfigs = zipFiles.Entries.Where(file => file.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase));
            foreach (var zipConfig in zipConfigs)
            {
                using var configStream = zipConfig.Open();
                var config = JsonSerializer.Deserialize<MapConfig>(configStream);
                using var imgStream = zipFiles.GetEntry(zipConfig.Name.Replace(".json", ".jpg", StringComparison.OrdinalIgnoreCase))!.Open();
                using var imgData = SKData.Create(imgStream);
                var img = SKImage.FromEncodedData(imgData);
                var map = new Map
                {
                    ConfigFile = config,
                    Image = img
                };
                foreach (var id in config!.MapID)
                    mapsBuilder[id] = map;
            }
            maps = mapsBuilder.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        }

        #region Types

        /// <summary>
        /// Defines a Map for use in the GUI.
        /// </summary>
        public sealed class Map
        {
            /// <summary>
            /// Name of map (Ex: CUSTOMS)
            /// </summary>
            public string Name =>
                MapNames[ID[0]].ToUpper();
            /// <summary>
            /// Map Identifier.
            /// </summary>
            public List<string> ID =>
                ConfigFile.MapID;
            /// <summary>
            /// 'MapConfig' class instance
            /// </summary>
            public MapConfig ConfigFile { get; init; }
            /// <summary>
            /// SKImage of the Map.
            /// </summary>
            public SKImage Image { get; init; }
        }

        /// <summary>
        /// Contains multiple map parameters used by the GUI.
        /// </summary>
        public sealed class MapParameters
        {
            /// <summary>
            /// Currently loaded Map File.
            /// </summary>
            public Map Map { get; init; }
            /// <summary>
            /// Rectangular 'zoomed' bounds of the Bitmap to display.
            /// </summary>
            public SKRect Bounds { get; init; }
            /// <summary>
            /// Regular -> Zoomed 'X' Scale correction.
            /// </summary>
            public float XScale { get; init; }
            /// <summary>
            /// Regular -> Zoomed 'Y' Scale correction.
            /// </summary>
            public float YScale { get; init; }
        }

        /// <summary>
        /// Defines a .JSON Map Config File
        /// </summary>
        public sealed class MapConfig
        {
            [JsonIgnore]
            private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions()
            {
                WriteIndented = true,
            };
            /// <summary>
            /// Map ID(s) for this Map.
            /// </summary>
            [JsonInclude]
            [JsonPropertyName("mapID")]
            public List<string> MapID { get; private set; }
            /// <summary>
            /// Bitmap 'X' Coordinate of map 'Origin Location' (where Unity X is 0).
            /// </summary>
            [JsonPropertyName("x")]
            public float X { get; set; }
            /// <summary>
            /// Bitmap 'Y' Coordinate of map 'Origin Location' (where Unity Y is 0).
            /// </summary>
            [JsonPropertyName("y")]
            public float Y { get; set; }
            /// <summary>
            /// Arbitrary scale value to align map scale between the Bitmap and Game Coordinates.
            /// </summary>
            [JsonPropertyName("scale")]
            public float Scale { get; set; }
        }
        #endregion
    }
}
