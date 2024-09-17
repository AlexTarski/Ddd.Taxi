using Ddd.Taxi.Domain;
using System.Reflection;
using System.Text;

namespace Ddd.Taxi.Infrastructure;

/// <summary>
/// Базовый класс для всех Value типов.
/// </summary>
public class ValueType<T>
{
    private readonly List<PropertyInfo> properties;

    public ValueType()
    {
        this.properties = this.GetType()
                                     .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                     .OrderBy(p => p.Name)
                                     .ToList();
    }

    public override bool Equals(object obj)
    {
        if (obj is null || obj.GetType() != GetType())
            return false;
        if (ReferenceEquals(this, obj))
            return true;

        return properties.All(property =>
        {
            var thisValue = property.GetValue(this, null);
            var objectValue = property.GetValue(obj, null);
            return thisValue?.Equals(objectValue) ?? objectValue is null;
        });
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return properties
                .Select(property => property.GetValue(this, null)?.GetHashCode() ?? 0)
                .Aggregate(0, (hash, propertyHash) => (hash * 1236594) ^ propertyHash);
        }
    }

    public bool Equals(PersonName name) => Equals((object)name);

    public override string ToString()
    {
        var result = new StringBuilder($"{GetType().Name}(");
        for (int i = 0; i < properties.Count; i++)
        {
            var property = properties[i];
            result.AppendFormat("{0}: {1}", property.Name, property.GetValue(this, null));
            if (i < properties.Count - 1)
                result.Append("; ");
        }
        result.Append(")");
        return result.ToString();
    }
}
