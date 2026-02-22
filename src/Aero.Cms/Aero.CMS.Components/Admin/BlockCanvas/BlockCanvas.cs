using Aero.CMS.Components.Admin.Draggable;

namespace Aero.CMS.Components.Admin.BlockCanvas;

public partial class BlockCanvas
{
    private readonly List<NestedModel> items =
    [
        new() { Data = "Item 1.1" },
        new NestedModel
        {
            Data = "Item 1.2", Children =
            [
                new() { Data = "Item 1.2-1" },
                new()
                {
                    Data = "Item 1.2-2", Children =
                    [
                        new() { Data = "Item 1.2-2.1" },
                        new() { Data = "Item 1.2-2.2" },
                        new() { Data = "Item 1.2-2.3" },
                        new() { Data = "Item 1.2-2.4" }
                    ]
                },

                new() { Data = "Item 1.2.3" },
                new() { Data = "Item 1.2.4" }

            ]
        },

        new NestedModel { Data = "Item 1.3" },
        new NestedModel
        {
            Data = "Item 1.4", Children =
            [
                new() { Data = "Item 1.4.1" },
                new() { Data = "Item 1.4.2" },
                new() { Data = "Item 1.4.3" },
                new() { Data = "Item 1.4.4" }
            ]
        },

        new NestedModel { Data = "Item 1.5" }

    ];
}