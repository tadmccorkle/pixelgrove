// Copyright (c) 2026 by Tad McCorkle
// Licensed under the MIT license.

namespace Csm.PixelGrove.Auth;

internal record GoogleOAuthConfig(string ClientId, string ClientSecret, string CallbackPath);
