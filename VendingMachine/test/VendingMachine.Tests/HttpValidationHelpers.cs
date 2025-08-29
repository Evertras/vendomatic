using Amazon.Lambda.APIGatewayEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using System.Net;
using System.Text.Json;

namespace VendingMachine.Tests;

internal class HttpValidationHelpers
{
    internal static T GetResponseIsOK<T>(APIGatewayHttpApiV2ProxyResponse res)
    {
        return GetResponseIs<T>(res, HttpStatusCode.OK);
    }

    internal static T GetResponseIs<T>(APIGatewayHttpApiV2ProxyResponse res, HttpStatusCode expectedStatus)
    {
        res.StatusCode.Should().Be((int)expectedStatus);
        res.Body.Should().NotBeNull();
        res.Headers.Should().ContainKey("Content-Type");
        res.Headers["Content-Type"].Should().Be("application/json");
        var resObj = JsonSerializer.Deserialize<T>(res.Body!);
        resObj.Should().NotBeNull();
        return resObj;
    }
}
