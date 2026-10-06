using System;
using System.Collections.Generic;

namespace StructureViewer.Application.Events
{
    public sealed class EventBus
    {
        private readonly Dictionary<Type, object> _handlers = new Dictionary<Type, object>();

        public void Publish<T>(T evt) where T : struct
        {
            if (!_handlers.TryGetValue(typeof(T), out var list))
                return;

            // Copy-on-write array: handlers may (un)subscribe mid-publish without affecting this pass, and publishing allocates nothing.
            var snapshot = ((HandlerList<T>)list).Items;
            for (int i = 0; i < snapshot.Length; i++)
                snapshot[i](evt);
        }

        public IDisposable Subscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            if (!_handlers.TryGetValue(typeof(T), out var list))
            {
                list = new HandlerList<T>();
                _handlers.Add(typeof(T), list);
            }

            var typed = (HandlerList<T>)list;
            typed.Add(handler);
            return new Subscription<T>(typed, handler);
        }

        private sealed class HandlerList<T>
        {
            public Action<T>[] Items = Array.Empty<Action<T>>();

            public void Add(Action<T> handler)
            {
                var next = new Action<T>[Items.Length + 1];
                Array.Copy(Items, next, Items.Length);
                next[Items.Length] = handler;
                Items = next;
            }

            public void Remove(Action<T> handler)
            {
                // Reference identity so two subscriptions of the same method stay independent.
                int index = -1;
                for (int i = 0; i < Items.Length && index < 0; i++)
                {
                    if (ReferenceEquals(Items[i], handler))
                        index = i;
                }
                if (index < 0)
                    return;

                var next = new Action<T>[Items.Length - 1];
                Array.Copy(Items, 0, next, 0, index);
                Array.Copy(Items, index + 1, next, index, Items.Length - index - 1);
                Items = next;
            }
        }

        private sealed class Subscription<T> : IDisposable
        {
            private HandlerList<T> _list;
            private Action<T> _handler;

            public Subscription(HandlerList<T> list, Action<T> handler)
            {
                _list = list;
                _handler = handler;
            }

            public void Dispose()
            {
                if (_list == null)
                    return;

                _list.Remove(_handler);
                _list = null;
                _handler = null;
            }
        }
    }
}
