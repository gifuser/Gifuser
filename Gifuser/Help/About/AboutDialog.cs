using System;
using System.Reflection;

namespace Gifuser.Help.About
{
	public partial class AboutDialog : Gtk.Dialog
	{
		public AboutDialog()
		{
			this.Build();

			string pngFile = System.IO.Path.Combine(
				AppDomain.CurrentDomain.BaseDirectory,
				"gifuser-icons",
				"gifuser-128.png"
			);
			if (System.IO.File.Exists(pngFile))
			{
				image1.Pixbuf = new Gdk.Pixbuf(pngFile);
			}
			versionLabel.Text = GetType().Assembly.GetName().Version.ToString();
		}
	}
}
