// Copyright (c) 2026 by Tad McCorkle
// Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Csm.PixelGrove;

internal enum ErrorType
{
    None,
    Failure,
    Validation,
    Conflict,
    NotFound,
}

internal record Error(ErrorType Type, string Code, string Description)
{
    public static Error None { get; } = new(ErrorType.None, string.Empty, string.Empty);
}

internal class Result
{
    protected Result(Error error)
    {
        this.Error = error;
    }

    public Error Error { get; }

    public bool IsFailure => this.Error != Error.None;

    public static Result Success() => new(Error.None);
    public static Result Failure(Error error) => new(error);

    public static implicit operator Result(Error error) => Failure(error);
}

internal class Result<T>
{
    public Result(T? value, Error error)
    {
        this.Value = value;
        this.Error = error;
    }

    public T? Value { get; }

    public Error Error { get; }

    [MemberNotNullWhen(false, nameof(Value))]
    public bool IsFailure => this.Error != Error.None;

    public static Result<T> Success(T value) => new(value, Error.None);
    public static Result<T> Failure(Error error) => new(default, error);

    public static implicit operator Result<T>(Error error) => Failure(error);
}
