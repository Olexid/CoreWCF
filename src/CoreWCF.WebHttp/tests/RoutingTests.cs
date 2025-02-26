﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Threading.Tasks;
using CoreWCF.Configuration;
using CoreWCF.Description;
using Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace CoreWCF.WebHttp.Tests
{
    public class RoutingTests
    {
        private readonly ITestOutputHelper _output;

        public RoutingTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task NoParam()
        {
            IWebHost host = ServiceHelper.CreateWebHostBuilder<Startup>(_output).Build();
            using (host)
            {
                host.Start();

                (HttpStatusCode statusCode, string _) = await HttpHelpers.GetAsync("api/noparam");

                Assert.Equal(HttpStatusCode.OK, statusCode);
            }
        }

        [Fact]
        public async Task PathParam()
        {
            IWebHost host = ServiceHelper.CreateWebHostBuilder<Startup>(_output).Build();
            using (host)
            {
                host.Start();

                (HttpStatusCode statusCode, string content) = await HttpHelpers.GetAsync("api/pathparam/test");

                Assert.Equal(HttpStatusCode.OK, statusCode);
                Assert.Equal("\"test\"", content);
            }
        }

        [Fact]
        public async Task QueryParam()
        {
            IWebHost host = ServiceHelper.CreateWebHostBuilder<Startup>(_output).Build();
            using (host)
            {
                host.Start();

                (HttpStatusCode statusCode, string content) = await HttpHelpers.GetAsync("api/queryparam?param=test");

                Assert.Equal(HttpStatusCode.OK, statusCode);
                Assert.Equal("\"test\"", content);
            }
        }

        [Fact]
        public async Task Wildcard()
        {
            IWebHost host = ServiceHelper.CreateWebHostBuilder<Startup>(_output).Build();
            using (host)
            {
                host.Start();

                (HttpStatusCode statusCode, string content) = await HttpHelpers.GetAsync("api/wildcard");

                Assert.Equal(HttpStatusCode.OK, statusCode);
                Assert.Equal("\"wildcard\"", content);
            }
        }

        [Fact]
        public async Task CompoundPathSegments()
        {
            IWebHost host = ServiceHelper.CreateWebHostBuilder<Startup>(_output).Build();
            using (host)
            {
                host.Start();

                (HttpStatusCode statusCode, string content) = await HttpHelpers.GetAsync("api/compound/test.jpg");

                Assert.Equal(HttpStatusCode.OK, statusCode);
                Assert.Equal("\"test.jpg\"", content);
            }
        }

        [Fact]
        public async Task NamedWildcard()
        {
            IWebHost host = ServiceHelper.CreateWebHostBuilder<Startup>(_output).Build();
            using (host)
            {
                host.Start();

                (HttpStatusCode statusCode, string content) = await HttpHelpers.GetAsync("api/named/one/two");

                Assert.Equal(HttpStatusCode.OK, statusCode);
                Assert.Equal("\"one\\/two\"", content);
            }
        }

        [Fact]
        public async Task DefaultValue()
        {
            IWebHost host = ServiceHelper.CreateWebHostBuilder<Startup>(_output).Build();
            using (host)
            {
                host.Start();

                (HttpStatusCode statusCode, string content) = await HttpHelpers.GetAsync("api/default/");

                Assert.Equal(HttpStatusCode.OK, statusCode);
                Assert.Equal("\"default\"", content);
            }
        }

        internal class Startup
        {
            public void ConfigureServices(IServiceCollection services)
            {
                services.AddServiceModelWebServices();
            }

            public void Configure(IApplicationBuilder app)
            {
                app.UseServiceModel(builder =>
                {
                    builder.AddService<Services.RoutingService>();
                    builder.AddServiceWebEndpoint<Services.RoutingService, ServiceContract.IRoutingService>("api");
                });
            }
        }
    }
}
