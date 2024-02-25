namespace TLTool;

/// <summary>A comparer that compares the key in a <see cref="KeyValuePair{TKey, TValue}"/>.</summary>
public sealed class KeyComparer<TKey, TValue> : IComparer<KeyValuePair<TKey, TValue>>
{
    /// <inheritdoc/>
    public int Compare(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y)
    {
        return Comparer<TKey>.Default.Compare(x.Key, y.Key);
    }
}
