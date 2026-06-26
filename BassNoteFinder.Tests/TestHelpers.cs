using System.Reflection;
using BassNoteFinder.MusicTheory;
using BassNoteFinder.Rendering;
using Xunit;

namespace BassNoteFinder.Tests;

internal static class TestHelpers
{
    internal static T RunOnSta<T>(Func<T> action)
    {
        T? result = default;
        Exception? ex = null;

        var thread = new Thread(() =>
        {
            try
            {
                result = action();
            }
            catch (Exception e)
            {
                ex = e;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (ex != null)
        {
            throw new TargetInvocationException(ex);
        }

        return result!;
    }

    internal static void RunOnSta(Action action) =>
        RunOnSta<int>(() =>
        {
            action();
            return 0;
        });

    internal static object? InvokePrivate(object target, string methodName, params object?[] args)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return method!.Invoke(target, args.Length > 0 ? args : null);
    }

    internal static void RaiseEvent<TOwner>(TOwner owner, string eventName) where TOwner : notnull
    {
        Type? type = typeof(TOwner);
        FieldInfo? field = null;

        while (field == null && type != null)
        {
            field = type.GetField(eventName, BindingFlags.Instance | BindingFlags.NonPublic);
            type = type.BaseType;
        }

        Assert.NotNull(field);
        var handler = field!.GetValue(owner) as Action;
        handler?.Invoke();
    }

    internal static T? GetPrivateField<T>(object target, string fieldName)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        return (T?)field?.GetValue(target);
    }

    internal static void InvokeSelectNote(object view, Note note, StaffRenderer.AccidentalMode mode)
    {
        var method = view.GetType().GetMethod("SelectNote", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(view, new object[] { note, mode });
    }
}
