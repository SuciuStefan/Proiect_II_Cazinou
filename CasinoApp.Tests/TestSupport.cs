using System.Reflection;
using CasinoApp.DataAccess;
using Microsoft.Data.Sqlite;

namespace CasinoApp.Tests;

internal static class ReflectionTestSupport
{
    private const BindingFlags MemberFlags =
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    public static T GetField<T>(object target, string name)
    {
        var field = target.GetType().GetField(name, MemberFlags)
            ?? throw new InvalidOperationException($"Missing field '{name}' on {target.GetType().Name}.");

        return (T)field.GetValue(target)!;
    }

    public static void SetField(object target, string name, object? value)
    {
        var field = target.GetType().GetField(name, MemberFlags)
            ?? throw new InvalidOperationException($"Missing field '{name}' on {target.GetType().Name}.");

        field.SetValue(target, value);
    }

    public static void SetMember(object target, string name, object value)
    {
        var property = target.GetType().GetProperty(name, MemberFlags);
        if (property != null)
        {
            property.SetValue(target, value);
            return;
        }

        SetField(target, name, value);
    }

    public static T Invoke<T>(object target, string name, params object?[] args) =>
        (T)GetMethod(target.GetType(), name).Invoke(target, args)!;

    public static void Invoke(object target, string name, params object?[] args) =>
        GetMethod(target.GetType(), name).Invoke(target, args);

    public static T InvokeStatic<T>(Type type, string name, params object?[] args) =>
        (T)GetMethod(type, name).Invoke(null, args)!;

    private static MethodInfo GetMethod(Type type, string name) =>
        type.GetMethod(name, MemberFlags)
        ?? throw new InvalidOperationException($"Missing method '{name}' on {type.Name}.");
}

internal sealed class TemporaryDatabase : IDisposable
{
    private readonly string originalDirectory = Directory.GetCurrentDirectory();
    private readonly string testDirectory = Path.Combine(
        Path.GetTempPath(),
        "CasinoApp.Tests",
        Guid.NewGuid().ToString("N")
    );

    public TemporaryDatabase()
    {
        Directory.CreateDirectory(testDirectory);
        Directory.SetCurrentDirectory(testDirectory);
        DatabaseInitializer.Initialize();
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(originalDirectory);
        SqliteConnection.ClearAllPools();
        Directory.Delete(testDirectory, recursive: true);
    }
}
