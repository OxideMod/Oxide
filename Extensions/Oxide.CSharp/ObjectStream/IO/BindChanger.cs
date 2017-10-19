using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace ObjectStream.IO
{
    public class BindChanger : SerializationBinder
    {
        public override Type BindToType(string assemblyName, string typeName)
        {
            /*if (typeName.Contains(".")) typeName = typeName.Substring(typeName.LastIndexOf(".", StringComparison.Ordinal) + 1);
            if (typeName.Contains("+")) typeName = typeName.Substring(typeName.LastIndexOf("+", StringComparison.Ordinal) + 1);

            var type = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .FirstOrDefault(x => x.Name.Equals(typeName));

            return type != null ? Type.GetType(string.Format("{0}, {1}", type.FullName, Assembly.GetExecutingAssembly().FullName)) : null;*/
            return Type.GetType(string.Format("{0}, {1}", typeName, Assembly.GetExecutingAssembly().FullName));
        }
    }
}
