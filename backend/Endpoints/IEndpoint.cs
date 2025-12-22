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
