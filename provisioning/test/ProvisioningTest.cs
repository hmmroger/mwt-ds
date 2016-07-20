﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using Microsoft.Rest;
using Microsoft.Azure.Management.ResourceManager;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Azure;
using Microsoft.Azure.Management.ResourceManager.Models;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Configuration;
using System.Net;
using System.Text;
using System.Web;
using Microsoft.Research.MultiWorldTesting.Contract;
using System.Threading;

namespace Microsoft.Research.DecisionServiceTest
{
    [TestClass]
    public partial class ProvisioningTest
    {
        [TestMethod]
        public void ProvisionOnlyTest()
        {
            var deployment = new ProvisioningUtil().Deploy();

            Assert.IsNotNull(deployment.ManagementCenterUrl);
            Assert.IsNotNull(deployment.ManagementPassword);
            Assert.IsNotNull(deployment.OnlineTrainerUrl);
            Assert.IsNotNull(deployment.OnlineTrainerToken);
            Assert.IsNotNull(deployment.WebServiceToken);
            Assert.IsNotNull(deployment.SettingsUrl);
        }

        [TestMethod]
        [TestCategory("End to End")]
        [Priority(2)]
        public async Task AllEndToEndTests()
        {
            var deployment = new ProvisioningUtil().Deploy();
            
            await new SimplePolicyTestClass().SimplePolicyTest(deployment);

            new EndToEndOnlineTrainerTest().E2ERankerStochasticRewards(deployment);
        }
    }
}
