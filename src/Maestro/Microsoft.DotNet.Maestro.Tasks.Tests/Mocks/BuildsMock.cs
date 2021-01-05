using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using FluentAssertions.Common;
using Microsoft.DotNet.Maestro.Client;
using Microsoft.DotNet.Maestro.Client.Models;

namespace Microsoft.DotNet.Maestro.Tasks.Tests.Mocks
{
    public class BuildsMock : IBuilds
    {
        public Task<Client.Models.Build> CreateAsync(BuildData body, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Client.Models.Build> GetBuildAsync(int id, CancellationToken cancellationToken = default)
        {
            // Uses predefined buildId values to return a build for testing
            Client.Models.Build defaultBuild = new Client.Models.Build
                (
                    id: id, 
                    dateProduced:DateTime.Now.ToDateTimeOffset(), 
                    staleness: 0, 
                    released: false, 
                    stable: true, 
                    commit: "commitString", 
                    channels: ImmutableList<Channel>.Empty, 
                    assets: ImmutableList<Asset>.Empty, 
                    dependencies: ImmutableList<BuildRef>.Empty, 
                    incoherencies: ImmutableList<BuildIncoherence>.Empty
                );

            return id switch
            {
                404 => null,// build not found
                _ => new Task<Client.Models.Build>(() => defaultBuild),
            };
        }

        public Task<BuildGraph> GetBuildGraphAsync(int id, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Client.Models.Build> GetLatestAsync(string buildNumber = null, int? channelId = null, string commit = null, bool? loadCollections = null, DateTimeOffset? notAfter = null, DateTimeOffset? notBefore = null, string repository = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public AsyncPageable<Client.Models.Build> ListBuildsAsync(string azdoAccount = null, int? azdoBuildId = null, string azdoProject = null, string buildNumber = null, int? channelId = null, string commit = null, bool? loadCollections = null, DateTimeOffset? notAfter = null, DateTimeOffset? notBefore = null, string repository = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Page<Client.Models.Build>> ListBuildsPageAsync(string azdoAccount = null, int? azdoBuildId = null, string azdoProject = null, string buildNumber = null, int? channelId = null, string commit = null, bool? loadCollections = null, DateTimeOffset? notAfter = null, DateTimeOffset? notBefore = null, int? page = null, int? perPage = null, string repository = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Client.Models.Build> UpdateAsync(BuildUpdate body, int buildId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
