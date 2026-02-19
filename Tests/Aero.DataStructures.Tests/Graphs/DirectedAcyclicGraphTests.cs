using FluentAssertions;
using Aero.DataStructures.Graphs;
using Bogus;
using AutoFixture;
using Humanizer;

namespace Aero.DataStructures.Tests;

public class DirectedAcyclicGraphTests
{
    private readonly Faker _faker = new();
    private readonly Fixture _fixture = new();

    #region Vertex Tests

    [Fact]
    public void AddVertex_ShouldIncreaseCount()
    {
        var dag = new DirectedAcyclicGraph<string>();
        var vertex = _faker.Hacker.Noun();

        dag.AddVertex(vertex);

        dag.VertexCount.Should().Be(1);
    }

    [Fact]
    public void AddVertex_ShouldReturnTrue_WhenNew()
    {
        var dag = new DirectedAcyclicGraph<int>();
        var vertex = _fixture.Create<int>();

        var result = dag.AddVertex(vertex);

        result.Should().BeTrue();
    }

    [Fact]
    public void AddVertex_ShouldReturnFalse_WhenExists()
    {
        var dag = new DirectedAcyclicGraph<string>();
        dag.AddVertex("existing");

        var result = dag.AddVertex("existing");

        result.Should().BeFalse();
    }

    #endregion

    #region Edge Tests

    [Fact]
    public void AddEdge_ShouldAddSuccessfully_WhenNoCycle()
    {
        var dag = new DirectedAcyclicGraph<string>();
        dag.AddVertex("A");
        dag.AddVertex("B");

        var act = () => dag.AddEdge("A", "B");

        act.Should().NotThrow();
        dag.EdgeCount.Should().Be(1);
    }

    [Fact]
    public void AddEdge_ShouldThrow_WhenWouldCreateCycle()
    {
        var dag = new DirectedAcyclicGraph<string>();
        dag.AddEdge("A", "B");
        dag.AddEdge("B", "C");

        var act = () => dag.AddEdge("C", "A");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*cycle*");
    }

    [Fact]
    public void AddEdge_ShouldAutoAddVertices()
    {
        var dag = new DirectedAcyclicGraph<int>();
        var v1 = _fixture.Create<int>();
        var v2 = _fixture.Create<int>();

        dag.AddEdge(v1, v2);

        dag.ContainsVertex(v1).Should().BeTrue();
        dag.ContainsVertex(v2).Should().BeTrue();
    }

    [Fact]
    public void TryAddEdge_ShouldReturnTrue_WhenNoCycle()
    {
        var dag = new DirectedAcyclicGraph<string>();
        
        var result = dag.TryAddEdge("A", "B");

        result.Should().BeTrue();
    }

    [Fact]
    public void TryAddEdge_ShouldReturnFalse_WhenWouldCreateCycle()
    {
        var dag = new DirectedAcyclicGraph<string>();
        dag.AddEdge("A", "B");
        dag.AddEdge("B", "C");

        var result = dag.TryAddEdge("C", "A");

        result.Should().BeFalse();
    }

    #endregion

    #region WouldCreateCycle Tests

    [Fact]
    public void WouldCreateCycle_ShouldReturnTrue_WhenPathExists()
    {
        var dag = new DirectedAcyclicGraph<string>();
        dag.AddEdge("A", "B");
        dag.AddEdge("B", "C");

        dag.WouldCreateCycle("C", "A").Should().BeTrue();
    }

    [Fact]
    public void WouldCreateCycle_ShouldReturnFalse_WhenNoPath()
    {
        var dag = new DirectedAcyclicGraph<string>();
        dag.AddVertex("A");
        dag.AddVertex("B");

        dag.WouldCreateCycle("A", "B").Should().BeFalse();
    }

    #endregion

    #region Reachability Tests

    [Fact]
    public void CanReach_ShouldReturnTrue_WhenPathExists()
    {
        var dag = new DirectedAcyclicGraph<string>();
        dag.AddEdge("A", "B");
        dag.AddEdge("B", "C");

        dag.CanReach("A", "C").Should().BeTrue();
    }

    [Fact]
    public void CanReach_ShouldReturnFalse_WhenNoPath()
    {
        var dag = new DirectedAcyclicGraph<string>();
        dag.AddVertex("A");
        dag.AddVertex("B");

        dag.CanReach("A", "B").Should().BeFalse();
    }

    [Fact]
    public void CanReach_ShouldReturnTrue_ForSameVertex()
    {
        var dag = new DirectedAcyclicGraph<string>();
        dag.AddVertex("A");

        dag.CanReach("A", "A").Should().BeTrue();
    }

    #endregion

    #region Topological Sort Tests

    [Fact]
    public void TopologicalSort_ShouldReturnAllVertices()
    {
        var dag = new DirectedAcyclicGraph<string>();
        dag.AddEdge("A", "B");
        dag.AddEdge("B", "C");

        var result = dag.TopologicalSort();

        result.Should().HaveCount(3);
        result.Should().Contain(new[] { "A", "B", "C" });
    }

    [Fact]
    public void TopologicalSort_ShouldRespectDependencies()
    {
        var dag = new DirectedAcyclicGraph<string>();
        dag.AddEdge("compile", "test");
        dag.AddEdge("test", "deploy");

        var result = dag.TopologicalSort();

        result.IndexOf("compile").Should().BeLessThan(result.IndexOf("test"));
        result.IndexOf("test").Should().BeLessThan(result.IndexOf("deploy"));
    }

    [Fact]
    public void TopologicalSort_ShouldHandleMultipleSources()
    {
        var dag = new DirectedAcyclicGraph<string>();
        dag.AddEdge("A", "C");
        dag.AddEdge("B", "C");

        var result = dag.TopologicalSort();

        result.IndexOf("A").Should().BeLessThan(result.IndexOf("C"));
        result.IndexOf("B").Should().BeLessThan(result.IndexOf("C"));
    }

    #endregion

    #region All Topological Sorts Tests

    [Fact]
    public void GetAllTopologicalSorts_ShouldReturnMultipleOrders()
    {
        var dag = new DirectedAcyclicGraph<string>();
        dag.AddVertex("A");
        dag.AddVertex("B");
        dag.AddVertex("C");

        var allSorts = dag.GetAllTopologicalSorts().ToList();

        allSorts.Should().HaveCount(6);
    }

    #endregion

    #region Sources and Sinks Tests

    [Fact]
    public void GetSources_ShouldReturnVerticesWithNoIncoming()
    {
        var dag = new DirectedAcyclicGraph<string>();
        dag.AddEdge("root", "child1");
        dag.AddEdge("root", "child2");

        var sources = dag.GetSources().ToList();

        sources.Should().ContainSingle("root");
    }

    [Fact]
    public void GetSinks_ShouldReturnVerticesWithNoOutgoing()
    {
        var dag = new DirectedAcyclicGraph<string>();
        dag.AddEdge("parent", "leaf1");
        dag.AddEdge("parent", "leaf2");

        var sinks = dag.GetSinks().ToList();

        sinks.Should().Contain(new[] { "leaf1", "leaf2" });
    }

    #endregion

    #region Longest Path Tests

    [Fact]
    public void GetLongestPathLengths_ShouldComputeCorrectly()
    {
        var dag = new DirectedAcyclicGraph<string>();
        dag.AddEdge("A", "B");
        dag.AddEdge("B", "C");
        dag.AddEdge("A", "D");
        dag.AddEdge("D", "C");

        var lengths = dag.GetLongestPathLengths();

        lengths["A"].Should().Be(0);
        lengths["B"].Should().Be(1);
        lengths["D"].Should().Be(1);
        lengths["C"].Should().Be(2);
    }

    [Fact]
    public void GetLongestPath_ShouldReturnCorrectPath()
    {
        var dag = new DirectedAcyclicGraph<string>();
        dag.AddEdge("A", "B");
        dag.AddEdge("B", "C");
        dag.AddEdge("A", "C");

        var path = dag.GetLongestPath();

        path.Should().ContainInOrder("A", "B", "C");
    }

    #endregion

    #region Ancestor/Descendant Tests

    [Fact]
    public void GetAncestors_ShouldReturnAllPredecessors()
    {
        var dag = new DirectedAcyclicGraph<string>();
        dag.AddEdge("A", "B");
        dag.AddEdge("B", "C");
        dag.AddEdge("X", "C");

        var ancestors = dag.GetAncestors("C");

        ancestors.Should().Contain(new[] { "A", "B", "X" });
    }

    [Fact]
    public void GetDescendants_ShouldReturnAllSuccessors()
    {
        var dag = new DirectedAcyclicGraph<string>();
        dag.AddEdge("A", "B");
        dag.AddEdge("B", "C");
        dag.AddEdge("A", "D");

        var descendants = dag.GetDescendants("A");

        descendants.Should().Contain(new[] { "B", "C", "D" });
    }

    #endregion

    #region LCA Tests

    [Fact]
    public void GetLowestCommonAncestors_ShouldFindCorrectLca()
    {
        var dag = new DirectedAcyclicGraph<string>();
        dag.AddEdge("root", "left");
        dag.AddEdge("root", "right");
        dag.AddEdge("left", "target");
        dag.AddEdge("right", "target");

        var lcas = dag.GetLowestCommonAncestors("left", "right");

        lcas.Should().ContainSingle("root");
    }

    #endregion

    #region Transitive Closure Tests

    [Fact]
    public void GetTransitiveClosure_ShouldAddIndirectEdges()
    {
        var dag = new DirectedAcyclicGraph<string>();
        dag.AddEdge("A", "B");
        dag.AddEdge("B", "C");

        var closure = dag.GetTransitiveClosure();

        closure.ContainsEdge("A", "C").Should().BeTrue();
    }

    #endregion

    #region Remove Tests

    [Fact]
    public void RemoveVertex_ShouldRemoveFromDag()
    {
        var dag = new DirectedAcyclicGraph<string>();
        dag.AddVertex("remove");
        dag.AddVertex("keep");

        dag.RemoveVertex("remove");

        dag.ContainsVertex("remove").Should().BeFalse();
        dag.VertexCount.Should().Be(1);
    }

    [Fact]
    public void RemoveEdge_ShouldAllowPreviousCycle()
    {
        var dag = new DirectedAcyclicGraph<string>();
        dag.AddEdge("A", "B");
        dag.AddEdge("B", "C");

        dag.RemoveEdge("B", "C");
        
        var act = () => dag.AddEdge("C", "A");
        act.Should().NotThrow();
    }

    #endregion
}
