using AncientMountain.Managed.Services;
using AncientMountain.Managed.Skia;
using MessagePack;
using SkiaSharp;
using System.Numerics;

namespace AncientMountain.Managed.Data
{
    [MessagePackObject]
    public readonly struct WebRadarLoot : IEntity
    {
        [IgnoreMember]
        public readonly string Id => $"{ShortName}-{Position.ToString()}";
        /// <summary>
        /// Item's Short Name.
        /// </summary>
        [Key(0)]
        public readonly string ShortName { get; init; }

        /// <summary>
        /// Item's Price (Roubles).
        /// </summary>
        [Key(1)]
        public readonly int Price { get; init; }

        /// <summary>
        /// Item's Position.
        /// </summary>
        [Key(2)]
        public readonly Vector3 Position { get; init; }

        /// <summary>
        /// Item is Meds.
        /// </summary>
        [Key(3)]
        public bool IsMeds { get; init; }

        /// <summary>
        /// Item is Food.
        /// </summary>
        [Key(4)]
        public bool IsFood { get; init; }

        /// <summary>
        /// Item is Backpack.
        /// </summary>
        [Key(5)]
        public bool IsBackpack { get; init; }

        [Key(6)]
        public string BsgId { get; init; }

        public void Draw(SKCanvas canvas, SKImageInfo info, RadarService.MapParameters mapParams,
            WebRadarPlayer localPlayer, LootFilterService lootFilter, SKImage image, SKPoint mousePos)
        {
            try
            {
                var point = Position.ToMapPos(mapParams.Map).ToZoomedPos(mapParams);

                // Check if the loot is out of map bounds and not visible
                if (point.X < info.Rect.Left - 15 || point.X > info.Rect.Right + 15 ||
                    point.Y < info.Rect.Top - 15 || point.Y > info.Rect.Bottom + 15)
                    return;

                var isHovered = Vector2.Distance(new Vector2(mousePos.X, mousePos.Y), new Vector2(point.X, point.Y)) < 10f;

                // 🚨 Ignore loot if filters are disabled
                if ((!lootFilter.ShowFood && IsFood) ||
                    (!lootFilter.ShowMeds && IsMeds) ||
                    (!lootFilter.ShowBackpacks && IsBackpack))
                {
                    return; // Do not render this loot
                }

                var AdjustedPrice = Price / 1000;

                // ✅ Fix backpack naming issue
                var Name = ShortName.StartsWith("NULL") ? "Backpack" : ShortName;

                var label = $"{Name} [{AdjustedPrice}K]";

                DrawLootMarker(canvas, point, localPlayer, Price, lootFilter);
                DrawLootText(canvas, point, label, lootFilter);

                if (lootFilter.SelectedItemId == this.Id)
                {
                    canvas.DrawLineToPOI(localPlayer, mapParams, point);
                }

                if (isHovered && image != null)
                    canvas.DrawImage(image, mousePos);
            }
            catch
            {
                // Handle error
            }
        }

        private void DrawLootMarker(SKCanvas canvas, SKPoint point, WebRadarPlayer localPlayer, int price, LootFilterService lootFilter)
        {
            var heightDiff = Position.Y - localPlayer.Position.Y;
            var size = 4 * RadarService.Scale;

            if (!lootFilter.ShowBackpacks && IsBackpack) return;

            if (lootFilter.ShowFood && IsFood || lootFilter.ShowMeds && IsMeds || lootFilter.ShowBackpacks && IsBackpack)
            {
                canvas.CanvasDrawIndicator(SKPaints.PaintLootFMB, heightDiff, point, size);
                return;
            }

            if (lootFilter.IsImportantLoot(this))
            {
                canvas.CanvasDrawIndicator(SKPaints.PaintLootImportant, heightDiff, point, size);
            } else
            {
                canvas.CanvasDrawIndicator(SKPaints.PaintLoot, heightDiff, point, size);
            }
        }

        private void DrawLootText(SKCanvas canvas, SKPoint point, string label, LootFilterService lootFilter)
        {
            point.Offset(9 * RadarService.Scale, 3 * RadarService.Scale);
            canvas.DrawText(label, point, SKPaints.TextOutline);
            canvas.DrawText(label, point, lootFilter.IsImportantLoot(this) ? SKPaints.TextImportantLoot : SKPaints.TextLoot);
        }

        public void DrawESP(SKCanvas canvas, WebRadarPlayer localPlayer, ESPUiConfig espConfig)
        {
            var distance = Vector3.Distance(localPlayer.Position, Position);
            if (distance > 200)
            {
                return;
            }
            if (!ScreenPositionCalculator.WorldToScreenPositionOnEnemyView(out var point, this, localPlayer, espConfig.ScreenWidth, espConfig.ScreenHeight,
                espConfig.FOV, localPlayer.ZoomLevel > 0f ? localPlayer.ZoomLevel : 1f))
            {
                return;
            }

            var paints = GetPaints();

            canvas.DrawCircle(point, RadarService.Scale, paints.Item1);
            //Make this more like local radar
            canvas.DrawText($"{ShortName} - {Price} - {distance.ToString("n2")}m", point, paints.Item2);
        }

        //Fix this after mambo merge
        private ValueTuple<SKPaint, SKPaint> GetPaints()
        {
            if (Price >  200000)
            {
                return new(SKPaints.PaintLootImportant, SKPaints.TextImportantLoot);
            }
            //Add more to this
            return new(SKPaints.PaintLoot, SKPaints.TextLoot);
        }
    }
}
