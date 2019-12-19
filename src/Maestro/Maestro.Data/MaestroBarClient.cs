// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.DarcLib;
using Microsoft.DotNet.Maestro.Client.Models;
using Microsoft.EntityFrameworkCore;

namespace Maestro.Data
{
    /// <summary>
    ///     A bar client interface for use by DarcLib which talks directly
    ///     to the database for diamond dependency resolution.  Only a few features are required.
    /// </summary>
    internal class MaestroBarClient : IBarClient
    {
        private readonly BuildAssetRegistryContext _context;

        public MaestroBarClient(BuildAssetRegistryContext context)
        {
            _context = context;
        }

        #region Unneeded APIs

        public Task AddDefaultChannelAsync(string repository, string branch, string channel)
        {
            throw new NotImplementedException();
        }

        public Task<Channel> CreateChannelAsync(string name, string classification)
        {
            throw new NotImplementedException();
        }

        public Task<Subscription> CreateSubscriptionAsync(string channelName, string sourceRepo, string targetRepo, string targetBranch,
            string updateFrequency, bool batchable, List<MergePolicy> mergePolicies)
        {
            throw new NotImplementedException();
        }

        public Task<Channel> DeleteChannelAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task DeleteDefaultChannelAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task UpdateDefaultChannelAsync(int id, string repository = null, string branch = null, string channel = null, bool? enabled = null)
        {
            throw new NotImplementedException();
        }

        public Task<Subscription> DeleteSubscriptionAsync(Guid subscriptionId)
        {
            throw new NotImplementedException();
        }

        public Task<Channel> GetChannelAsync(string channel)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Channel>> GetChannelsAsync(string classification = null)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<DefaultChannel>> GetDefaultChannelsAsync(string repository = null, string branch = null, string channel = null)
        {
            throw new NotImplementedException();
        }

        public Task<Subscription> GetSubscriptionAsync(Guid subscriptionId)
        {
            throw new NotImplementedException();
        }

        public Task<Subscription> TriggerSubscriptionAsync(Guid subscriptionId)
        {
            throw new NotImplementedException();
        }

        public Task<Subscription> UpdateSubscriptionAsync(Guid subscriptionId, SubscriptionUpdate subscription)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<MergePolicy>> GetRepositoryMergePoliciesAsync(string repoUri, string branch)
        {
            throw new NotImplementedException();
        }

        public Task AssignBuildToChannel(int buildId, int channelId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<RepositoryBranch>> GetRepositoriesAsync(string repoUri = null, string branch = null)
        {
            throw new NotImplementedException();
        }

        public Task SetRepositoryMergePoliciesAsync(string repoUri, string branch, List<MergePolicy> mergePolicies)
        {
            throw new NotImplementedException();
        }

        #endregion

        /// <summary>
        ///     Get a set of subscriptions based on input filters.
        /// </summary>
        /// <param name="sourceRepo">Filter by the source repository of the subscription.</param>
        /// <param name="targetRepo">Filter by the target repository of the subscription.</param>
        /// <param name="channelId">Filter by the source channel id of the subscription.</param>
        /// <returns>Set of subscription.</returns>
        public async Task<IEnumerable<Subscription>> GetSubscriptionsAsync(string sourceRepo = null, string targetRepo = null, int? channelId = null)
        {
            IQueryable<Maestro.Data.Models.Subscription> subscriptions = _context.Subscriptions;

            // This isn't directly used, but if it's explicitly loaded it fills in the Channel objects in the subscriptions
            List<Maestro.Data.Models.Channel> channels = _context.Channels.ToList();

            if (sourceRepo != null)
            {
                subscriptions = subscriptions.Where(s => s.SourceRepository == sourceRepo);
            }
            if (targetRepo != null)
            {
                subscriptions = subscriptions.Where(s => s.TargetRepository == targetRepo);
            }
            if (channelId != null)
            {
                subscriptions = subscriptions.Where(s => s.ChannelId == channelId);
            }

            return subscriptions.Select(sub => ModelTranslators.DataToClientModel_Subscription(sub, null));
        }

        /// <summary>
        ///     Get a list of builds for the given repo uri and commit.
        /// </summary>
        /// <param name="repoUri">Repository uri</param>
        /// <param name="commit">Commit</param>
        /// <returns>Build with specific Id</returns>
        /// <remarks>This only implements the narrow needs of the dependency graph
        /// builder in context of coherency.  For example channels are not included./remarks>
        public async Task<Build> GetBuildAsync(int buildId)
        {
            Maestro.Data.Models.Build build = await _context.Builds.Where(b => b.Id == buildId)
                .Include(b => b.Assets)
                .FirstOrDefaultAsync();

            if (build == null)
            {
                throw new DarcException($"Could not find a build with id '{buildId}'");
            }

            return ModelTranslators.DataToClientModel_Build(build);
        }

        public Task<Build> UpdateBuildAsync(int buildId, BuildUpdate buildUpdate)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Get a list of builds for the given repo uri and commit.
        /// </summary>
        /// <param name="repoUri">Repository uri</param>
        /// <param name="commit">Commit</param>
        /// <returns>List of builds</returns>
        public async Task<IEnumerable<Build>> GetBuildsAsync(string repoUri, string commit)
        {
            List<Maestro.Data.Models.Build> builds = await _context.Builds.Where(b =>
                (repoUri == b.AzureDevOpsRepository || repoUri == b.GitHubRepository) && (commit == b.Commit))
                .Include(b => b.Assets)
                .OrderByDescending(b => b.DateProduced)
                .ToListAsync();

            return builds.Select(b => ModelTranslators.DataToClientModel_Build(b));
        }

        /// <summary>
        ///     Retrieve the latest build of a repository on a specific channel.
        /// </summary>
        /// <param name="repoUri">URI of repository to obtain a build for.</param>
        /// <param name="channelId">Channel the build was applied to.</param>
        /// <returns>Latest build of <paramref name="repoUri"/> on channel <paramref name="channelId"/>,
        /// or null if there is no latest.</returns>
        /// <remarks>The build's assets are returned</remarks>
        public async Task<Build> GetLatestBuildAsync(string repoUri, int channelId)
        {
            Data.Models.Build build = await _context.Builds.Where(b =>
            (repoUri == b.AzureDevOpsRepository || repoUri == b.GitHubRepository))
            .Where(b => b.BuildChannels.Any(c => c.ChannelId == channelId))
            .Include(b => b.Assets)
            .OrderByDescending(b => b.DateProduced)
            .FirstOrDefaultAsync();

            return ModelTranslators.DataToClientModel_Build(build) ?? null;
        }

        public async Task<IEnumerable<Asset>> GetAssetsAsync(
            string name = null,
            string version = null,
            int? buildId = null,
            bool? nonShipping = null)
        {
            IQueryable<Maestro.Data.Models.Asset> assets = _context.Assets;
            if (name != null)
            {
                assets = assets.Where(a => a.Name == name);
            }
            if (version != null)
            {
                assets = assets.Where(a => a.Version == version);
            }
            if (buildId != null)
            {
                assets = assets.Where(a => a.BuildId == buildId);
            }
            if (nonShipping != null)
            {
                assets = assets.Where(a => a.NonShipping == nonShipping);
            }

            var assetList = await assets.Include(a => a.Locations)
                .OrderByDescending(a => a.BuildId)
                .ToListAsync();

            return assetList.Select(a => ModelTranslators.DataToClientModel_Asset(a));
        }
    }
}
