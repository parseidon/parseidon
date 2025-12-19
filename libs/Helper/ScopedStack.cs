namespace Parseidon.Helper;

public sealed class ScopedStack<T> where T : class
{
    private sealed class StackEntry
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
            throw new InvalidOperationException("No active scope to exit!");

        _scopes.Pop();
        _currentDepth = _scopes.Count > 0 ? _scopes.Peek() : 0;
    }

    public void Push(T item)
    {
        _entries.Add(new StackEntry(item, _currentDepth));
    }

    public T Pop()
    {
        return TryPop() ?? throw new InvalidOperationException("No item in current or child scopes!");
    }

    public T? TryPop()
    {
        var entry = _entries.Last();
        if (entry.Depth > _currentDepth)
        {
            _entries.Remove(entry);
            return entry.Item;
        }
        return null;
    }

    public NT Pop<NT>() where NT : class, T
    {
        T result = Pop();
        if (result is not NT)
            throw new InvalidOperationException($"Expected {typeof(NT).Name}, got {((Type)result.GetType()).Name}!");
        return (result as NT)!;
    }

    public NT? TryPop<NT>() where NT : class, T
    {
        if (TryPeek() is NT)
            return Pop() as NT;
        else
            return null;
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

        throw new InvalidOperationException("No item in current or child scopes!");
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

    public List<NT> PopList<NT>() where NT : class, T
    {
        List<NT> resultList = new List<NT>();
        while (TryPeek() is NT)
            resultList.Add(Pop<NT>());
        return resultList;
    }
}
