using System.Diagnostics.CodeAnalysis;
using LineCount.Errors;

namespace LineCount;

public sealed class Result<T, E> : IEquatable<Result<T, E>> where T : notnull where E : IError
{
    public T? Value { get; }
    public E? Error { get; }

    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess { get; }

    [MemberNotNullWhen(true, nameof(Error))]
    [MemberNotNullWhen(false, nameof(Value))]
    public bool IsFailure => !IsSuccess;

    public Result(T value)
    {
        Error = default;
        Value = value;
        IsSuccess = true;
    }

    public Result(E error)
    {
        Error = error;
        Value = default;
        IsSuccess = false;
    }

    public bool Equals(Result<T, E>? other)
    {
        return this == other;
    }

    public override bool Equals(object? obj)
    {
        return obj is Result<T, E> result && Equals(result);
    }

    public static bool operator ==(Result<T, E>? left, Result<T, E>? right)
    {
        return left?.IsSuccess & right?.IsSuccess & left?.Value!.Equals(right!.Value) ?? false;
    }
    
    public static bool operator !=(Result<T, E>? left, Result<T, E>? right)
    {
        return !(left == right);
    }

    public static implicit operator Result<T, E>(T value)
    {
        return new Result<T, E>(value);
    }

    public static implicit operator Result<T, E>(E error)
    {
        return new Result<T, E>(error);
    }

    public static explicit operator T(Result<T, E> result)
    {
        return result.IsSuccess ? result.Value : throw new InvalidCastException(nameof(result.Value));
    }

    public static explicit operator E(Result<T, E> result)
    {
        return result.IsFailure ? result.Error : throw new InvalidCastException(nameof(result.Error));
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(IsSuccess, Value);
    }

    public void Match(Action<T> onSuccess, Action<E> onError)
    {
        if (IsSuccess)
        {
            onSuccess(Value);
            return;
        }

        onError(Error);
    }

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<E, TResult> onError)
    {
        if (IsSuccess)
        {
            return onSuccess(Value);
        }

        return onError(Error);
    }

    public Result<U, E> Map<U>(Func<T, U> map) where U : notnull
    {
        if (IsFailure)
        {
            return Error;
        }

        return map(Value);
    }

    public Result<T, U> MapError<U>(Func<E, U> map) where U : IError
    {
        if (IsSuccess)
        {
            return Value;
        }

        return map(Error);
    }

    public override string ToString()
    {
        return IsSuccess ? $"Success -> Value: {Value}" : $"Failure -> Error: {Error}";
    }

    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool TryGetValue([MaybeNullWhen(false)] out T value)
    {
        value = default;

        if (IsSuccess)
        {
            value = Value;
            return true;
        }

        return false;
    }

    [MemberNotNullWhen(true, nameof(Error))]
    [MemberNotNullWhen(false, nameof(Value))]
    public bool TryGetError([MaybeNullWhen(false)] out E error)
    {
        error = default;

        if (IsFailure)
        {
            error = Error;
            return true;
        }

        return false;
    }

    public static Result<T, E> Success(T value)
    {
        return new Result<T, E>(value);
    }

    public static Result<T, E> Failure(E error)
    {
        return new Result<T, E>(error);
    }
}