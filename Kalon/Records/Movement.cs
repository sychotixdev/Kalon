using Kalon.Native.Structs;

namespace Kalon.Records;

public sealed record Movement(TimeSpan Delay, IEnumerable<Point> Points);