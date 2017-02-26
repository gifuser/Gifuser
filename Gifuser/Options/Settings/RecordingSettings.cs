using System.ComponentModel;
using System.IO;

namespace Gifuser.Options.Settings
{
	public class RecordingSettings : INotifyPropertyChanged
	{
		private readonly string _defaultFolder;
		private string _folder;
		private int _delay;

		public RecordingSettings(MainSettings settings)
		{
			_defaultFolder = Path.Combine(settings.DefaultFolder, "recordings");
			ResetDelay();
			ResetFolder();
		}

		public void ResetFolder()
		{
			Folder = _defaultFolder;
		}

		public void ResetDelay()
		{
			Delay = 100;
		}

		public string Folder
		{
			get
			{
				return _folder;
			}

			set
			{
				if (value != _folder)
				{
					_folder = value;
					OnPropertyChanged("Folder");
				}
			}
		}

		public int Delay
		{
			get
			{
				return _delay;
			}

			set
			{
				if (value != _delay)
				{
					_delay = value;
					OnPropertyChanged("Delay");
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected virtual void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}
	}
}
