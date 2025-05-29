namespace Parseidon.Helper;

public class ScopedStack<T>
{
    private class StackEntry
    {
        public T Item { get; }
        public int Depth { get; }

        public StackEntry(T item, int depth)
        {
            Item = item;
            Depth = depth;
        }
    }

    private readonly List<StackEntry> _entries = new();
    private readonly Stack<int> _scopes = new();
    private int _currentDepth = 0;

    public void EnterScope()
    {
        _currentDepth++;
        _scopes.Push(_currentDepth);
    }

    public void ExitScope()
    {
        if (_scopes.Count == 0)
            throw new InvalidOperationException("No active scope to exit.");

        _scopes.Pop();
        _currentDepth = _scopes.Count > 0 ? _scopes.Peek() : 0;
    }

    public void Push(T item)
    {
        _entries.Add(new StackEntry(item, _currentDepth));
    }

    public T Pop()
    {
        for (int i = _entries.Count - 1; i >= 0; i--)
        {
            var entry = _entries[i];
            if (entry.Depth > _currentDepth)
            {
                _entries.RemoveAt(i);
                return entry.Item;
            }
        }

        throw new InvalidOperationException("No item in current or child scopes.");
    }

    public T Peek()
    {
        for (int i = _entries.Count - 1; i >= 0; i--)
        {
            var entry = _entries[i];
            if (entry.Depth > _currentDepth)
            {
                return entry.Item;
            }
        }

        throw new InvalidOperationException("No item in current or child scopes.");
    }

    public T? TryPeek()
    {
        for (int i = _entries.Count - 1; i >= 0; i--)
        {
            var entry = _entries[i];
            if (entry.Depth > _currentDepth)
            {
                return entry.Item;
            }
        }

        return default;
    }

    public IEnumerable<T> GetItemsInCurrentAndChildScopes()
    {
        return _entries
            .Where(e => e.Depth >= _currentDepth)
            .Select(e => e.Item)
            .Reverse();
    }
}