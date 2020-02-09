using NSubstitute;
using RedisTribute.Types.Graphs;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace RedisTribute.UnitTests.Types.Graphs
{
    public class QueryExtensionsTests
    {
        [Fact]
        public async Task ExecuteAsync_SomeQuery_ReturnsResults()
        {
            var vertex = Substitute.For<IVertex<string>>();

            vertex.TraverseAsync(Arg.Any<IVisitor<string>>()).Returns(async call =>
            {
                await call.Arg<IVisitor<string>>().VisitAsync(vertex, default);
            });

            vertex.Label.Returns("abc");
            vertex.Attributes.Returns("xyz");

            var query = Query<string>.Create()
                .HasLabel("abc")
                .HasAttributes(a => a.Contains("x")).Build();

            var results = await vertex.ExecuteAsync(query);

            Assert.True(results.Any());
        }
    }
}
