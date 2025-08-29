using Amazon.Lambda.APIGatewayEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using System.Text.Json;
using System.Net;
using Xunit;
using VendingMachine.Dtos;
using NSubstitute;

namespace VendingMachine.Tests;

public class ServerTest
{
    [Fact]
    public async Task HandleRequestReturns404OnUnknownRoute()
    {
        var mockRepository = Substitute.For<IRepository>();
        var server = new Server(mockRepository);
        var req = new APIGatewayHttpApiV2ProxyRequest
        {
            RouteKey = "GET /not/a/real/route"
        };
        var res = await server.HandleRequest(req);

        var resObj = HttpValidationHelpers.GetResponseIs<GenericErrorResponse>(res, HttpStatusCode.NotFound);

        resObj.Error.Should().NotBeNull();
        resObj!.Error.Should().Be("Route GET /not/a/real/route Not Found");
    }
}
