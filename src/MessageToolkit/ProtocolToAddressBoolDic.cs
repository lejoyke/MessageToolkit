using System.Reflection;

namespace MessageToolkit
{
    public class ProtocolToAddressBoolDic
    {
        public static Dictionary<int, bool> FindProperties<T>(T protocol)
        {
            var dict = new Dictionary<int, bool>();
            Type type = typeof(T);

            // 遍历所有属性
            foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (prop.PropertyType == typeof(bool))
                {
                    var addressAttr = prop.GetCustomAttribute(typeof(AddressAttribute), false) as AddressAttribute;
                    if (addressAttr != null)
                    {
                        bool value = (bool)prop.GetValue(protocol);
                        dict[addressAttr.Address] = value;
                    }
                }
            }

            return dict;
        }

        public static Dictionary<int,bool> FindFiels<T>(T protocol)
        {
            var dict = new Dictionary<int, bool>();
            Type type = typeof(T);
            // 遍历所有字段
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                if (field.FieldType == typeof(bool))
                {
                    var addressAttr = field.GetCustomAttribute(typeof(AddressAttribute), false) as AddressAttribute;
                    if (addressAttr != null)
                    {
                        bool value = (bool)field.GetValue(protocol);
                        dict[addressAttr.Address] = value;
                    }
                }
            }
            return dict;
        }
    }
}
