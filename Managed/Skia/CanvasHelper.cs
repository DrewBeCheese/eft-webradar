using AncientMountain.Managed.Data;
using AncientMountain.Managed.Services;
using SkiaSharp;
using System.Numerics;

namespace AncientMountain.Managed.Skia
{
    public static class CanvasHelper
    {
        public static int CanvasDrawIndicator(this SKCanvas canvas, SKPaint paint, float heightDiff, SKPoint point, float size)
        {
            var indicator = 0;

            if (heightDiff > 1.45) // loot is above player
            {
                using var path = point.GetUpArrow(5);
                canvas.DrawPath(path, SKPaints.ShapeOutline);
                canvas.DrawPath(path, paint);
                indicator -= 1;
            } else if (heightDiff < -1.45) // loot is below player
            {
                using var path = point.GetDownArrow(5);
                canvas.DrawPath(path, SKPaints.ShapeOutline);
                canvas.DrawPath(path, paint);
                indicator += 1;
            } else // loot is level with player
            {
                canvas.DrawCircle(point, size, SKPaints.ShapeOutline);
                canvas.DrawCircle(point, size, paint);
            }

            return indicator;
        }

        public static void DrawLineToPOI(this SKCanvas canvas, IEntity startingEntity, RadarService.MapParameters mapParams, SKPoint interestPoint)
        {
            var startPoint = startingEntity.Position.ToMapPos(mapParams.Map).ToZoomedPos(mapParams);
            canvas.DrawLine(startPoint, interestPoint, SKPaints.POILine);
        }

        public static void DrawInfoCard(SKCanvas canvas, SKPoint cardPosition, List<string> lines)
        {
            float padding = 8 * RadarService.Scale;
            float textHeight = (lines.Count + 1) * 16 * RadarService.Scale;
            float maxWidth = lines.Max(line => SKPaints.TextBasic.MeasureText(line)) + padding * 2;

            SKRect backgroundRect = new SKRect(
                cardPosition.X,
                cardPosition.Y,
                cardPosition.X + maxWidth,
                cardPosition.Y + textHeight
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

            var textY = cardPosition.Y + padding;
            foreach (var line in lines)
            {
                textY += 16 * RadarService.Scale;
                canvas.DrawText(line, cardPosition.X + padding, textY, SKPaints.TextBasic);
            }
        }
    }
}
