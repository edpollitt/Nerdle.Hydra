using System;
using System.Collections.Generic;
using FluentAssertions;
using Moq;
using Nerdle.Hydra.Exceptions;
using NUnit.Framework;

namespace Nerdle.Hydra.Tests.Unit.DynamicClusterTests
{
    [TestFixture]
    class When_executing_a_query : _on_a_dynamic_cluster_of<IEnumerator<int>>
    {
        readonly Func<IEnumerator<int>, int> _theQuery = enumerator => enumerator.Current;

        [Test]
        public void A_read_lock_is_obtained()
        {
            foreach (var component in Components)
                component.Value.Setup(c => c.IsAvailable).Returns(true);

            Sut.Execute(_theQuery);
            SyncManagerProxy.ReadOnlyLocksTaken.Should().Be(1);
        }

        [TestCase(true, true, true, Primary)]
        [TestCase(true, false, false, Primary)]
        [TestCase(false, true, true, Secondary)]
        [TestCase(false, true, false, Secondary)]
        [TestCase(false, false, true, Tertiary)]
        public void The_call_is_routed_to_the_first_working_component(bool primaryAvailability,
            bool secondaryAvailability, bool tertiaryAvailability, string expectedHandler)
        {
            Components[Primary].Setup(component => component.IsAvailable).Returns(primaryAvailability);
            Components[Secondary].Setup(component => component.IsAvailable).Returns(secondaryAvailability);
            Components[Tertiary].Setup(component => component.IsAvailable).Returns(tertiaryAvailability);

            var result = Sut.Execute(_theQuery);

            foreach (var component in Components)
                component.Value.Verify(c => c.Execute(_theQuery),
                    component.Key == expectedHandler ? Times.Once() : Times.Never());

            result.HandledByComponentId.Should().Be(expectedHandler);
        }

        [TestCase(1)]
        [TestCase(5)]
        [TestCase(int.MinValue)]
        public void The_query_result_is_returned(int queryResult)
        {
            Components[Primary].Setup(component => component.IsAvailable).Returns(true);
            Components[Primary].Setup(component => component.Execute(_theQuery)).Returns(queryResult);

            var actual = Sut.Execute(_theQuery);

            actual.Result.Should().Be(queryResult);
        }

        [Test]
        public void An_exception_is_thrown_if_no_working_component_is_available()
        {
            foreach (var component in Components)
                component.Value.Setup(c => c.IsAvailable).Returns(false);

            Action executing = () => Sut.Execute(_theQuery);

            executing.ShouldThrow<ClusterFailureException>()
                .WithMessage("There are no currently available components in the cluster to process the request.");
        }

        [Test]
        public void An_exception_is_thrown_if_components_are_available_but_no_component_successfully_handled_the_request()
        {
            foreach (var component in Components)
            {
                component.Value.Setup(c => c.IsAvailable).Returns(true);
                component.Value.Setup(c => c.Execute(_theQuery)).Throws<AccessViolationException>();
            }

            Action executing = () => Sut.Execute(_theQuery);

            executing.ShouldThrow<ClusterFailureException>()
                .WithMessage("There are available components in the cluster, but the request was not successfully processed by any component.")
                .And.InnerException.Should().BeOfType<AggregateException>().Which.InnerExceptions.Should().HaveCount(3);
        }

        [Test]
        public void Availability_of_components_is_evaluated_lazily()
        {
            Components[Primary].Setup(component => component.IsAvailable).Returns(false);
            Components[Secondary].Setup(component => component.IsAvailable).Returns(true);

            Sut.Execute(_theQuery);

            foreach (var component in Components)
                component.Value.Verify(c => c.IsAvailable, component.Key == Tertiary ? Times.Never() : Times.Once());
        }
    }
}