namespace Parseidon.Helper;

public class StackVisitorContext<StackClass> : BaseVisitorContext where StackClass : class
{
    public StackVisitorContext(String text) : base(text) { }

    private ScopedStack<StackClass> Stack { get; } = new ScopedStack<StackClass>();

    public T Pop<T>() where T : class, StackClass
    {
        return Stack.Pop<T>();
    }

    public T? TryPop<T>() where T : class, StackClass
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