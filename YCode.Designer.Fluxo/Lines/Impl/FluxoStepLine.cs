namespace YCode.Designer.Fluxo;

public class FluxoStepLine(FluxoDesigner designer) : FluxoBaseLine(LineType.Step, designer)
{
    private readonly double _spacing = 0d;

    public override void DrawLine(FluxoLineParameter @params, StreamGeometryContext context)
    {
        base.DrawLine(@params, context);

        context.BeginFigure(this.Parameter.Start, false, false);
        context.LineTo(this.Points[0], true, true);
        context.LineTo(this.Points[1], true, true);
        context.LineTo(this.Points[2], true, true);
        context.LineTo(this.Points[3], true, true);
        context.LineTo(this.Parameter.End, true, true);
    }

    protected override void OnHorizontal()
    {
        var points = GetLinePoints(this.Parameter.Source.Right, this.Parameter.Target.Left);

        this.Points.AddRanage([points.P0, points.P1, points.P2, points.P3]);
    }

    protected override void OnVertical()
    {
        var points = GetLinePoints(this.Parameter.Source.Bottom, this.Parameter.Target.Top);

        this.Points.AddRanage([points.P0, points.P1, points.P2, points.P3]);
    }

    protected override void OnCross()
    {
        throw new NotImplementedException();
    }

    private (Point P0, Point P1, Point P2, Point P3) GetLinePoints(Point source, Point target)
    {
        var sourceDir = GetConnectorDirection(this.Parameter.SourcePosition);
        
        var targetDir = GetConnectorDirection(this.Parameter.TargetPosition);

        this.Parameter.Start = source + new Vector(_spacing * sourceDir.X, _spacing * sourceDir.Y);

        this.Parameter.End = target + new Vector(_spacing * targetDir.X, _spacing * targetDir.Y);

        var connectionDir =
            GetConnectionDirection(this.Parameter.Start, this.Parameter.SourcePosition, this.Parameter.End);

        var horizontalConnection = connectionDir.X != 0;

        if (IsOppositePosition(this.Parameter.SourcePosition, this.Parameter.TargetPosition))
        {
            var (p1, p2) = GetOppositePositionPoints();

            return (this.Parameter.Start, p1, p2, this.Parameter.End);
        }

        if (this.Parameter.SourcePosition == this.Parameter.TargetPosition)
        {
            var p = GetSamePositionPoint();
            
            return (this.Parameter.Start, p, p, this.Parameter.End);
        }

        var isSameDir = horizontalConnection ? sourceDir.X == targetDir.Y : sourceDir.Y == targetDir.X;
        
        var startGreaterThanEnd = horizontalConnection
            ? this.Parameter.Start.Y > this.Parameter.End.Y
            : this.Parameter.Start.X > this.Parameter.End.X;

        var positiveDir = horizontalConnection ? sourceDir.X == 1 : sourceDir.Y == 1;
        
        var shouldFlip = positiveDir
            ? isSameDir ? !startGreaterThanEnd : startGreaterThanEnd
            : isSameDir
                ? startGreaterThanEnd
                : !startGreaterThanEnd;

        if (shouldFlip)
        {
            var sourceTarget = new Point(this.Parameter.Start.X, this.Parameter.End.Y);
            
            var targetSource = new Point(this.Parameter.End.X, this.Parameter.Start.Y);

            var pf = horizontalConnection ? sourceTarget : targetSource;
            
            return (this.Parameter.Start, pf, pf, this.Parameter.End);
        }

        var pp = GetSamePositionPoint();
        
        return (this.Parameter.Start, pp, pp, this.Parameter.End);

        (Point P1, Point P2) GetOppositePositionPoints()
        {
            var center = this.Parameter.Start + (this.Parameter.End - this.Parameter.Start) / 2;

            (Point P1, Point P2) verticalSplit = (new Point(center.X, this.Parameter.Start.Y),
                new Point(center.X, this.Parameter.End.Y));
            (Point P1, Point P2) horizontalSplit = (new Point(this.Parameter.Start.X, center.Y),
                new Point(this.Parameter.End.X, center.Y));

            if (horizontalConnection)
            {
                // left to right / right to left
                return sourceDir.X == connectionDir.X ? verticalSplit : horizontalSplit;
            }

            // top to bottom / bottom to top
            return sourceDir.Y == connectionDir.Y ? horizontalSplit : verticalSplit;
        }

        Point GetSamePositionPoint()
        {
            var sourceTarget = new Point(this.Parameter.Start.X, this.Parameter.End.Y);
            
            var targetSource = new Point(this.Parameter.End.X, this.Parameter.Start.Y);

            if (horizontalConnection)
            {
                return sourceDir.X == connectionDir.X ? targetSource : sourceTarget;
            }

            return sourceDir.Y == connectionDir.Y ? sourceTarget : targetSource;
        }

        static Point GetConnectionDirection(in Point source, FluxoLinePosition sourcePosition, in Point target)
        {
            return sourcePosition == FluxoLinePosition.Left || sourcePosition == FluxoLinePosition.Right
                ? new Point(Math.Sign(target.X - source.X), 0)
                : new Point(0, Math.Sign(target.Y - source.Y));
        }

        static Point GetConnectorDirection(FluxoLinePosition position)
            => position switch
            {
                FluxoLinePosition.Top => new Point(0, -1),
                FluxoLinePosition.Left => new Point(-1, 0),
                FluxoLinePosition.Bottom => new Point(0, 1),
                FluxoLinePosition.Right => new Point(1, 0),
                _ => default
            };

        static bool IsOppositePosition(FluxoLinePosition sourcePosition, FluxoLinePosition targetPosition)
        {
            return sourcePosition == FluxoLinePosition.Left && targetPosition == FluxoLinePosition.Right
                   || sourcePosition == FluxoLinePosition.Right && targetPosition == FluxoLinePosition.Left
                   || sourcePosition == FluxoLinePosition.Top && targetPosition == FluxoLinePosition.Bottom
                   || sourcePosition == FluxoLinePosition.Bottom && targetPosition == FluxoLinePosition.Top;
        }
    }

    public override Point GetPoint(double target)
    {
        var current = 0d;
        var start = this.Points[0];

        var length = this.GetLength();

        var targetLength = length * target;

        var prePoint = new Point(start.X, start.Y);

        for (var i = 1; i < this.Points.Count; i++)
        {
            var pointToLength = Math.Sqrt(Math.Pow(prePoint.X - this.Points[i].X, 2) +
                                          Math.Pow(prePoint.Y - this.Points[i].Y, 2));

            var distance = Point.Subtract(this.Points[i], prePoint).Length;

            if (current + distance >= targetLength)
            {
                var extra = targetLength - current;

                var factor = extra / distance;

                return new Point(
                    prePoint.X + (factor * (this.Points[i].X - prePoint.X)),
                    prePoint.Y + (factor * (this.Points[i].Y - prePoint.Y)));
            }

            prePoint = this.Points[i];

            current += pointToLength;
        }

        return prePoint;
    }

    public override double GetLength()
    {
        var start = this.Points[0];

        var length = 0d;

        var prePoint = new Point(start.X, start.Y);

        for (var i = 1; i < this.Points.Count; i++)
        {
            length += Math.Sqrt(Math.Pow(prePoint.X - this.Points[i].X, 2) +
                                Math.Pow(prePoint.Y - this.Points[i].Y, 2));

            prePoint = this.Points[i];
        }

        return length;
    }
}