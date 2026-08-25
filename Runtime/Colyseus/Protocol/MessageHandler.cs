using System;

namespace Colyseus
{
    /// <summary>
    ///     Base interface for MessageHandlers
    /// </summary>
    public interface IMessageHandler
    {
        /// <summary>
        ///     Message Type
        /// </summary>
        Type Type { get; }

        /// <summary>
        ///     Base invocation for the MessageHandler
        /// </summary>
        /// <param name="message">The data to be passed into the function</param>
        void Invoke(object message);

        /// <summary>
        ///     Whether all handlers registered for this message type have been removed
        /// </summary>
        bool IsEmpty { get; }
    }

    /// <summary>
    ///     Base Implementation of the IMessageHandler interface
    /// </summary>
    /// <typeparam name="T">Message Type</typeparam>
    public class MessageHandler<T> : IMessageHandler
    {
        /// <summary>
        ///     The Action this message will invoke. Multicast: every handler registered through
        ///     <c>room.OnMessage()</c> for this message type is combined here, in registration order.
        /// </summary>
        public Action<T> Action;

        /// <summary>
        ///     Invokes this message's Action
        /// </summary>
        /// <param name="message">Data for the Action, will be cast to "T"</param>
        public void Invoke(object message)
        {
            Action?.Invoke((T) message);
        }

        /// <summary>
        ///     Implementation of the interface Type
        /// </summary>
        /// <returns>typeof(T)</returns>
        public Type Type
        {
            get { return typeof(T); }
        }

        /// <summary>
        ///     Implementation of the interface IsEmpty
        /// </summary>
        /// <returns>true once every registered handler has been removed</returns>
        public bool IsEmpty
        {
            get { return Action == null; }
        }
    }
}