using System;
using System.Collections.Generic;
using UnityEngine;
using Project.Runtime.Interfaces;
using Project.Utility;

namespace Project.Runtime.Events
{
    /// <summary>
    /// Centralized event management system for the robot project.
    /// Implements the Event Bus pattern for loose coupling between components.
    /// Follows Singleton pattern for global access while maintaining SOLID principles.
    /// </summary>
    public class EventManager : Singleton<EventManager>, IEventManager
    {
        /// <summary>
        /// Dictionary storing event subscribers by event type
        /// </summary>
        private readonly Dictionary<Type, List<object>> eventHandlers = new Dictionary<Type, List<object>>();

        /// <summary>
        /// Lock object for thread-safe operations
        /// </summary>
        private readonly object eventLock = new object();

        #region IEventManager Implementation

        /// <summary>
        /// Subscribes to an event of specific type.
        /// Uses generics for type safety and better performance.
        /// </summary>
        /// <typeparam name="T">Event type to subscribe to</typeparam>
        /// <param name="handler">Event handler delegate</param>
        public void Subscribe<T>(Action<T> handler) where T : class
        {
            if (handler == null)
            {
                Debug.LogWarning($"[EventManager] Attempted to subscribe null handler for event type {typeof(T).Name}");
                return;
            }

            lock (eventLock)
            {
                Type eventType = typeof(T);

                if (!eventHandlers.ContainsKey(eventType))
                {
                    eventHandlers[eventType] = new List<object>();
                }

                eventHandlers[eventType].Add(handler);
                Debug.Log($"[EventManager] Subscribed to event type: {eventType.Name}. Total subscribers: {eventHandlers[eventType].Count}");
            }
        }

        /// <summary>
        /// Unsubscribes from an event of specific type.
        /// Ensures proper cleanup to prevent memory leaks.
        /// </summary>
        /// <typeparam name="T">Event type to unsubscribe from</typeparam>
        /// <param name="handler">Event handler delegate</param>
        public void Unsubscribe<T>(Action<T> handler) where T : class
        {
            if (handler == null)
            {
                Debug.LogWarning($"[EventManager] Attempted to unsubscribe null handler for event type {typeof(T).Name}");
                return;
            }

            lock (eventLock)
            {
                Type eventType = typeof(T);

                if (eventHandlers.ContainsKey(eventType))
                {
                    bool removed = eventHandlers[eventType].Remove(handler);

                    if (removed)
                    {
                        Debug.Log($"[EventManager] Unsubscribed from event type: {eventType.Name}. Remaining subscribers: {eventHandlers[eventType].Count}");

                        // Clean up empty handler lists to prevent memory waste
                        if (eventHandlers[eventType].Count == 0)
                        {
                            eventHandlers.Remove(eventType);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[EventManager] Handler not found for event type: {eventType.Name}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[EventManager] No subscribers found for event type: {eventType.Name}");
                }
            }
        }

        /// <summary>
        /// Publishes an event to all subscribers.
        /// Implements error handling to prevent one failing handler from affecting others.
        /// </summary>
        /// <typeparam name="T">Event type to publish</typeparam>
        /// <param name="eventData">Event data to send to subscribers</param>
        public void Publish<T>(T eventData) where T : class
        {
            if (eventData == null)
            {
                Debug.LogWarning($"[EventManager] Attempted to publish null event of type {typeof(T).Name}");
                return;
            }

            Type eventType = typeof(T);
            List<object> handlers = null;

            // Create a copy of handlers to avoid modification during iteration
            lock (eventLock)
            {
                if (eventHandlers.ContainsKey(eventType))
                {
                    handlers = new List<object>(eventHandlers[eventType]);
                }
            }

            if (handlers == null || handlers.Count == 0)
            {
                Debug.Log($"[EventManager] No subscribers for event type: {eventType.Name}");
                return;
            }

            Debug.Log($"[EventManager] Publishing event {eventType.Name} to {handlers.Count} subscribers");

            // Notify all handlers, with error isolation
            foreach (var handler in handlers)
            {
                try
                {
                    if (handler is Action<T> typedHandler)
                    {
                        typedHandler.Invoke(eventData);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[EventManager] Error in event handler for {eventType.Name}: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            Debug.Log("[EventManager] Event Manager initialized");
        }

        private void OnDestroy()
        {
            // Clear all event handlers on destruction
            lock (eventLock)
            {
                eventHandlers.Clear();
            }
            Debug.Log("[EventManager] Event Manager destroyed and cleaned up");
        }

        #endregion

        #region Debug and Statistics

        /// <summary>
        /// Gets the number of subscribers for a specific event type.
        /// Useful for debugging and monitoring system health.
        /// </summary>
        /// <typeparam name="T">Event type to check</typeparam>
        /// <returns>Number of subscribers</returns>
        public int GetSubscriberCount<T>() where T : class
        {
            lock (eventLock)
            {
                Type eventType = typeof(T);
                return eventHandlers.ContainsKey(eventType) ? eventHandlers[eventType].Count : 0;
            }
        }

        /// <summary>
        /// Gets total number of event types being tracked.
        /// Useful for system monitoring and debugging.
        /// </summary>
        /// <returns>Number of event types</returns>
        public int GetEventTypeCount()
        {
            lock (eventLock)
            {
                return eventHandlers.Count;
            }
        }

        /// <summary>
        /// Logs current event system statistics.
        /// Helpful for debugging performance and memory usage.
        /// </summary>
        [ContextMenu("Log Event Statistics")]
        public void LogEventStatistics()
        {
            lock (eventLock)
            {
                Debug.Log($"[EventManager] Event System Statistics:");
                Debug.Log($"  - Total Event Types: {eventHandlers.Count}");

                foreach (var kvp in eventHandlers)
                {
                    Debug.Log($"  - {kvp.Key.Name}: {kvp.Value.Count} subscribers");
                }
            }
        }

        #endregion
    }
}