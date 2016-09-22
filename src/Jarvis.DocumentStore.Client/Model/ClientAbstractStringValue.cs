using System;
using System.ComponentModel;
using System.Globalization;

namespace Jarvis.DocumentStore.Client.Model
{

    public abstract class ClientAbstractStringValue : IEquatable<ClientAbstractStringValue>
    {
        string _value;

        public bool Equals(ClientAbstractStringValue other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            // NB!
            if (other.GetType() != this.GetType()) return false;

            return string.Equals(Value, other.Value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ClientAbstractStringValue)obj);
        }

        public override int GetHashCode()
        {
            return (Value != null ? Value.GetHashCode() : 0);
        }

        public static bool operator ==(ClientAbstractStringValue left, ClientAbstractStringValue right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ClientAbstractStringValue left, ClientAbstractStringValue right)
        {
            return !Equals(left, right);
        }

        private string Value
        {
            get { return _value; }
            set { _value = Normalize(value); }
        }

        protected virtual string Normalize(string value)
        {
            return value;
        }

        public static implicit operator string(ClientAbstractStringValue id)
        {
            return id.Value;
        }

        protected ClientAbstractStringValue(string value)
        {
            Value = value;
        }

        public bool IsValid()
        {
            return !String.IsNullOrWhiteSpace(this._value);
        }

        public override string ToString()
        {
            return _value;
        }
    }


    public abstract class LowercaseClientAbstractStringValue : ClientAbstractStringValue
    {
        protected LowercaseClientAbstractStringValue(string value)
            : base(value)
        {
        }

        protected override string Normalize(string value)
        {
            return value == null ? null : value.ToLowerInvariant();
        }
    }

    public class StringValueTypeConverter<T> : TypeConverter where T : ClientAbstractStringValue
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
            {
                return true;
            }
            return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            return Activator.CreateInstance(typeof(T), new object[] { (string)value });
        }
    }
}