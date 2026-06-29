using System.Collections.Generic;

public class UIStack
{
    private Stack<UIView> stack = new Stack<UIView>();

    public void Push(UIView view)
    {
        stack.Push(view);
    }

    public UIView Pop()
    {
        if (stack.Count == 0)
            return null;

        return stack.Pop();
    }

    public UIView Peek()
    {
        if (stack.Count == 0)
            return null;

        return stack.Peek();
    }
}