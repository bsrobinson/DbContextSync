using System;
using PastelExtended;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace DbContextSync.Prompts
{
	public class IPrompt<T>
	{
        public string Question { get; set; }

        public Nullable<T> DefaultValue { get; set; } = default;
        public Nullable<T> AutoSelectValue { get; set; } = default;
        public bool SilentOnAuto { get; set; } = false;
        public bool SpaceBefore { get; set; } = false;

        public IPrompt(string question)
        {
            Question = question;
        }
    }

    //https://stackoverflow.com/a/72630290/404459
    public struct Nullable<T>
    {
        [MaybeNull]
        public static Nullable<T> Null => new Nullable<T>(false, default!);

        [MemberNotNullWhen(true, nameof(Value))]
        public bool HasValue { get; init; }
        public bool IsNull => !HasValue;

        private T? _value;
        [MaybeNull]
        public T Value
        {
            get => _value ?? throw new NullReferenceException();
            init => _value = value;
        }

        private Nullable(bool hasValue, T value)
        {
            HasValue = hasValue;
            _value = value;
        }

        public static implicit operator Nullable<T>(T? n) => n == null ? Null : new Nullable<T>(true, n);
        //public static implicit operator T?(Nullable<T> n) => n.Value;

        public static bool operator ==(Nullable<T> a, T? b) => a.Equals(b);
        public static bool operator !=(Nullable<T> a, T? b) => !a.Equals(b);

        public override bool Equals(object? obj)
        {
            if (typeof(object) == typeof(T))
            {
                return HasValue && obj != null ? Value.Equals(obj) : _value == null && obj == null;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return _value?.GetHashCode() ?? 0;
        }
    }
}

