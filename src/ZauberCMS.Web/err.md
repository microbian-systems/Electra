System.Reflection.AmbiguousMatchException: Ambiguous match found for 'Electra.Core.Identity.ElectraRole`1[System.String] System.String Id'.
   at System.RuntimeType.GetPropertyImpl(String name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers)
   at System.Type.GetProperty(String name, BindingFlags bindingAttr)
   at Raven.Client.Documents.Conventions.DocumentConventions.GetIdentityProperty(Type type)
   at Raven.Client.Documents.Identity.GenerateEntityIdOnTheClient.TrySetIdentityInternal(Object entity, String id, Boolean isProjection)
   at Raven.Client.Documents.Identity.GenerateEntityIdOnTheClient.TrySetIdentity(Object entity, String id)
   at Raven.Client.Documents.Session.InMemoryDocumentSessionOperations.StoreInternal(Object entity, String changeVector, String id, ConcurrencyCheckMode forceConcurrencyCheck)
   at Raven.Client.Documents.Session.InMemoryDocumentSessionOperations.StoreAsyncInternal(Object entity, String changeVector, String id, ConcurrencyCheckMode forceConcurrencyCheck, CancellationToken token)
   at Electra.Persistence.RavenDB.Identity.RoleStore`2.CreateAsync(TRole role, CancellationToken cancellationToken) in D:\proj\microbians\microbians.io\Electra\src\Electra.Persistence.RavenDB\Identity\RavenDbRoleStore.cs:line 130
   at Microsoft.AspNetCore.Identity.RoleManager`1.CreateAsync(TRole role)
   at Electra.Auth.Seeder.Initialize(IServiceProvider serviceProvider, IConfiguration configuration) in D:\proj\microbians\microbians.io\Electra\src\Electra.Auth\Seeder.cs:line 31
   at ZauberCMS.Core.ZauberSetup.AddZauberCms[T](WebApplication app) in D:\proj\microbians\microbians.io\Electra\src\ZauberCMS.Core\ZauberSetup.cs:line 336
