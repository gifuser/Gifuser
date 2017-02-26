using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using Gifuser.Options.Settings.Models;
using Newtonsoft.Json;

namespace Gifuser.Options.Settings
{
	public class MainSettings : INotifyPropertyChanged
	{
		private const string FILENAME = "config.json";
		private static readonly MainSettings _instance;

		static MainSettings()
		{
			_instance = LoadFromConfig();
		}

		public static MainSettings Instance
		{
			get
			{
				return _instance;
			}
		}
		
		private readonly string _defaultFolder;
		private RecordingSettings _recordings;
		private bool _uploadOnStop;
		private string _uploadServiceName;

		private MainSettings()
		{
			_defaultFolder = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
				"gifuser"
			);

			_recordings = new RecordingSettings(this);
			_uploadOnStop = true;
			_uploadServiceName = "imgur";
		}

		private static MainSettings LoadFromConfig()
		{
			MainSettings settings = new MainSettings();

			try
			{
				using (FileStream stream = File.OpenRead(Path.Combine(settings.DefaultFolder, FILENAME)))
				using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
				{
					JsonSerializer serializer = new JsonSerializer();
					MainSettingsModel model = (MainSettingsModel)(serializer.Deserialize(reader, typeof(MainSettingsModel)));

					settings.UploadOnStop = model.UploadOnStop;

					switch (model.UploadServiceName)
					{
						case "imgur":
						case "giphy":
						case "gfycat":
							{
								settings.UploadServiceName = model.UploadServiceName;
							}
							break;
						default:
							break;
					}

					if (model.Recordings != null)
					{
						double delayMs = model.Recordings.Delay.TotalMilliseconds;
						if (33.0 <= delayMs && delayMs <= ushort.MaxValue)
						{
							settings.Recordings.Delay = (int)(delayMs);
						}

						if (model.Recordings.Folder != null)
						{
							settings.Recordings.Folder = model.Recordings.Folder;
						}
					}
				}
			}
			catch
			{

			}

			return settings;
		}

		public void Save()
		{
			try
			{
				Directory.CreateDirectory(_defaultFolder);

				using (FileStream stream = File.Create(Path.Combine(_defaultFolder, FILENAME)))
				using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8))
				{
					JsonSerializer serializer = new JsonSerializer
					{
						Formatting = Formatting.Indented
					};

					serializer.Serialize(
						writer,
						new MainSettingsModel
						{
							UploadOnStop = UploadOnStop,
							UploadServiceName = UploadServiceName,
							Recordings = new RecordingModel
							{
								Delay = TimeSpan.FromMilliseconds(Recordings.Delay),
								Folder = Recordings.Folder
							}
						}
					);
				}
			}
			catch
			{

			}
		}

		public string DefaultFolder
		{
			get
			{
				return _defaultFolder;
			}
		}

		public RecordingSettings Recordings
		{
			get
			{
				return _recordings;
			}

			set
			{
				if (value != _recordings)
				{
					_recordings = value;
					OnPropertyChanged("Recordings");
				}
			}
		}

		public bool UploadOnStop
		{
			get
			{
				return _uploadOnStop;
			}

			set
			{
				if (value != _uploadOnStop)
				{
					_uploadOnStop = value;
					OnPropertyChanged("UploadOnStop");
				}
			}
		}

		public string UploadServiceName
		{
			get
			{
				return _uploadServiceName;
			}

			set
			{
				if (value != _uploadServiceName)
				{
					_uploadServiceName = value;
					OnPropertyChanged("UploadServiceName");
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
