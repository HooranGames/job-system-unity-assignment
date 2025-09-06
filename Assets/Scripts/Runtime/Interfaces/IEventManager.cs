using System;

/// <summary>
/// Interface for event management system.
/// Provides centralized event handling for the robot system.
/// </summary>
namespace Project.Runtime.Interfaces
{
    public interface IEventManager
    {
        /// <summary>
        /// Subscribes to an event of specific type
        /// </summary>
        /// <typeparam name="T">Event type</typeparam>
        /// <param name="handler">Event handler delegate</param>
        void Subscribe<T>(Action<T> handler) where T : class;

        /// <summary>
        /// Unsubscribes from an event of specific type
        /// </summary>
        /// <typeparam name="T">Event type</typeparam>
        /// <param name="handler">Event handler delegate</param>
        void Unsubscribe<T>(Action<T> handler) where T : class;

        /// <summary>
        /// Publishes an event to all subscribers
        /// </summary>
        /// <typeparam name="T">Event type</typeparam>
        /// <param name="eventData">Event data</param>
        void Publish<T>(T eventData) where T : class;
    }
}