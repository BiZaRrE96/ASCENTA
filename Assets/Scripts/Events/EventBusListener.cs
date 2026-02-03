using System;
using UnityEngine;

namespace ASCENTA.Events
{
    /// <summary>
    /// Base <see cref="MonoBehaviour"/> that can automatically listen for typed events on the <see cref="EventBus"/>.
    /// Derived classes must provide <see cref="OnEvent(T)"/>.
    /// </summary>
    public abstract class EventBusListener<T> : MonoBehaviour where T : IEvent
    {
        [SerializeField, Tooltip("Automatically subscribe when the GameObject is enabled.")]
        bool autoSubscribe = true;

        Action<T> handler;
        bool isSubscribed;

        protected virtual void Awake()
        {
            handler = Raise;
        }

        protected virtual void OnEnable()
        {
            if (autoSubscribe)
            {
                Subscribe();
            }
        }

        protected virtual void OnDisable()
        {
            if (isSubscribed)
            {
                Unsubscribe();
            }
        }

        protected virtual void OnDestroy()
        {
            if (isSubscribed)
            {
                Unsubscribe();
            }
        }

        /// <summary>
        /// Subscribe to the bus manually (useful if auto-subscribe is disabled).
        /// </summary>
        protected void Subscribe()
        {
            if (handler == null || isSubscribed)
            {
                return;
            }

            if (!EventBus.Subscribe(handler))
            {
                OnSubscribeFailed();
                return;
            }

            isSubscribed = true;
        }

        /// <summary>
        /// Called when the listener could not subscribe to the event bus.
        /// </summary>
        protected virtual void OnSubscribeFailed()
        {
            Debug.LogWarning("EventBusListener failed to subscribe a handler; it may already be registered.");
        }

        /// <summary>
        /// Unsubscribe from the bus.
        /// </summary>
        protected void Unsubscribe()
        {
            if (handler == null || !isSubscribed)
            {
                return;
            }

            EventBus.Unsubscribe(handler);
            isSubscribed = false;
        }

        void Raise(T eventData)
        {
            OnEvent(eventData);
        }

        /// <summary>
        /// Override to respond to events of type <typeparamref name="T"/>.
        /// </summary>
        protected abstract void OnEvent(T eventData);

        /// <summary>
        /// Publish an event to the bus.
        /// </summary>
        protected bool Publish(T eventData) => EventBus.Publish(eventData);
    }
}
