// <copyright file="DataSourceBaseTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using System.Collections;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MUnique.OpenMU.Persistence;

/// <summary>
/// Tests for the <see cref="DataSourceBase{TOwner}"/>.
/// </summary>
[TestFixture]
public class DataSourceBaseTests
{
    [Test]
    public async Task SwitchingOwnerCreatesFreshContext()
    {
        var firstOwner = new TestOwner { Id = Guid.NewGuid() };
        var secondOwner = new TestOwner { Id = Guid.NewGuid() };
        var firstContext = CreateContext(firstOwner);
        var secondContext = CreateContext(secondOwner);
        var contextProvider = new Mock<IPersistenceContextProvider>();
        contextProvider.SetupSequence(provider => provider.CreateNewContext())
            .Returns(firstContext.Object)
            .Returns(secondContext.Object);
        using var dataSource = new TestDataSource(
            NullLogger<DataSourceBase<TestOwner>>.Instance,
            contextProvider.Object);

        var loadedFirstOwner = await dataSource.GetOwnerAsync(firstOwner.Id).ConfigureAwait(false);
        var loadedSecondOwner = await dataSource.GetOwnerAsync(secondOwner.Id).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(loadedFirstOwner, Is.SameAs(firstOwner));
            Assert.That(loadedSecondOwner, Is.SameAs(secondOwner));
        });
        contextProvider.Verify(provider => provider.CreateNewContext(), Times.Exactly(2));
        firstContext.Verify(context => context.Dispose(), Times.Once);
    }

    private static Mock<IContext> CreateContext(TestOwner owner)
    {
        var context = new Mock<IContext>();
        context.Setup(c => c.GetByIdAsync<TestOwner>(owner.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(owner);
        return context;
    }

    private sealed class TestDataSource(
        ILogger<DataSourceBase<TestOwner>> logger,
        IPersistenceContextProvider persistenceContextProvider)
        : DataSourceBase<TestOwner>(logger, persistenceContextProvider)
    {
        protected override IReadOnlyDictionary<Type, Func<TestOwner, IEnumerable>> TypeToEnumerables { get; }
            = new Dictionary<Type, Func<TestOwner, IEnumerable>>();
    }

    private sealed class TestOwner : IIdentifiable
    {
        public Guid Id { get; set; }
    }
}
