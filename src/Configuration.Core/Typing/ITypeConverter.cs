using System;

namespace Configuration.Core.Typing
{
    public interface ITypeConverter
    {
        object Convert(string value, Type targetType);
        bool TryConvert(string value, Type targetType, out object? result);
    }
}


