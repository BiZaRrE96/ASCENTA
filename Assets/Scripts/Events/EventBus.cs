using System;
using System.Collections.Generic;
using UnityEngine;

namespace ASCENTA.Events
{
    /// <summary>
    /// Lightweight publisher/subscriber bus for routing arbitrary game events.
    /// </summary>
    public static class EventBus
    {
        static readonly Dictionary<Type, List<Delegate>> subscribers = new Dictionary<Type, List<Delegate>>();
        static readonly object syncRoot = new object();

        /// <summary>
        /// Subscribe to events of type <typeparamref name="T"/>.
        /// </summary>
        public static bool Subscribe<T>(Action<T> handler) where T : IEvent
        {
            if (handler == null)
            {
                Debug.LogWarning("EventBus.Subscribe received a null handler.");
                return false;
            }

            lock (syncRoot)
            {
                var eventType = typeof(T);
                if (!subscribers.TryGetValue(eventType, out List<Delegate> handlers))
                {
                    handlers = new List<Delegate>();
                    subscribers[eventType] = handlers;
                }

                if (handlers.Contains(handler))
                {
                    return false;
                }

                handlers.Add(handler);
                return true;
            }
        }

        /// <summary>
        /// Unsubscribe a previously registered handler.
        /// </summary>
        public static bool Unsubscribe<T>(Action<T> handler) where T : IEvent
        {
            if (handler == null)
            {
                return false;
            }

            lock (syncRoot)
            {
                var eventType = typeof(T);
                if (!subscribers.TryGetValue(eventType, out List<Delegate> handlers))
                {
                    return false;
                }

                bool removed = handlers.Remove(handler);
                if (removed && handlers.Count == 0)
                {
                    subscribers.Remove(eventType);
                }

                return removed;
            }
        }

        /// <summary>
        /// Publish an event instance to all subscribers.
        /// </summary>
        public static bool Publish<T>(T eventData) where T : IEvent
        {
            List<Delegate> snapshot = null;

            lock (syncRoot)
            {
                var eventType = typeof(T);
                if (subscribers.TryGetValue(eventType, out List<Delegate> handlers) && handlers.Count > 0)
                {
                    snapshot = new List<Delegate>(handlers);
                }
            }

            if (snapshot == null)
            {
                return false;
            }

            foreach (Action<T> handler in snapshot)
            {
                try
                {
                    handler.Invoke(eventData);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }

            return true;
        }

        /// <summary>
        /// Remove all subscriptions (useful for domain reloads or tests).
        /// </summary>
        public static bool Clear()
        {
            lock (syncRoot)
            {
                if (subscribers.Count == 0)
                {
                    return false;
                }

                subscribers.Clear();
                return true;
            }
        }
    }
}
