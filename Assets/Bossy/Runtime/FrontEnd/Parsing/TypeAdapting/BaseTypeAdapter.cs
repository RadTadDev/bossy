namespace Bossy.FrontEnd.Parsing
{
    public abstract class BaseTypeAdapter<T> : ITypeAdapter
    {
        public TypeAdapterResult TryConvert(TokenStream cursor, out object output)
        {
            var result = TryConvertToType(cursor, out var converted);

            output = result.Success ? converted : null;

            return result;
        }

        protected abstract TypeAdapterResult TryConvertToType(TokenStream cursor, out T output);
        
        public override bool Equals(object obj) => obj?.GetType() == GetType();
        public override int GetHashCode() => GetType().GetHashCode();
    }
}