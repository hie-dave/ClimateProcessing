using System.Collections;

namespace ClimateProcessing.Tests;

/// <summary>
/// An enumerable that disposes of its elements when disposed.
/// </summary>
/// <typeparam name="T">The type of the elements.</typeparam>
public sealed class DisposableEnumerable<T> : IEnumerable<T>, IDisposable
    where T : IDisposable
{
    /// <summary>
    /// The enumerable to dispose of.
    /// </summary>
    private readonly IEnumerable<T> enumerable;

    /// <summary>
    /// Create a disposable enumerable.
    /// </summary>
    /// <param name="enumerable">The enumerable to dispose of.</param>
    public DisposableEnumerable(IEnumerable<T> enumerable)
    {
        // Ensure the collection is enumerated once
        this.enumerable = enumerable.ToList();
    }

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator() => enumerable.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (T item in enumerable)
            item.Dispose();
    }
}
