using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Aero.Cms.UI;

public static class CmsAdminExtensions
{
    public static void AddCmsAdmin(this IServiceCollection servcies, IConfiguration config, IWebHostEnvironment env)
    {
        // Register admin services, controllers, etc. here
        
    }
}
