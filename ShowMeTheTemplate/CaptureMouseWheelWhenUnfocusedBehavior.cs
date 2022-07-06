using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows;
using System.Windows.Controls;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Windows.Interop;
using System.Windows.Media;
using System.Xml.Linq;
using System.Globalization;
using System.Windows.Forms;
using System.Drawing;

namespace ShowMeTheTemplate
{
	public static class CaptureMouseWheelWhenUnfocusedBehavior
	{
		public static ScrollViewer NotifyScrollViewer = null;

		private static readonly HashSet<WindowsFormsHost> TrackedHosts = new HashSet<WindowsFormsHost>();

		private static readonly System.Windows.Forms.IMessageFilter MessageFilter = new MouseWheelMessageFilter();

		private sealed class MouseWheelMessageFilter : System.Windows.Forms.IMessageFilter
		{
			public const int WM_MOUSEWHEEL = 0x020A;

			[DllImport("user32.dll"), SuppressUnmanagedCodeSecurity]
			private static extern IntPtr WindowFromPoint(System.Drawing.Point point);

			[DllImport("user32.dll")]
			private static extern bool ScreenToClient(IntPtr hWnd, ref System.Drawing.Point lpPoint);

			[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
			private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

			private static System.Drawing.Point LocationFromLParam(IntPtr lParam)
			{
				int x = (int)((((long)lParam) >> 0) & 0xffff);
				int y = (int)((((long)lParam) >> 16) & 0xffff);
				return new System.Drawing.Point(x, y);
			}

			private static bool ConsiderRedirect(WindowsFormsHost host)
			{
				var control = host.Child;
				return control != null &&
					  !control.IsDisposed &&
					   control.IsHandleCreated &&
					   control.Visible &&
					  !control.Focused;
			}

			private static int DeltaFromWParam(IntPtr wParam)
			{
				return (short)((((long)wParam) >> 16) & 0xffff);
			}

			public bool PreFilterMessage(ref System.Windows.Forms.Message m)
			{
				if (m.Msg == WM_MOUSEWHEEL)
				{
					System.Drawing.Point location = new System.Drawing.Point(m.LParam.ToInt32());

					foreach (WindowsFormsHost host in TrackedHosts)
					{
						if (!ConsiderRedirect(host)) continue;

						DependencyObject parent = host.Parent;
						System.Windows.Window window = parent.GetParents().OfType<System.Windows.Window>().FirstOrDefault();
						if (window == null)
						{
							return false;
						}

						IntPtr windowHandle = new WindowInteropHelper(window).Handle;
						IntPtr mainHwnd = WindowFromPoint(location);
						if (mainHwnd == windowHandle)
						{
							return false;
						}

						bool undercursor_IEServer = isIEServerWindow(mainHwnd);
						if (undercursor_IEServer)
						{
							var delta = DeltaFromWParam(m.WParam);

							{
								// Raise event for WPF control
								var args = new MouseWheelEventArgs(InputManager.Current.PrimaryMouseDevice, Environment.TickCount, delta);
								args.RoutedEvent = WindowsFormsHost.MouseWheelEvent;
								NotifyScrollViewer.RaiseEvent(args);
							}

							return true;
						}

					}
				}
				return false;
			}

			private static bool isIEServerWindow(IntPtr hWnd)
			{
				// Pre-allocate 256 characters, since this is the maximum class name length.
				StringBuilder className = new StringBuilder(256);
				//Get the window class name
				int nRet = GetClassName(hWnd, className, className.Capacity);
				if (nRet != 0)
				{
					return (string.Compare(className.ToString(), "Internet Explorer_Server", true, CultureInfo.InvariantCulture) == 0);
				}
				else
				{
					return false;
				}
			}

		}
		public static bool GetIsEnabled(DependencyObject obj)
		{
			return (bool)obj.GetValue(IsEnabledProperty);
		}

		public static void SetIsEnabled(DependencyObject obj, bool value)
		{
			obj.SetValue(IsEnabledProperty, value);
		}

		public static readonly DependencyProperty IsEnabledProperty =
			DependencyProperty.RegisterAttached(
				"IsEnabled",
				typeof(bool),
				typeof(CaptureMouseWheelWhenUnfocusedBehavior),
				new PropertyMetadata(false, OnIsEnabledChanged));

		private static void OnIsEnabledChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			var wfh = o as WindowsFormsHost;
			if (wfh == null) return;

			if ((bool)e.NewValue)
			{
				wfh.Loaded += OnHostLoaded;
				wfh.Unloaded += OnHostUnloaded;
				if (wfh.IsLoaded && TrackedHosts.Add(wfh))
				{
					if (TrackedHosts.Count == 1)
					{
						System.Windows.Forms.Application.AddMessageFilter(MessageFilter);
					}
				}
			}
			else
			{
				wfh.Loaded -= OnHostLoaded;
				wfh.Unloaded -= OnHostUnloaded;
				if (TrackedHosts.Remove(wfh))
				{
					if (TrackedHosts.Count == 0)
					{
						System.Windows.Forms.Application.RemoveMessageFilter(MessageFilter);
					}
				}
			}
		}

		private static void OnHostLoaded(object sender, EventArgs e)
		{
			var wfh = (WindowsFormsHost)sender;
			if (TrackedHosts.Add(wfh))
			{
				if (TrackedHosts.Count == 1)
				{
					System.Windows.Forms.Application.AddMessageFilter(MessageFilter);
				}
			}
		}

		private static void OnHostUnloaded(object sender, EventArgs e)
		{
			var wfh = (WindowsFormsHost)sender;
			if (TrackedHosts.Remove(wfh))
			{
				if (TrackedHosts.Count == 0)
				{
					System.Windows.Forms.Application.RemoveMessageFilter(MessageFilter);
				}
			}
		}
	}
}
