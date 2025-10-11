using Electra.Persistence;
using Electra.Web.BlogEngine.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Electra.Web.BlogEngine.Data;

public class BlogRepository(BlogDbContext context, ILogger<GenericEntityFrameworkRepository<BlogEntry>> log)
    : GenericEntityFrameworkRepository<BlogEntry>(context, log);