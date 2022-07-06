using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Windows;

namespace ShowMeTheTemplate
{
	internal class TemplatedElementInfo
	{
		Type elementType;
		public Type ElementType
		{
			get { return elementType; }
		}

		IEnumerable<PropertyInfo> templatedProperties;
		public IEnumerable<PropertyInfo> TemplateProperties
		{
			get { return templatedProperties; }
		}

		public TemplatedElementInfo(Type elementType, IEnumerable<PropertyInfo> templatedProperties)
		{
			this.elementType = elementType;
			this.templatedProperties = templatedProperties;
		}

		public static IEnumerable<TemplatedElementInfo> GetTemplatedElements(Assembly assem)
		{
			Type frameworkTemplateType = typeof(FrameworkTemplate);

			foreach (Type type in assem.GetTypes())
			{
				if (type.IsAbstract) { continue; }
				if (type.ContainsGenericParameters) { continue; }
				if (type.GetConstructor(new Type[] { }) == null) { continue; }

				List<PropertyInfo> templatedProperties = new List<PropertyInfo>();
				foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
				{
					if (frameworkTemplateType.IsAssignableFrom(prop.PropertyType))
					{
						templatedProperties.Add(prop);
					}
				}

				if (templatedProperties.Count == 0) { continue; }

				yield return new TemplatedElementInfo(type, templatedProperties);
			}
		}
	}

}
