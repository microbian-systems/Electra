using Raven.Client.Documents;
using Raven.Client.Documents.Session;

namespace ZauberCMS.Core.Data.Interfaces;

public interface ISeedData
{
    void Initialise(IDocumentSession store);
}