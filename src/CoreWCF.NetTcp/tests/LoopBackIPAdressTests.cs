﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using CoreWCF.Configuration;
using Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace CoreWCF.NetTcp.Tests
{
    public class LoopbackIPAdressTests
    {
        private readonly ITestOutputHelper _output;

        public LoopbackIPAdressTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void SettingIPAdressProperty()
        {
            string testString = new string('a', 3000);
            int port = 11808;
            string expectedBaseAddress = $"net.tcp://{IPAddress.Loopback}:{port}";
            IWebHost host = ServiceHelper.CreateWebHostBuilder<Startup>(_output, IPAddress.Loopback, port).Build();
            using (host)
            {
                host.Start();
                Assert.Equal(expectedBaseAddress, host.GetNetTcpAddressInUse());
                var netTcpBinding = ClientHelper.GetBufferedModeBinding();
                var factory = new System.ServiceModel.ChannelFactory<ClientContract.ITestService>(netTcpBinding,
                    new System.ServiceModel.EndpointAddress(new Uri($"{expectedBaseAddress}/EchoService.svc")));
                var channel = factory.CreateChannel();
                
                var result = channel.EchoString(testString);
                Assert.Equal(testString, result);
            }
        }

        internal class Startup
        {
            public void ConfigureServices(IServiceCollection services)
            {
                services.AddServiceModelServices();
            }

            public void Configure(IApplicationBuilder app)
            {
                app.UseServiceModel(builder =>
                {
                    builder.AddService<Services.TestService>();
                    builder.AddServiceEndpoint<Services.TestService, ServiceContract.ITestService>(new NetTcpBinding(SecurityMode.None), "/EchoService.svc");
                });
            }
        }
    }
}
