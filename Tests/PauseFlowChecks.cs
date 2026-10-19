using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

// Exercises the actual action enumerator without starting Unity or calling engine APIs.
internal static class PauseFlowChecks
{
    private static int checks;

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        checks++;
    }

    private static IEnumerator Child(List<string> events)
    {
        try
        {
            events.Add("child-start");
            yield return null;
            events.Add("child-finish");
        }
        finally { events.Add("child-disposed"); }
    }

    private static IEnumerator Scenario(List<string> events, object waitToken)
    {
        events.Add("start");
        yield return Child(events);
        events.Add("after-child");
        yield return waitToken;
        events.Add("complete");
    }

    // Models nested IEnumerator scheduling; opaque Unity yield instructions are returned intact.
    private static object Advance(Stack<IEnumerator> stack)
    {
        while (stack.Count > 0)
        {
            IEnumerator current = stack.Peek();
            if (!current.MoveNext())
            {
                (stack.Pop() as IDisposable)?.Dispose();
                continue;
            }
            if (current.Current is IEnumerator nested) stack.Push(nested);
            else return current.Current;
        }
        return null;
    }

    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.AssemblyResolve += (_, request) =>
        {
            string name = new AssemblyName(request.Name).Name + ".dll";
            foreach (string directory in new[] { args[1], args[2] })
            {
                string path = Path.Combine(directory, name);
                if (File.Exists(path)) return Assembly.LoadFrom(path);
            }
            return null;
        };
        Type actionSystem = Assembly.LoadFrom(args[0]).GetType("ActionSystem", true);
        MethodInfo wrapper = actionSystem.GetMethod("RunPausable", BindingFlags.NonPublic | BindingFlags.Static);
        bool paused = true;
        Func<bool> isPaused = () => paused;
        var events = new List<string>();
        object waitToken = new object();
        IEnumerator routine = (IEnumerator)wrapper.Invoke(null, new object[] { Scenario(events, waitToken), isPaused });
        var stack = new Stack<IEnumerator>();
        stack.Push(routine);

        Advance(stack);
        Advance(stack);
        Check(events.Count == 0, "A paused action must not start or consume reactions.");
        paused = false;
        Advance(stack);
        Check(string.Join(",", events) == "start,child-start", "Resume enters the nested performer once.");
        paused = true;
        Advance(stack);
        Advance(stack);
        Check(events.Count == 2, "A nested yield-null performer must remain frozen.");
        paused = false;
        Check(ReferenceEquals(Advance(stack), waitToken), "Preserve the original yield instruction.");
        Check(string.Join(",", events) == "start,child-start,child-finish,child-disposed,after-child",
            "Nested performer resumes in order and disposes once.");
        paused = true;
        Advance(stack);
        Check(!events.Contains("complete"), "Pause also prevents completion callbacks.");
        paused = false;
        Advance(stack);
        Check(stack.Count == 0 && events.FindAll(item => item == "complete").Count == 1,
            "Completion occurs once after resume.");

        events.Clear();
        routine = (IEnumerator)wrapper.Invoke(null, new object[] { Scenario(events, waitToken), isPaused });
        stack.Push(routine);
        Advance(stack);
        paused = true;
        Advance(stack);
        while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose();
        Check(events.Contains("child-disposed") && !events.Contains("child-finish") && !events.Contains("complete"),
            "Cancelling a paused enumerator disposes it without completing combat work.");
        Console.WriteLine($"PASS: {checks} pause/resume enumerator checks; no Unity Editor execution.");
    }
}
