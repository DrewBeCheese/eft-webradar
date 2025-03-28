using AncientMountain.Managed.Services;
using AncientMountain.Managed.Skia;
using MessagePack;
using SkiaSharp;
using System;
using System.Drawing;
using System.Numerics;

namespace AncientMountain.Managed.Data
{
    [MessagePackObject]
    public sealed class WebRadarPlayer : IEntity
    {
        /// <summary>
        /// Player Name.
        /// </summary>
        [Key(0)]
        public string Name { get; init; }
        /// <summary>
        /// Player Type (PMC, Scav,etc.)
        /// </summary>
        [Key(1)]
        public WebPlayerType Type { get; init; }
        /// <summary>
        /// True if player is active, otherwise False.
        /// </summary>
        [Key(2)]
        public bool IsActive { get; init; }
        /// <summary>
        /// True if player is alive, otherwise False.
        /// </summary>
        [Key(3)]
        public bool IsAlive { get; init; }
        /// <summary>
        /// Unity World Position.
        /// </summary>
        [Key(4)]
        public System.Numerics.Vector3 Position { get; init; }
        /// <summary>
        /// Unity World Rotation.
        /// </summary>
        [Key(5)]
        public System.Numerics.Vector2 Rotation { get; init; }
        [Key(6)] public int Value { get; init; }
        [Key(7)] public string PrimaryWeapon { get; init; }
        [Key(8)] public string SecondaryWeapon { get; init; }
        [Key(9)] public string Armor { get; init; }
        [Key(10)] public string Helmet { get; init; }
        [Key(11)] public string Backpack { get; init; }
        [Key(12)] public string Rig { get; init; }
        [Key(13)] public float KD { get; init; }
        [Key(14)] public float TotalHoursPlayed { get; init; }
        [Key(15)] public bool IsAiming { get; init; }
        [Key(16)] public float ZoomLevel { get; init; }
        [Key(17)] public IEnumerable<WebRadarLoot> Loot { get; init; }

        /// <summary>
        /// Player has exfil'd/left the raid.
        /// </summary>
        [IgnoreMember]
        public bool HasExfild => !IsActive && IsAlive;

        /// <summary>
        /// Player's Map Rotation (with 90 degree correction applied).
        /// </summary>
        [IgnoreMember]
        public float MapRotation
        {
            get
            {
                float mapRotation = Rotation.X; // Cache value
                mapRotation -= 90f;
                while (mapRotation < 0f)
                    mapRotation += 360f;

                return mapRotation;
            }
        }
        [IgnoreMember]
        public bool IsHovered { get; set; }

        public void Draw(SKCanvas canvas, SKImageInfo info, RadarService.MapParameters mapParams, WebRadarPlayer localPlayer, SKPoint mousePosition)
        {
            try
            {
                var point = Position.ToMapPos(mapParams.Map).ToZoomedPos(mapParams);

                // Check if the player is out of map bounds and not visible
                if (point.X < info.Rect.Left - 15 || point.X > info.Rect.Right + 15 ||
                    point.Y < info.Rect.Top - 15 || point.Y > info.Rect.Bottom + 15)
                    return;

                IsHovered = Vector2.Distance(new Vector2(mousePosition.X, mousePosition.Y), new Vector2(point.X, point.Y)) < 10f;

                // ✅ Get proper color based on Player Type / AI Name Check
                var (markerPaint, textPaint) = GetPaints(localPlayer);

                // 🎯 Draw dead players with a death marker
                if (!IsAlive)
                {
                    DrawDeathMarker(canvas, point);
                    return;
                }

                // 🟢 Draw the player marker
                DrawPlayerMarker(canvas, point, markerPaint, localPlayer);

                // 👤 Skip drawing name for local player
                if (this == localPlayer)
                    return;

                // 🔎 Calculate Height & Distance
                var height = Position.Y - localPlayer.Position.Y;
                var dist = Vector3.Distance(localPlayer.Position, Position);

                var lines = new string[]
                {
                    this.Name,
                    $"H: {(int)Math.Round(height)} D: {(int)Math.Round(dist)}"
                };

                DrawPlayerText(canvas, localPlayer, point, lines);

                if (IsHovered)
                    DrawGearInfo(canvas, mousePosition);
            }
            catch
            {
                // Debug.WriteLine($"WARNING! Player Draw Error: {ex}");
            }
        }

        private void DrawGearInfo(SKCanvas canvas, SKPoint position)
        {
            var lines = new List<string>
            {
                $"🎯 {this.Name}",
                $"🔫 Primary: {PrimaryWeapon}",
                $"🔫 Secondary: {SecondaryWeapon}",
                $"🛡️ Armor: {Armor}",
                $"⛑️ Helmet: {Helmet}",
                $"🎒 Backpack: {Backpack}",
                $"🦺 Rig: {Rig}",
                $"💰 Value: {Value}",
                $"⚔️ KD: {KD:F2}",
                $"⏳ Hours Played: {TotalHoursPlayed:F1}"
            };

            float padding = 8 * RadarService.Scale;
            float textHeight = (lines.Count + 1) * 16 * RadarService.Scale;
            float maxWidth = lines.Max(line => SKPaints.TextBasic.MeasureText(line)) + padding * 2;

            SKRect backgroundRect = new SKRect(
                position.X,
                position.Y,
                position.X + maxWidth,
                position.Y + textHeight
            );

            // ✅ Draw Background
            using (var backgroundPaint = new SKPaint
            {
                Color = SKColors.Black.WithAlpha(200),
                Style = SKPaintStyle.Fill
            })
            {
                canvas.DrawRoundRect(backgroundRect, 6 * RadarService.Scale, 6 * RadarService.Scale, backgroundPaint);
            }

            // ✅ Draw Border
            using (var borderPaint = new SKPaint
            {
                Color = SKColors.White,
                StrokeWidth = 2,
                Style = SKPaintStyle.Stroke
            })
            {
                canvas.DrawRoundRect(backgroundRect, 6 * RadarService.Scale, 6 * RadarService.Scale, borderPaint);
            }

            // ✅ Draw Text
            float textY = position.Y + padding;
            foreach (var line in lines)
            {
                textY += 16 * RadarService.Scale;
                canvas.DrawText(line, position.X + padding, textY, SKPaints.TextBasic);
            }
        }

        public void DrawESP(SKCanvas canvas, WebRadarPlayer localPlayer, ESPUiConfig espConfig)
        {
            var corner = new SKPoint(0, 0);
            corner.Offset(0, 12 * RadarService.Scale);
            canvas.DrawText($"{localPlayer.ZoomLevel} || {localPlayer.IsAiming}", corner, SKPaints.PaintDeathMarker);
            var distance = Vector3.Distance(localPlayer.Position, Position);
            if (distance > 500)
            {
                return;
            }

            if (this.HasExfild || !ScreenPositionCalculator.WorldToScreenPositionOnEnemyView(out var point, this, localPlayer, espConfig.ScreenWidth,
                espConfig.ScreenHeight, espConfig.FOV, localPlayer.ZoomLevel > 0f ? localPlayer.ZoomLevel : 1f))
            {
                return;
            }

            var paints = GetPaints(localPlayer);

            canvas.DrawCircle(point, RadarService.Scale, paints.Item1);
            canvas.DrawText($"{Name} - {distance.ToString("n2")}m", point, paints.Item2);
        }

        /// <summary>
        /// Draws Player Text on this location.
        /// </summary>
        private void DrawPlayerText(SKCanvas canvas, WebRadarPlayer localPlayer, SKPoint point, string[] lines)
        {
            var paints = GetPaints(localPlayer);
            var spacing = 3 * RadarService.Scale;
            point.Offset(9 * RadarService.Scale, spacing);
            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line?.Trim()))
                    continue;

                canvas.DrawText(line, point, SKPaints.TextOutline); // Draw outline
                canvas.DrawText(line, point, paints.Item2); // draw line text
                point.Offset(0, 12 * RadarService.Scale);
            }
            if (IsAiming)
            {
                canvas.DrawText("Aiming", point, SKPaints.TextOutline); // Draw outline
                canvas.DrawText("Aiming", point, paints.Item2); // draw line text
            }
        }

        /// <summary>
        /// Draws a Player Marker on this location.
        /// </summary>
        private void DrawPlayerMarker(SKCanvas canvas, SKPoint point, SKPaint markerPaint, WebRadarPlayer localPlayer)
        {
            var heightDiff = Position.Y - localPlayer.Position.Y;
            float size = 6 * RadarService.Scale;

            var circleOutline = SKPaints.ShapeOutline;
            circleOutline.StrokeWidth = 2f;

            canvas.DrawCircle(point, size, circleOutline); // Draw outline
            canvas.DrawCircle(point, size, markerPaint);

            var radians = MapRotation.ToRadians();
            int aimlineLength = this.Name == localPlayer.Name ? 40 : 15;
            var aimlineEnd = GetAimlineEndpoint(point, radians, aimlineLength);

            canvas.DrawLine(point, aimlineEnd, SKPaints.ShapeOutline);
            canvas.DrawLine(point, aimlineEnd, markerPaint);
        }

        /// <summary>
        /// Gets the point where the Aimline 'Line' ends. Applies UI Scaling internally.
        /// </summary>
        private static SKPoint GetAimlineEndpoint(SKPoint start, double radians, float aimlineLength)
        {
            aimlineLength *= RadarService.Scale;
            return new SKPoint((float)(start.X + Math.Cos(radians) * aimlineLength),
                (float)(start.Y + Math.Sin(radians) * aimlineLength));
        }

        /// <summary>
        /// Draws a Death Marker on this location.
        /// </summary>
        private static void DrawDeathMarker(SKCanvas canvas, SKPoint point)
        {
            var length = 6 * RadarService.Scale;
            canvas.DrawLine(new SKPoint(point.X - length, point.Y + length),
                new SKPoint(point.X + length, point.Y - length), SKPaints.PaintDeathMarker);
            canvas.DrawLine(new SKPoint(point.X - length, point.Y - length),
                new SKPoint(point.X + length, point.Y + length), SKPaints.PaintDeathMarker);
        }

        private ValueTuple<SKPaint, SKPaint> GetPaints(WebRadarPlayer localPlayer)
        {
            if (this == localPlayer)
                return new ValueTuple<SKPaint, SKPaint>(SKPaints.PaintLocalPlayer, SKPaints.TextLocalPlayer);

            // If the player is an AI (Bot), determine their type based on name
            if (this.Type == WebPlayerType.Bot)
            {
                var playerType = PlayerColorManager.GetPlayerType(this.Name);

                return playerType switch
                {
                    WebPlayerType.Boss => (SKPaints.PaintPlayerBoss, SKPaints.TextPlayer),   // Boss Color
                    WebPlayerType.Guard or WebPlayerType.Follower => (SKPaints.PaintFollower, SKPaints.TextTeammate), // Guard/Follower Color
                    WebPlayerType.Rogue => (SKPaints.PaintPlayerBoss, SKPaints.TextPlayer), // Rogue Color (same as Boss)
                    _ => (SKPaints.PaintBot, SKPaints.TextBot) // Default AI color
                };
            }

            // Normal player types (PMC, Scavs, etc.)
            return this.Type switch
            {
                WebPlayerType.LocalPlayer => (SKPaints.PaintTeammate, SKPaints.TextTeammate),
                WebPlayerType.Teammate => (SKPaints.PaintTeammate, SKPaints.TextTeammate),
                WebPlayerType.Player => (SKPaints.PaintPlayer, SKPaints.TextPlayer),
                WebPlayerType.PlayerScav => (SKPaints.PaintPlayerScav, SKPaints.TextPlayerScav),
                _ => (SKPaints.PaintBot, SKPaints.TextBot) // Default for unknown types
            };
        }
    }
}
