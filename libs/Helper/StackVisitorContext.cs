namespace Parseidon.Helper;

public class StackVisitorContext<StackClass> where StackClass : class
{
    public StackVisitorContext(Func<String, Int32, Exception> getException)
    {
        GetException = getException;
    }

    private ScopedStack<StackClass> Stack { get; } = new ScopedStack<StackClass>();
    public Func<String, Int32, Exception> GetException { get; }

    public T Pop<T>(Int32 position) where T : class, StackClass
    {
        try
        {
            return Stack.Pop<T>();
        }
        catch (Exception e)
        {
            throw GetException(e.Message, position);
        }
    }

    public T? TryPop<T>(Int32 position) where T : class, StackClass
    {
        return Stack.TryPop<T>();
    }

    public List<T> PopList<T>() where T : class, StackClass
    {
        return Stack.PopList<T>();
    }

    public void Push(StackClass element)
    {
        Stack.Push(element);
    }

    public void EnterScope()
    {
        Stack.EnterScope();
    }

    public void ExitScope()
    {
        Stack.ExitScope();
    }
}