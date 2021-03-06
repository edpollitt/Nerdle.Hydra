using System;
using System.Threading;
using Moq;
using Nerdle.Hydra.InfrastructureAbstractions;
using Nerdle.Hydra.StateManagement;
using Nerdle.Hydra.Tests.Unit.TestHelpers;
using NUnit.Framework;

namespace Nerdle.Hydra.Tests.Unit.StateManagement.RollingWindowStateManagerTests
{
    class _to_a_rolling_window_state_manager
    {
        protected CountingSyncManagerProxy SyncManagerProxy;
        protected Mock<IClock> Clock;
        protected Mock<IRollingWindow> SuccessWindow;
        protected Mock<IRollingWindow> FailureWindow;
        protected Mock<ICondition<int, int>> FailureCondition;

        protected readonly TimeSpan FailFor = TimeSpan.FromMinutes(1);

        [SetUp]
        public void BeforeEach()
        {
            SyncManagerProxy = new CountingSyncManagerProxy(new SyncManager(new ReaderWriterLockSlim(), TimeSpan.Zero));
            Clock = new Mock<IClock>();
            Clock.Setup(c => c.UtcNow).Returns(DateTime.UtcNow);
            SuccessWindow = new Mock<IRollingWindow>();
            FailureWindow = new Mock<IRollingWindow>();
            FailureCondition = new Mock<ICondition<int, int>>();
        }

        protected RollingWindowStateManager RollingWindowAveragingStateManagerWithState(State state)
        {
            return new RollingWindowStateManager(
                SuccessWindow.Object,
                FailureWindow.Object,
                SyncManagerProxy,
                FailFor,
                Clock.Object,
                FailureCondition.Object,
                state);
        }
    }
}