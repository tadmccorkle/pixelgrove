// Copyright (c) 2026 by Tad McCorkle
// Licensed under the MIT license.

using Microsoft.AspNetCore.Routing;

namespace Csm.PixelGrove.Endpoints;

internal interface IEndpoint
{
    static abstract void Map(IEndpointRouteBuilder app);
}

internal static class EndpointExtensions
{
    public static IEndpointRouteBuilder Map<TEndpoint>(this IEndpointRouteBuilder app)
        where TEndpoint : IEndpoint
    {
        TEndpoint.Map(app);
        return app;
    }
}
