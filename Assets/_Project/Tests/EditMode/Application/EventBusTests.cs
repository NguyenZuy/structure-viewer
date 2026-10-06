using System;
using System.Collections.Generic;
using NUnit.Framework;
using StructureViewer.Application.Events;

namespace StructureViewer.Tests.EditMode.Application
{
    public sealed class EventBusTests
    {
        private readonly struct Ping
        {
            public readonly int Value;
            public Ping(int value) => Value = value;
        }

        private readonly struct Pong
        {
        }

        private EventBus _bus;

        [SetUp]
        public void SetUp() => _bus = new EventBus();

        [Test]
        public void Publish_SubscribedType_DeliversEvent()
        {
            int received = 0;
            _bus.Subscribe<Ping>(e => received = e.Value);

            _bus.Publish(new Ping(7));

            Assert.AreEqual(7, received);
        }

        [Test]
        public void Publish_OtherType_DoesNotDeliver()
        {
            int calls = 0;
            _bus.Subscribe<Ping>(_ => calls++);

            _bus.Publish(new Pong());

            Assert.AreEqual(0, calls);
        }

        [Test]
        public void Publish_NoSubscribers_DoesNothing()
        {
            Assert.DoesNotThrow(() => _bus.Publish(new Ping(1)));
        }

        [Test]
        public void Dispose_Subscription_StopsDelivery()
        {
            int calls = 0;
            var subscription = _bus.Subscribe<Ping>(_ => calls++);

            subscription.Dispose();
            _bus.Publish(new Ping(1));

            Assert.AreEqual(0, calls);
        }

        [Test]
        public void Dispose_Twice_IsSafeAndKeepsOtherSubscribers()
        {
            int calls = 0;
            var first = _bus.Subscribe<Ping>(_ => { });
            _bus.Subscribe<Ping>(_ => calls++);

            first.Dispose();
            Assert.DoesNotThrow(() => first.Dispose());
            _bus.Publish(new Ping(1));

            Assert.AreEqual(1, calls);
        }

        [Test]
        public void Dispose_SameHandlerSubscribedTwice_RemovesOnlyOneSubscription()
        {
            int calls = 0;
            Action<Ping> handler = _ => calls++;
            var first = _bus.Subscribe(handler);
            _bus.Subscribe(handler);

            first.Dispose();
            _bus.Publish(new Ping(1));

            Assert.AreEqual(1, calls);
        }

        [Test]
        public void Publish_HandlerUnsubscribesItself_OtherHandlersStillCalled()
        {
            var order = new List<string>();
            IDisposable self = null;
            _bus.Subscribe<Ping>(_ => order.Add("a"));
            self = _bus.Subscribe<Ping>(_ =>
            {
                order.Add("b");
                self.Dispose();
            });
            _bus.Subscribe<Ping>(_ => order.Add("c"));

            _bus.Publish(new Ping(1));
            _bus.Publish(new Ping(2));

            CollectionAssert.AreEqual(new[] { "a", "b", "c", "a", "c" }, order);
        }

        [Test]
        public void Publish_HandlerSubscribesAnother_NewHandlerCalledFromNextPublish()
        {
            int lateCalls = 0;
            bool added = false;
            _bus.Subscribe<Ping>(_ =>
            {
                if (added)
                    return;
                added = true;
                _bus.Subscribe<Ping>(__ => lateCalls++);
            });

            _bus.Publish(new Ping(1));
            Assert.AreEqual(0, lateCalls);

            _bus.Publish(new Ping(2));
            Assert.AreEqual(1, lateCalls);
        }
    }
}
