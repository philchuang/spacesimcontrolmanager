namespace SSCM.Core;

/// <summary>
/// <see cref="Path.Combine(string[])"/> appends each argument as written. A relative path that contains
/// <c>\</c> is one segment on Linux, so <c>data\Localization</c> is not <c>data/Localization</c>.
/// </summary>
public static class PathSegments
{
    public static string Combine(params string[] parts)
    {
        var segments = new List<string>();
        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(part)) continue;
            if (Path.IsPathRooted(part))
            {
                segments.Clear();
                segments.Add(part);
                continue;
            }

            foreach (var segment in part.Split('\\', '/'))
            {
                if (segment.Length > 0) segments.Add(segment);
            }
        }

        return segments.Count == 0 ? string.Empty : Path.Combine(segments.ToArray());
    }
}
