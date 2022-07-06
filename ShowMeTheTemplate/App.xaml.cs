using System;
using System.Windows;
using System.Data;
using System.Xml;
using System.Configuration;
using System.Reflection;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Diagnostics;

namespace ShowMeTheTemplate
{
	public partial class App : System.Windows.Application
	{
		public static IEnumerable<Type> GetTemplatePartTypes(Assembly assem)
		{
			foreach (Type type in assem.GetTypes())
			{
				if (!type.IsPublic) { continue; }
				if (type.GetCustomAttributes(typeof(TemplatePartAttribute), false).Length == 0) { continue; }
				yield return type;
			}
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			List<Type> templatePartTypes = new List<Type>(GetTemplatePartTypes(Assembly.LoadWithPartialName("PresentationFramework")));
			templatePartTypes.Sort(delegate (Type lhs, Type rhs) { return lhs.Name.CompareTo(rhs.Name); });

			foreach (Type type in templatePartTypes)
			{
				//Debug.WriteLine(string.Format("{0}: ", type.Name));
				foreach (TemplatePartAttribute attrib in type.GetCustomAttributes(typeof(TemplatePartAttribute), false))
				{
					Debug.WriteLine(string.Format("{0}, {1}, {2}", type.Name, attrib.Name, attrib.Type.Name));
				}
			}
		}
	}
}