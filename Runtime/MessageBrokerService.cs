using System;
using System.Collections;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace

namespace GameLovers.Services
{
	/// <summary>
	/// The message contract that must be used for all messages being published via the <see cref="IMessageBrokerService"/>
	/// </summary>
	public interface IMessage { }

	/// <summary>
	/// This services provides the execution of the Message Broker.
	/// It provides a easy way to decouple objects across the system with an independent channel of communication, by dispatching
	/// message events to be caught by all it's observer subscribers.
	/// </summary>
	/// <remarks>
	/// Follows the "Message Broker Pattern" <see cref="https://en.wikipedia.org/wiki/Message_broker"/>
	/// </remarks>
	public interface IMessageBrokerService
	{
		/// <summary>
		/// Publish a message in the message broker.
		/// If there is no object subscribing the message type, nothing will happen
		/// </summary>
		/// <remarks>
		/// Use <see cref="PublishSafe{T}(T)"/> it there are a chain subscriptions during publishing
		/// </remarks>
		void Publish<T>(T message) where T : IMessage;

		/// <summary>
		/// Publish a message in the message broker.
		/// If there is no object subscribing the message type, nothing will happen
		/// </summary>
		/// <remarks>
		/// This method can be slow and allocated extra memory if there are a lot of subscribers to the <typeparamref name="T"/>.
		/// Use <see cref="Publish{T}(T)"/> instead for faster iteration speed IF and ONLY IF there aren't chain subscriptions during publishing
		/// </remarks>
		void PublishSafe<T>(T message) where T : IMessage;

		/// <summary>
		/// Subscribes to the message type.
		/// Will invoke the <paramref name="action"/> every time the message of the subscribed type is published.
		/// </summary>
		void Subscribe<T>(Action<T> action) where T : IMessage;

		/// <summary>
		/// Unsubscribe the action of <typeparamref name="T"/> from the <paramref name="subscriber"/> in the message broker.
		/// If <paramref name="subscriber"/> is null then will unsubscribe from ALL subscribers currently subscribed to <typeparamref name="T"/>
		/// </summary>
		void Unsubscribe<T>(object subscriber = null) where T : IMessage;
		
		/// <summary>
		/// Unsubscribe from all messages.
		/// If <paramref name="subscriber"/> is null then will unsubscribe from EVERYTHING, other wise only from the given subscriber
		/// </summary>
		void UnsubscribeAll(object subscriber = null);
	}

	/// <inheritdoc />
	public class MessageBrokerService : IMessageBrokerService
	{
		private readonly IDictionary<Type, IDictionary<object, Delegate>> _subscriptions = new Dictionary<Type, IDictionary<object, Delegate>>();

		private (bool, IMessage) _isPublishing;

		/// <summary>
		/// Subscribers keyed by message type, then by subscriber target. Editor introspection only — see AGENTS.md §4.
		/// </summary>
		internal IReadOnlyDictionary<Type, IDictionary<object, Delegate>> Subscriptions =>
			(IReadOnlyDictionary<Type, IDictionary<object, Delegate>>)_subscriptions;
		/// <summary>
		/// True while a publish is in flight, when mutating the subscriber list would throw.
		/// Editor introspection only — see AGENTS.md §4.
		/// </summary>
		internal bool IsPublishing => _isPublishing.Item1;

		/// <inheritdoc />
		public void Publish<T>(T message) where T : IMessage
		{
			if (!_subscriptions.TryGetValue(typeof(T), out var subscriptionObjects))
			{
				return;
			}

			_isPublishing = (true, message);

			foreach (var subscription in subscriptionObjects)
			{
				var action = (Action<T>)subscription.Value;

				action(message);
			}

			_isPublishing = (false, message);
		}

		/// <inheritdoc />
		public void PublishSafe<T>(T message) where T : IMessage
		{
			if (!_subscriptions.TryGetValue(typeof(T), out var subscriptionObjects))
			{
				return;
			}

			var subscriptionCopy = new Delegate[subscriptionObjects.Count];

			subscriptionObjects.Values.CopyTo(subscriptionCopy, 0);

			for (var i = 0; i < subscriptionCopy.Length; i++)
			{
				var action = (Action<T>)subscriptionCopy[i];

				action(message);
			}
		}

		/// <inheritdoc />
		public void Subscribe<T>(Action<T> action) where T : IMessage
		{
			var type = typeof(T);
			var subscriber = action.Target;

			if (subscriber == null)
			{
				throw new ArgumentException("Subscribe static functions to a message is not supported!");
			}
			if(_isPublishing.Item1)
			{
				throw new InvalidOperationException($"Cannot subscribe to {type.Name} message while publishing " +
					$"{_isPublishing.Item2.GetType().Name} message. Use {nameof(PublishSafe)} instead!");
			}

			if (!_subscriptions.TryGetValue(type, out var subscriptionObjects))
			{
				subscriptionObjects = new Dictionary<object, Delegate>();
				_subscriptions.Add(type, subscriptionObjects);
			}

			subscriptionObjects[subscriber] = action;
		}

		/// <inheritdoc />
		public void Unsubscribe<T>(object subscriber = null) where T : IMessage
		{
			var type = typeof(T);

			if (subscriber == null)
			{
				_subscriptions.Remove(type);

				return;
			}

			if (_isPublishing.Item1)
			{
				throw new InvalidOperationException($"Cannot unsubscribe to {type.Name} message while publishing " +
					$"{_isPublishing.Item2.GetType().Name} message. Use {nameof(PublishSafe)} instead!");
			}
			if (!_subscriptions.TryGetValue(type, out var subscriptionObjects))
			{
				return;
			}

			subscriptionObjects.Remove(subscriber);

			if (subscriptionObjects.Count == 0)
			{
				_subscriptions.Remove(type);
			}
		}

		/// <inheritdoc />
		public void UnsubscribeAll(object subscriber = null)
		{
			if (subscriber == null)
			{
				_subscriptions.Clear();
				return;
			}

			if (_isPublishing.Item1)
			{
				throw new InvalidOperationException($"Cannot unsubscribe from {subscriber} message while publishing " +
					$"{_isPublishing.Item2.GetType().Name} message. Use {nameof(PublishSafe)} instead!");
			}
			foreach (var subscriptionObjects in _subscriptions.Values)
			{
				subscriptionObjects.Remove(subscriber);
			}
		}
	}
}
