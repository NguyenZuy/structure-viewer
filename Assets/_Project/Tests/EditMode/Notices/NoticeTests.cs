using System;
using NUnit.Framework;
using StructureViewer.Application.Events;
using StructureViewer.Application.Onboarding;
using StructureViewer.Presentation.Contracts;
using StructureViewer.Presentation.Notices;
using StructureViewer.Tests.Fixtures;
using UnityEngine;
using UnityEngine.UIElements;

namespace StructureViewer.Tests.EditMode.Notices
{
    public sealed class NoticeTests
    {
        [Test]
        public void Onboarding_FirstVisit_ShowsHintForTheGuessedDevice()
        {
            var view = new FakeNoticeView();

            new OnboardingPresenter(view, new FakeStore(), new FakePointerEvents(), PointerDevice.Touch);

            Assert.IsTrue(view.Shown);
            Assert.AreEqual(OnboardingPresenter.TouchHint, view.Message);
            Assert.IsNull(view.ActionLabel);
        }

        [Test]
        public void Onboarding_AlreadySeen_StaysHidden()
        {
            var view = new FakeNoticeView();

            new OnboardingPresenter(view, new FakeStore { HasSeenHint = true }, new FakePointerEvents(), PointerDevice.Mouse);

            Assert.IsFalse(view.Shown);
        }

        [Test]
        public void Onboarding_Dismiss_HidesAndRemembers()
        {
            var view = new FakeNoticeView();
            var store = new FakeStore();
            new OnboardingPresenter(view, store, new FakePointerEvents(), PointerDevice.Mouse);

            view.ClickClose();

            Assert.IsFalse(view.Shown);
            Assert.IsTrue(store.HasSeenHint);
        }

        [Test]
        public void Onboarding_FirstTouchTap_SwitchesWording()
        {
            var view = new FakeNoticeView();
            var pointer = new FakePointerEvents();
            new OnboardingPresenter(view, new FakeStore(), pointer, PointerDevice.Mouse);

            pointer.Current = PointerDevice.Touch;
            pointer.RaiseTap(Vector2.zero);

            Assert.AreEqual(OnboardingPresenter.TouchHint, view.Message);
        }

        [Test]
        public void EverythingHidden_ShowsShowAll_OnlyWhenNothingIsVisible()
        {
            var bus = new EventBus();
            var view = new FakeNoticeView();
            int showAllCalls = 0;
            using var presenter = new EverythingHiddenPresenter(view, new FakeStructureRenderer(3), () => showAllCalls++, bus);

            bus.Publish(new VisibilityChanged(new FakeVisibility(0, 1)));
            Assert.IsFalse(view.Shown);

            bus.Publish(new VisibilityChanged(new FakeVisibility(0, 1, 2)));
            Assert.IsTrue(view.Shown);
            Assert.AreEqual(EverythingHiddenPresenter.ActionLabel, view.ActionLabel);

            view.ClickAction();
            Assert.AreEqual(1, showAllCalls);

            bus.Publish(new VisibilityChanged(new FakeVisibility()));
            Assert.IsFalse(view.Shown);
        }

        [Test]
        public void NoticeView_ShowAndHide_ToggleDisplayAndActionButton()
        {
            var view = new NoticeView("test-notice");

            view.Show("Hello", null);
            Assert.IsTrue(view.IsShown);
            Assert.AreEqual(DisplayStyle.None, view.Root.Query<Button>().First().style.display.value);

            view.Hide();
            Assert.IsFalse(view.IsShown);
        }

        private sealed class FakeStore : IOnboardingStore
        {
            public bool HasSeenHint { get; set; }
            public void MarkHintSeen() => HasSeenHint = true;
        }

        private sealed class FakeNoticeView : INoticeView
        {
            public event Action ActionClicked;
            public event Action CloseClicked;

            public bool Shown { get; private set; }
            public string Message { get; private set; }
            public string ActionLabel { get; private set; }

            public void Show(string message, string actionLabel)
            {
                Shown = true;
                Message = message;
                ActionLabel = actionLabel;
            }

            public void Hide() => Shown = false;

            public void ClickAction() => ActionClicked?.Invoke();
            public void ClickClose() => CloseClicked?.Invoke();
        }
    }
}
