// Copyright (c) 2026 by Tad McCorkle
// Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Csm.PixelGrove.Auth;
using Csm.PixelGrove.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Csm.PixelGrove.Endpoints;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
internal class Users : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users/{id}", async (
            HttpContext context,
            AppDbContext db,
            string id) =>
        {
            if (id == "me")
            {
                if (!(context.User.Identity?.IsAuthenticated ?? false))
                {
                    return Results.Unauthorized();
                }

                if (!context.User.TryGetAppUserId(out var userId))
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        detail: "Authenticated user has invalid or missing id claim.");
                }

                var user = await db.Users.FindAsync(userId);
                return user is not null
                    ? Results.Ok(user)
                    : Results.NotFound();
            }

            if (Guid.TryParse(id, out _))
            {
                return Results.StatusCode(StatusCodes.Status501NotImplemented);
            }

            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: "Invalid user id.");
        });
    }
}
