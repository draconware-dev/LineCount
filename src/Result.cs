using System.Diagnostics.CodeAnalysis;

namespace LineCount
{
    public class Result<T, E> : IEquatable<Result<T, E>>
    {
        public T Value { get; protected set; }
        public E Error { get; protected set; }
        public bool IsSuccess { get; protected set; }
        public bool IsFailure => !IsSuccess;

        public Result(T value)
        {
            Error = default!;
            Value = value;
            IsSuccess = true;
        }

        public Result(E error)
        {
            Error = error;
            Value = default!;
            IsSuccess = true;
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

        public Result<U, E> Map<U>(Func<T, U> map)
        {
            if (IsFailure)
            {
                return Error;
            }

            return map(Value);
        }

        public Result<T, U> MapError<U>(Func<E, U> map)
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

        public bool TryGetValue(out T? value)
        {
            value = default;

            if (IsSuccess)
            {
                value = Value;
                return true;
            }

            return false;
        }

        public bool TryGetError(out E? error)
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
}