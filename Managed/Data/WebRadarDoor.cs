using AncientMountain.Managed.Services;
using AncientMountain.Managed.Skia;
using MessagePack;
using SkiaSharp;
using System.Numerics;

namespace AncientMountain.Managed.Data
{
    [MessagePackObject]
    public readonly struct WebRadarDoor
    {
        [Key(0)]
        public readonly EDoorState DoorState { get; init; }
        [Key(1)]
        public readonly string Id { get; init; }
        [Key(2)]
        public readonly string? KeyId { get; init; }
        [Key(3)]
        public readonly Vector3 Position { get; init; }

        public void DrawDoor(SKCanvas canvas, RadarService.MapParameters mapParams, SKPoint mousePosition, WebRadarPlayer localPlayer)
        {
            var point = Position.ToMapPos(mapParams.Map).ToZoomedPos(mapParams);
            var size = 6 * RadarService.Scale;
            var isHovered = Vector2.Distance(new Vector2(mousePosition.X, mousePosition.Y), new Vector2(point.X, point.Y)) < 10f;
            var heightDiff = Position.Y - localPlayer.Position.Y;
            var allItems = Utils.GetAllItemDict();

            var indicator = canvas.CanvasDrawIndicator(SKPaints.InfoOutline, heightDiff, point, size);
            if (indicator == 0)
            {
                canvas.DrawText("i", point.X, point.Y, SKPaints.TextBasic);
            }

            if (isHovered)
            {
                var lines = new List<string>
                {
                    $"Door Id: {this.Id}",
                    $"Key Required: {(KeyId != null ? allItems[KeyId]?.Name : "None")}",
                    $"Door State: {DoorState}",
                };

                CanvasHelper.DrawInfoCard(canvas, mousePosition, lines);
            }
        }
    }

    public enum EDoorState
    {
        // Token: 0x0400E90E RID: 59662
        None = 0,
        // Token: 0x0400E90F RID: 59663
        Locked = 1,
        // Token: 0x0400E910 RID: 59664
        Shut = 2,
        // Token: 0x0400E911 RID: 59665
        Open = 4,
        // Token: 0x0400E912 RID: 59666
        Interacting = 8,
        // Token: 0x0400E913 RID: 59667
        Breaching = 16
    }
}
