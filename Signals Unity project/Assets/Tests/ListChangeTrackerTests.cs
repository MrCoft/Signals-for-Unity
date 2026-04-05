using NUnit.Framework;
using System.Collections.Generic;

namespace Coft.Signals.Tests
{
    public class ListChangeTrackerTests
    {
        [Test]
        public void Tracker_FirstUpdate_TreatsEverythingAsAdded()
        {
            var source = new List<string> { "a", "b", "c" };
            var tracker = new ListChangeTracker<string>(() => source);

            tracker.Update();

            CollectionAssert.AreEquivalent(new[] { "a", "b", "c" }, tracker.Added);
            CollectionAssert.IsEmpty(tracker.Removed);
        }

        [Test]
        public void Tracker_NoChanges_AddedAndRemovedAreEmpty()
        {
            var source = new List<string> { "a", "b" };
            var tracker = new ListChangeTracker<string>(() => source);
            tracker.Update();

            tracker.Update();

            CollectionAssert.IsEmpty(tracker.Added);
            CollectionAssert.IsEmpty(tracker.Removed);
        }
        
        [Test]
        public void Tracker_AddAndRemove_BothReported()
        {
            var source = new List<string> { "a", "b" };
            var tracker = new ListChangeTracker<string>(() => source);
            tracker.Update();

            source.Remove("a");
            source.Add("c");
            tracker.Update();

            CollectionAssert.AreEquivalent(new[] { "c" }, tracker.Added);
            CollectionAssert.AreEquivalent(new[] { "a" }, tracker.Removed);
        }

        [Test]
        public void Tracker_InsideEffect_DetectsChangesAcrossRuns()
        {
            var context = new SignalContext();
            var list = context.List<string>(0);
            var tracker = new ListChangeTracker<string>(() => list);
            var addedSnapshot = new List<string>();
            var removedSnapshot = new List<string>();
            context.Effect(0, () =>
            {
                tracker.Update();
                addedSnapshot.Clear();
                addedSnapshot.AddRange(tracker.Added);
                removedSnapshot.Clear();
                removedSnapshot.AddRange(tracker.Removed);
            });

            list.GetMutable().Add("sword");
            context.Update(0);
            CollectionAssert.AreEquivalent(new[] { "sword" }, addedSnapshot);
            CollectionAssert.IsEmpty(removedSnapshot);

            var mutable = list.GetMutable();
            mutable.Remove("sword");
            mutable.Add("shield");
            context.Update(0);
            CollectionAssert.AreEquivalent(new[] { "shield" }, addedSnapshot);
            CollectionAssert.AreEquivalent(new[] { "sword" }, removedSnapshot);
        }

        [Test]
        public void Tracker_GetterSwappedToDifferentList_OldRemovedNewAdded()
        {
            var listA = new List<string> { "a1", "a2" };
            var listB = new List<string> { "b1", "b2" };
            var useA = true;
            var tracker = new ListChangeTracker<string>(() => useA ? listA : listB);
            tracker.Update();

            useA = false;
            tracker.Update();

            CollectionAssert.AreEquivalent(new[] { "b1", "b2" }, tracker.Added);
            CollectionAssert.AreEquivalent(new[] { "a1", "a2" }, tracker.Removed);
        }
    }
}
