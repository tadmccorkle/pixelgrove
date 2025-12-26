// Copyright (c) 2026 by Tad McCorkle
// Licensed under the MIT license.

using Csm.PixelGrove;
using Csm.PixelGrove.Auth;
using Csm.PixelGrove.Data;
using Csm.PixelGrove.Endpoints;
using Csm.PixelGrove.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);
var isDev = builder.Environment.IsDevelopment();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("App Database"));

builder.Services.AddOpenApi();
builder.Services.AddAuth(builder.Configuration);

if (isDev)
{
    builder.AddDevServerProxy();
}

var app = builder.Build();

if (isDev)
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseHttpsRedirection();
app.UseAntiforgery();
app.UseMiddleware<CsrfRequestTokenCookie>();

if (isDev)
{
    app.UseDevServerProxy();
}
else
{
    app.UseFileServer();
    app.MapFallbackToFile("index.html");
}

app
    .Map<Auth>()
    .Map<Users>();

app.Run();
