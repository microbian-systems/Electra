using Bunit;
using Electra.MerakiUI.Tables;
using Xunit;

namespace Electra.MerakiUI.Tests.Tables;

public class TableTests : BunitContext
{
    [Fact]
    public void SimpleTable_ShouldRenderCorrectStructure()
    {
        var headers = new[] { "Name", "Status", "Role" };
        var cut = Render<SimpleTable>(parameters => parameters
            .Add(p => p.Headers, headers)
            .AddChildContent("<tr><td>John Doe</td><td>Active</td><td>Admin</td></tr>")
        );

        // Verify headers
        var ths = cut.FindAll("th");
        Assert.Equal(3, ths.Count);
        Assert.Contains("Name", ths[0].TextContent);
        
        // Verify rows
        Assert.Contains("John Doe", cut.Markup);
    }
}
