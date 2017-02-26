using System;
namespace Gifuser.Options.Settings
{
	public partial class SettingsDialog : Gtk.Dialog
	{
		public SettingsDialog(RecordingSettings settings)
		{
			this.Build();

			if (settings == null)
			{
				throw new ArgumentNullException("settings");
			}

			_settings = settings;

			folderEntry.Text = _settings.Folder;
			spinbutton1.Value = _settings.Delay;

			_settings.PropertyChanged += _settings_PropertyChanged;
			folderEntry.FocusOutEvent += FolderEntry_FocusOutEvent;
			spinbutton1.FocusOutEvent += Spinbutton1_FocusOutEvent;
		}

		void FolderEntry_FocusOutEvent(object o, Gtk.FocusOutEventArgs args)
		{
			_settings.Folder = folderEntry.Text;
		}

		void Spinbutton1_FocusOutEvent(object o, Gtk.FocusOutEventArgs args)
		{
			_settings.Delay = (int)(spinbutton1.Value);
		}

		private readonly RecordingSettings _settings;

		void _settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case "Folder":
					{
						folderEntry.Text = _settings.Folder;
					}
					break;
				case "Delay":
					{
						spinbutton1.Value = _settings.Delay;
					}
					break;
				default:
					break;
			}
		}

		protected override void OnClose()
		{
			_settings.PropertyChanged -= _settings_PropertyChanged;
			folderEntry.FocusOutEvent -= FolderEntry_FocusOutEvent;
			spinbutton1.FocusOutEvent -= Spinbutton1_FocusOutEvent;
			
			base.OnClose();
		}

		void OnResetFolderClicked(object sender, EventArgs e)
		{
			_settings.ResetFolder();
		}

		void OnResetDelayClicked(object sender, EventArgs e)
		{
			_settings.ResetDelay();
		}
	}
}
