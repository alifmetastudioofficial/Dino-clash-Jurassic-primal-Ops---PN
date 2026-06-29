using System;
using System.Collections.Generic;
using UnityEngine;

public interface ISignal { }

public static class SignalBus
{
    private static readonly Dictionary<Type, Delegate> _signalTable = new Dictionary<Type, Delegate>();

    public static void Subscribe<T>(Action<T> listener) where T : ISignal
    {
        var type = typeof(T);

        if (_signalTable.TryGetValue(type, out var existingDelegate))
        {
            _signalTable[type] = Delegate.Combine(existingDelegate, listener);
        }
        else
        {
            _signalTable[type] = listener;
        }

        Debug.Log($"[SignalBus] SUBSCRIBE: {type.Name} -> {listener.Method.Name} (Target: {listener.Target})");
    }

    public static void Unsubscribe<T>(Action<T> listener) where T : ISignal
    {
        var type = typeof(T);

        if (_signalTable.TryGetValue(type, out var existingDelegate))
        {
            var newDelegate = Delegate.Remove(existingDelegate, listener);

            if (newDelegate == null)
            {
                _signalTable.Remove(type);
                Debug.Log($"[SignalBus] UNSUBSCRIBE: {type.Name} -> {listener.Method.Name} (Target: {listener.Target}) | No listeners left");
            }
            else
            {
                _signalTable[type] = newDelegate;
                Debug.Log($"[SignalBus] UNSUBSCRIBE: {type.Name} -> {listener.Method.Name} (Target: {listener.Target})");
            }
        }
        else
        {
            Debug.LogWarning($"[SignalBus] Tried to UNSUBSCRIBE from {type.Name} but no listeners registered");
        }
    }

    public static void Publish<T>(T signal) where T : ISignal
    {
        var type = typeof(T);

        if (_signalTable.TryGetValue(type, out var d))
        {
            var callback = d as Action<T>;
            var count = callback?.GetInvocationList().Length ?? 0;

            Debug.Log($"[SignalBus] PUBLISH: {type.Name} -> {count} listener(s)");

            callback?.Invoke(signal);
        }
        else
        {
            Debug.Log($"[SignalBus] PUBLISH: {type.Name} -> no listeners");
        }
    }

    public static void ClearAll()
    {
        _signalTable.Clear();
        Debug.Log("[SignalBus] CLEARED all signals");
    }
}
