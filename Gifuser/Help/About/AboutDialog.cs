using System;
namespace Gifuser.Help.About
{
	public partial class AboutDialog : Gtk.Dialog
	{
		public AboutDialog()
		{
			this.Build();

			versionLabel.Text = GetType().Assembly.GetName().Version.ToString();
		}
	}
}
