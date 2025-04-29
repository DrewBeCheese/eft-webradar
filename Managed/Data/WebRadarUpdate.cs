using AncientMountain.Managed.Services;
using AncientMountain.Managed.Skia;
using AncientMountain.Pages;
using MessagePack;
using SkiaSharp;
using System.Drawing;
using System.Numerics;

namespace AncientMountain.Managed.Data
{
    [MessagePackObject]
    public sealed class WebRadarUpdate
    {
        /// <summary>
        /// Update version (used for ordering).
        /// </summary>
        [Key(0)]
        public uint Version { get; set; } = 0;
        /// <summary>
        /// True if In-Game, otherwise False.
        /// </summary>
        [Key(1)]
        public bool InGame { get; set; } = false;
        /// <summary>
        /// Contains the Map ID of the current map.
        /// </summary>
        [Key(2)]
        public string MapID { get; set; } = null;
        /// <summary>
        /// All Players currently on the map.
        /// </summary>
        [Key(3)]
        public IEnumerable<WebRadarPlayer> Players { get; set; } = null;

        [Key(4)]
        public IEnumerable<WebRadarLoot> Loot { get; set; }

        [Key(5)]
        public DateTime SendTime { get; set; }

        [Key(6)]
        public IEnumerable<WebRadarDoor> Doors { get; set; } = null;
    }
}
