using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.DotNet.Maestro.Client;

namespace Microsoft.DotNet.Maestro.Tasks.Tests.Mocks
{
    internal class MaestroApiMock : IMaestroApi
    {
        public MaestroApiOptions Options { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public IAssets Assets => throw new NotImplementedException();

        public IBuilds Builds = new BuildsMock();

        public IBuildTime BuildTime => throw new NotImplementedException();

        public IChannels Channels => throw new NotImplementedException();

        public IDefaultChannels DefaultChannels => throw new NotImplementedException();

        public IGoal Goal => throw new NotImplementedException();

        public IRepository Repository => throw new NotImplementedException();

        public ISubscriptions Subscriptions => throw new NotImplementedException();
    }
}
