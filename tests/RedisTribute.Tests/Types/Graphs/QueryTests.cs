using NSubstitute;
using RedisTribute.Types.Graphs;
using RedisTribute.UnitTests.Serialization;
using System.Threading.Tasks;
using Xunit;

namespace RedisTribute.UnitTests.Types.Graphs
{
    public class QueryTests
    {
        [Fact]
        public async Task ExecuteAsync_MatchingLabelQuery_ReturnsTrue()
        {
            var query = Query<TestComplexDto>
                .Create()
                .WithLabel(x => x.Contains("b"))
                .Build();

            var vertex = Substitute.For<IVertex<TestComplexDto>>();

            vertex.Label.Returns("abc");

            Assert.True(await query.ExecuteAsync(vertex));
        }

        [Fact]
        public async Task ExecuteAsync_TwoMatchingLabelConditions_ReturnsTrue()
        {
            var query = Query<TestComplexDto>
                .Create()
                .WithLabel(x => x.Contains("b"))
                .WithLabel(x => x.Contains("c"))
                .Build();

            var vertex = Substitute.For<IVertex<TestComplexDto>>();

            vertex.Label.Returns("abc");

            Assert.True(await query.ExecuteAsync(vertex));
        }

        [Fact]
        public async Task ExecuteAsync_TwoLabelConditionsOneMatching_ReturnsFalse()
        {
            var query = Query<TestComplexDto>
                .Create()
                .WithLabel(x => x.Contains("b"))
                .WithLabel(x => x.Contains("z"))
                .Build();

            var vertex = Substitute.For<IVertex<TestComplexDto>>();

            vertex.Label.Returns("abc");

            Assert.False(await query.ExecuteAsync(vertex));
        }
    }
}
