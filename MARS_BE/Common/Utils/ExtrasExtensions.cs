namespace MARS_BE.Common.Utils;

public static class ExtrasExtensions
{
    /// <summary>
    /// Merge dictionary: update/add keys; remove when value is null (PATCH semantics).
    /// If <paramref name="patch"/> is null, no-op. Throws if target is null.
    /// </summary>
    public static void MergeFrom(
        this IDictionary<string, object?> target,
        IReadOnlyDictionary<string, object?>? patch)
    {
        if (target is null)
            throw new ArgumentNullException(nameof(target));

        if (patch is null)
            return; // no-op

        foreach (var kv in patch)
        {
            if (kv.Value is null)
                target.Remove(kv.Key);
            else
                target[kv.Key] = kv.Value;
        }
    }
}