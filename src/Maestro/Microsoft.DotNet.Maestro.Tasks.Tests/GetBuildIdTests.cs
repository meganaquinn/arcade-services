// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using FluentAssertions;
using Microsoft.DotNet.Maestro.Client;
using Microsoft.DotNet.Maestro.Client.Models;
using Microsoft.DotNet.Maestro.Tasks.Proxies;
using Microsoft.DotNet.Maestro.Tasks.Tests.Mocks;
using Microsoft.DotNet.VersionTools.BuildManifest.Model;
using Moq;
using NUnit.Framework;

namespace Microsoft.DotNet.Maestro.Tasks.Tests
{
    [TestFixture]
    public class GetBuildIdTests
    {
        //    private static async Task<int?> GetBuildId(DependencyDetail dep, IMaestroApi client, Dictionary<int, Client.Models.Build> buildCache,
        // Dictionary<(string name, string version, string commit), int> assetCache, CancellationToken cancellationToken)

        private readonly string matchingAsset1Name = "MatchingAsset1";
        private readonly string matchingAsset2Name = "MatchingAsset2";
        private readonly string assetNotFound = "AssetNotFoundInBuild";
        private readonly string matchingVersion = "MatchingAssetVersion";
        private readonly string mismatchingVersion = "MismatchingAssetVersion";
        private readonly string matchingCommit = "MatchingCommit";
        private readonly string mismatchingCommit = "MismatchingCommit";
        private readonly int matchingId = 12345;
        private readonly int mismatchingId = 54321;

        [SetUp]
        public void GetBuildIdTestSetup()
        {
            //Need to pass in IMaestroApi client, which will call Builds.GetBuildAsync
            //A lot of the logic relies on object references being passed in, so it's important to do the object filling in this setup & not a class one
            IMaestroApi client = new MaestroApiMock();
            Dictionary<(string name, string version, string commit), int> assetCache 
                = new Dictionary<(string name, string version, string commit), int>();
            assetCache.Add((matchingAsset1Name, matchingVersion, matchingCommit), matchingId);
            assetCache.Add((matchingAsset2Name, matchingVersion, matchingCommit), matchingId);
            assetCache.Add((assetNotFound, mismatchingVersion, mismatchingCommit), mismatchingId);
        }

        [Test]
        public void GivenAllValuesArePresentAndValid()
        {
            //Golden path (given assets have a matching commit, buildId has a value, and build has assets in it aren’t in the given list)
        }

        [Test]
        public void GivenAssetsAreOnlyAssets()
        {
            //Given assets are the only assets for build
        }

        [Test]
        public void GivenAssetsWithoutMatchingCommit_ExpectNullReturn()
        {
            //Given assets don’t have a matching commit so no buildId is found(no exception, return null)
        }

        [Test]
        public void GivenAssetsMissingRequiredFields_ExpectNullRefException()
        {
            //Given assets are missing field values that are expected / used in logic
        }

        [Test]
        public void GivenNullEmptyArguments_ExceptArgNullException()
        {
            //Given null/empty arguments (if possible from caller)
        }
    }
}
