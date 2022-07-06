using System;
using System.Text;
using System.Linq;
using System.Windows.Media;
using System.Windows;
using System.Collections;
using System.Collections.Generic;

namespace ShowMeTheTemplate
{
	public static class ParentOfTypeExtensions
	{
		public static T ParentOfType<T>(this DependencyObject element) where T : DependencyObject
		{
			if (element == null)
			{
				return null;
			}
			return element.GetParents().OfType<T>().FirstOrDefault();
		}

		public static IEnumerable<DependencyObject> GetParents(this DependencyObject element)
		{
			if (element == null)
			{
				throw new ArgumentNullException("element");
			}
			while (true)
			{
				DependencyObject parent;
				element = (parent = element.GetParent());
				if (parent != null)
				{
					yield return element;
					continue;
				}
				break;
			}
		}

		private static DependencyObject GetParent(this DependencyObject element)
		{
			DependencyObject dependencyObject = null;
			try
			{
				dependencyObject = VisualTreeHelper.GetParent(element);
			}
			catch (InvalidOperationException)
			{
				dependencyObject = null;
			}
			if (dependencyObject == null)
			{
				if (element is FrameworkElement frameworkElement)
				{
					dependencyObject = frameworkElement.Parent;
				}
				if (element is FrameworkContentElement frameworkContentElement)
				{
					dependencyObject = frameworkContentElement.Parent;
				}
			}
			return dependencyObject;
		}
	}
}
