using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace RedisTribute.Configuration
{
    class ConfigurationParser<T>
    {
        readonly static PropertyInfo[] _properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead && p.CanWrite).ToArray();
        readonly IDictionary<string, (Type type, Action<T, object> action)> _actions;

        public ConfigurationParser()
        {
            _actions = _properties.ToDictionary(x => x.Name, x => (x.PropertyType, (Action<T, object>)((i, v) => x.SetValue(i, v))), StringComparer.OrdinalIgnoreCase);
        }

        public void Register<TValue>(string name, Action<T, TValue> action)
        {
            var type = typeof(TValue);

            _actions[name] = (type, (i, x) => action(i, (TValue)x));
        }

        public void RegisterAlias(string alias, string propertyName)
        {
            var propAct = _actions[propertyName];

            _actions[alias] = propAct;
        }

        public void RegisterDefault<TValue>(Action<T, TValue> action)
        {
            Register("*", action);
        }

        public void Parse(string configString, T instance)
        {
            foreach (var item in configString.Split(',', ';'))
            {
                var i = item.IndexOf('=');
                var kv = i > -1 ? new[] { item.Substring(0, i), item.Substring(i + 1, item.Length - (i + 1)) } : new string[] { item };

                if (kv.Length == 1)
                {
                    if (string.IsNullOrEmpty(kv[0]))
                    {
                        continue;
                    }

                    if (_actions.TryGetValue("*", out var act))
                    {
                        act.action(instance, ParseValue(act.type, kv[0]));
                    }
                }
                else
                {
                    if (_actions.TryGetValue(kv[0], out var act))
                    {
                        act.action(instance, ParseValue(act.type, kv[1]));
                    }
                }
            }
        }

        object ParseValue(Type type, string value)
        {
            if (type == typeof(string))
            {
                return value;
            }

            if (type == typeof(Encoding))
            {
                return Encoding.GetEncoding(value);
            }

            if (type == typeof(bool))
            {
                return bool.Parse(value.ToLower());
            }

            if (type == typeof(TimeSpan))
            {
                return TimeSpan.Parse(value);
            }

            if (type == typeof(int))
            {
                return int.Parse(value);
            }

            if (type.IsEnum)
            {
                return Enum.Parse(type, value);
            }

            throw new NotSupportedException(type.Name);
        }
    }
}