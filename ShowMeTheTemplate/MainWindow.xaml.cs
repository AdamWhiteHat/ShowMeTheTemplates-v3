using System;
using System.IO;
using System.Xml;
using System.Linq;
using System.Windows;
using System.Reflection;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Collections.Generic;
using System.Windows.Forms.Integration;
using System.Windows.Interop;

namespace ShowMeTheTemplate
{
	public partial class MainWindow : Window
	{
		Assembly presentationFrameworkAssembly = Assembly.LoadWithPartialName("PresentationFramework");
		Dictionary<Type, object> typeElementMap = new Dictionary<Type, object>();
		List<string> filesToDeleteOnExit = new List<string>();

		public MainWindow()
		{
			InitializeComponent();
			bookLink.Click += bookLink_Click;
			Closing += MainWindow_Closing;
			themes.SelectionChanged += themes_SelectionChanged;
			DataContext = new List<TemplatedElementInfo>(TemplatedElementInfo.GetTemplatedElements(presentationFrameworkAssembly));
			themes.SelectedIndex = 0;

			CaptureMouseWheelWhenUnfocusedBehavior.NotifyScrollViewer = mainScrollViewer;
		}

		private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			filesToDeleteOnExit = filesToDeleteOnExit.Distinct().ToList();
			foreach (string file in filesToDeleteOnExit)
			{
				File.Delete(file);
			}
		}

		void bookLink_Click(object sender, RoutedEventArgs e)
		{
			System.Diagnostics.Process.Start("http://sellsbrothers.com/writing/wpfbook/");
		}

		void themes_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ComboBox cb = (ComboBox)sender;
			Uri themeUri = new Uri((string)((ComboBoxItem)cb.SelectedItem).Tag, UriKind.Relative);
			ResourceDictionary themeResources = (ResourceDictionary)Application.LoadComponent(themeUri);
			mainItemsControl.Resources = themeResources;
		}

		void ElementHolder_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			ContentControl elementHolder = (ContentControl)sender;
			TemplatedElementInfo elementInfo = (TemplatedElementInfo)elementHolder.DataContext;

			// Get element (cached)
			object element = GetElement(elementInfo.ElementType);

			// Create and show the element (some have to be shown to give up their templates...)
			CreateSampleControl(elementHolder, element);

			// Fill the element (don't seem to need to do this, but makes it easier to see on the screen...)
			PopulateExampleControl(element);
		}

		/// <summary>
		/// Get the element from a cache based on the type
		/// Used to avoid recreating a type twice and used so that when the WebBrowser needs to get the templates for each property, it knows where to look
		/// </summary>
		object GetElement(Type elementType)
		{
			if (!typeElementMap.ContainsKey(elementType))
			{
				typeElementMap[elementType] = presentationFrameworkAssembly.CreateInstance(elementType.FullName);
			}

			return typeElementMap[elementType];
		}

		/// <summary>
		/// Handles the Loaded event of the WindowsFormsHost control.
		/// Tells the WebBrowser to navigate to the property's template.
		/// </summary>
		void WindowsFormsHost_Loaded(object sender, RoutedEventArgs e)
		{
			WindowsFormsHost host = (WindowsFormsHost)sender;
			PropertyInfo prop = (PropertyInfo)host.DataContext;

			//CaptureMouseWheelWhenUnfocusedBehavior.SetIsEnabled(host, true);

			Type elementType = prop.ReflectedType;
			object element = GetElement(elementType);
			FrameworkTemplate template = (FrameworkTemplate)prop.GetValue(element, null);

			System.Windows.Forms.WebBrowser browser = (System.Windows.Forms.WebBrowser)host.Child;
			ShowTemplate(browser, template);

			CaptureMouseWheelWhenUnfocusedBehavior.SetIsEnabled(host, true);
		}

		private void WindowsFormsHost_Unloaded(object sender, RoutedEventArgs e)
		{
			WindowsFormsHost host = (WindowsFormsHost)sender;
			CaptureMouseWheelWhenUnfocusedBehavior.SetIsEnabled(host, false);
		}

		/// <summary>
		/// Creates an instance of the selected template type (if it hasn't already), 
		/// serializes that as XAML, saves that it to a temporary file, and sets 
		/// the WebBrowser's Document property to the XAML for viewing.
		/// </summary>
		private void ShowTemplate(System.Windows.Forms.WebBrowser browser, FrameworkTemplate template)
		{
			if (template == null)
			{
				browser.DocumentText = "(no template)";
				return;
			}

			// Writes the template to a file so that the browser knows to show it as XML
			string filename = Path.GetTempFileName();
			File.Delete(filename);
			filename = Path.ChangeExtension(filename, "xml");

			// pretty print the XAML (for View Source)
			using (XmlTextWriter writer = new XmlTextWriter(filename, System.Text.Encoding.UTF8))
			{
				writer.Formatting = Formatting.Indented;
				XamlWriter.Save(template, writer);
			}

			// Show the template
			browser.Navigate(new Uri(@"file:///" + filename));
		}

		/// <summary>
		/// Queues temporary files to be deleted at shutdown (otherwise, View Source doesn't work)
		/// </summary>
		private void WebBrowser_Navigated(object sender, System.Windows.Forms.WebBrowserNavigatedEventArgs e)
		{
			if (e.Url.IsFile) { filesToDeleteOnExit.Add(e.Url.LocalPath); }
		}

		/// <summary>
		/// Resizes the WebBrowser control Height to whatever height the it needs to be to display the full XAML
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.Forms.WebBrowserDocumentCompletedEventArgs"/> instance containing the event data.</param>
		private void WebBrowser_DocumentCompleted(object sender, System.Windows.Forms.WebBrowserDocumentCompletedEventArgs e)
		{
			System.Windows.Forms.WebBrowser browser = (System.Windows.Forms.WebBrowser)sender;
			browser.Size = browser.Document.Body.ScrollRectangle.Size;
		}

		/// <summary>
		/// Instantiates and displays an example of the control the template targets.
		/// </summary>
		private void CreateSampleControl(ContentControl elementHolder, object element)
		{
			elementHolder.Content = null;

			Type elementType = element.GetType();
			if ((elementType == typeof(ToolTip)) ||
				(elementType == typeof(Window)))
			{
				// can't be set as a child, but don't need to be shown, so do nothing
			}
			else if (elementType == typeof(NavigationWindow))
			{
				NavigationWindow wnd = (NavigationWindow)element;
				wnd.WindowState = WindowState.Minimized;
				wnd.ShowInTaskbar = false;
				wnd.Show(); // needs to be shown once to "hydrate" the template
				wnd.Hide();
			}
			else if (typeof(ContextMenu).IsAssignableFrom(elementType))
			{
				elementHolder.ContextMenu = (ContextMenu)element;
			}
			else if (typeof(Page).IsAssignableFrom(elementType))
			{
				Frame frame = new Frame();
				frame.Content = element;
				elementHolder.Content = frame;
			}
			else
			{
				elementHolder.Content = element;
			}
		}

		/// <summary>
		/// Populates the example control's properties with sample data so it renders normally (e.g. Text, Content, Headers, Items).
		/// </summary>
		void PopulateExampleControl(object element)
		{
			if (element is ContentControl)
			{
				((ContentControl)element).Content = "(some content)";

				if (element is HeaderedContentControl)
				{
					((HeaderedContentControl)element).Header = "(a header)";
				}
			}
			else if (element is ItemsControl)
			{
				((ItemsControl)element).Items.Add("(an item)");
			}
			else if (element is PasswordBox)
			{
				((PasswordBox)element).Password = "(a password)";
			}
			else if (element is System.Windows.Controls.Primitives.TextBoxBase)
			{
				((System.Windows.Controls.Primitives.TextBoxBase)element).AppendText("(some text)");
			}
			else if (element is Page)
			{
				((Page)element).Content = "(some content)";
			}
		}

		#region MouseWheel event routing to ScrollViewer for seamless mouse wheel scrolling

		/// <summary>
		/// A handler for MouseWheel events that routes the event to the ScrollViewer, which results in a scrolling its children.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="MouseWheelEventArgs"/> instance containing the event data.</param>
		private void RouteMouseWheelEventToScrollViewer(object sender, MouseWheelEventArgs e)
		{
			if (sender == null || e == null)
			{
				return;
			}

			FrameworkElement senderElement = (FrameworkElement)sender;
			if (senderElement == null)
			{
				return;
			}

			var mouseWheelEventArgs = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
			{
				RoutedEvent = ScrollViewer.MouseWheelEvent,
				Source = sender
			};

			mainScrollViewer.RaiseEvent(mouseWheelEventArgs);

			e.Handled = true;
		}

		/// <summary>
		///  System.Windows.Forms controls MouseWheel events have a different signature. Events are routed to ScrollViewer.
		/// </summary>
		private void WebBrowser_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			if (sender == null || e == null)
			{
				return;
			}

			System.Windows.Forms.WebBrowser browser = (System.Windows.Forms.WebBrowser)sender;

			if (browser == null)
			{
				return;
			}

			System.Windows.Forms.MouseButtons button = e.Button;
			int delta = e.Delta;

			MouseDevice mouse = InputManager.Current.PrimaryMouseDevice;
			var mouseWheelEventArgs = new MouseWheelEventArgs(mouse, Environment.TickCount, e.Delta)
			{
				RoutedEvent = ScrollViewer.MouseWheelEvent,
				Source = sender
			};

			mainScrollViewer.RaiseEvent(mouseWheelEventArgs);
		}

		private void WebBrowser_MouseEnter(object sender, EventArgs e)
		{
			(sender as System.Windows.Forms.WebBrowser).Focus();
		}

		#endregion

		private void itemTemplate_windowsFormsHost_MouseEnter(object sender, MouseEventArgs e)
		{
			mainScrollViewer.Focus();
		}
	}
}