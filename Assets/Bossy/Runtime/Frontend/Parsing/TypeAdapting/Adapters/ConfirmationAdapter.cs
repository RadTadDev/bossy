using System;
using Bossy.Command;
using Bossy.Utils;

namespace Bossy.Frontend.Parsing
{
    public class ConfirmationAdapter : BaseTypeAdapter<Confirmation>
    {
        protected override TypeAdapterResult TryConvertToType(TokenStream stream, TypeAdapterRegistry registry, out Confirmation output)
        {
            var valid = string.Join(", ", Enum.GetNames(typeof(Confirmation)));

            if (stream.TryConsume(out var token))
            {
                if (Enum.TryParse(token, ignoreCase: true, out output) && Enum.IsDefined(typeof(Confirmation), output))
                {
                    return TypeAdapterResult.Pass();
                }

                if (token.ToLower() is not ("yes" or "y" or "no" or "n"))
                {
                    return TypeAdapterResult.Fail($"\"{token}\" is not a valid {typeof(Confirmation).GetFriendlyName()}. Valid values: {valid}");
                }
                
                output = token.StartsWith("y") ? Confirmation.Confirm : Confirmation.Deny;
                return TypeAdapterResult.Pass();
            }

            output = default;
            return TypeAdapterResult.Fail("Missing token!");
        }
    }
}