using System;
using Gtk;
using System.IO;

using Gifuser;
using Gifuser.Core;
using Gifuser.Upload;
using Gifuser.Options.Settings;
using ImgurPlugin;
using GiphyPlugin;
using GfycatPlugin;
using System.Globalization;
using System.Text;

public partial class MainWindow : Gtk.Window
{
	private const string IMGUR_CLIENT_ID = "ab96e06949e54ad";
	private const string GIPHY_BETA_CLIENT_ID = "dc6zaTOxFJmzC";
	private const string GFYCAT_CLIENT_ID = "2_tZmNDO";
	private const string GFYCAT_CLIENT_SECRET = "BvPmA4C0HZgw4RKQhsdbwlSNAIe9RSoy3wyYl_C_MahBztcGntbYtD0Etap4-90Z";

	private readonly IScreenRecorder _screenRecorder;
	private readonly MainSettings _settings;
	private IFileTrackedUpload _uploader;
	private bool _releasing;

	public MainWindow() : base(Gtk.WindowType.Toplevel)
	{
		Build();

		CreateDesktopEntry();

		_settings = MainSettings.Instance;
		_settings.PropertyChanged += _settings_PropertyChanged;

		_screenRecorder = new GifRecorder();
		_screenRecorder.Completed += _screenRecorder_Completed;
		_screenRecorder.ScreenshotTaken += _screenRecorder_ScreenshotTaken;
		_screenRecorder.Delay = TimeSpan.FromMilliseconds(_settings.Recordings.Delay);

		_releasing = true;

		uploadOnStop.Active = _settings.UploadOnStop;

		RadioButton radioBtn = Array.Find(
			new RadioButton[] {
				imgur,
				giphy,
				gfycat
			},
			rBtn => rBtn.Name == _settings.UploadServiceName
		);
		radioBtn.Active = true;

		SetUploader(_settings.UploadServiceName);
	}

	private void CreateDesktopEntry()
	{
		if (Environment.OSVersion.Platform == PlatformID.Unix)
		{
			string desktopEntry = System.IO.Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
				".local",
				"share",
				"applications",
				"gifuser.desktop"
			);

			using (FileStream file = File.Create(desktopEntry))
			{
				Encoding encoding = new UTF8Encoding(false);
				
				using (StreamWriter writer = new StreamWriter(file, encoding))
				{
					string appDir = AppDomain.CurrentDomain.BaseDirectory;
					
					writer.WriteLine("[Desktop Entry]");
					writer.WriteLine("Encoding=UTF-8");
					writer.WriteLine("Type=Application");
					writer.WriteLine("Name=Gifuser");
					writer.WriteLine("Icon=" + System.IO.Path.Combine(appDir, "gifuser-icons", "gifuser-128.png"));
					writer.WriteLine("Path=" + appDir);
					writer.WriteLine("Exec=mono Gifuser.exe");
				}
			}
		}
	}

	protected void OnDeleteEvent(object sender, DeleteEventArgs a)
	{
		if (_screenRecorder.Recording)
		{
			MessageDialog dialog = new MessageDialog(
				this,
				DialogFlags.Modal,
				MessageType.Info,
				ButtonsType.Ok,
				"You must stop the recorder in order to exit"
			);
			dialog.Run();
			dialog.Destroy();
			a.RetVal = true;
		}
		else
		{
			_settings.Save();
			Application.Quit();
			a.RetVal = true;
		}
	}

	void _settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case "Recordings":
				{
					_screenRecorder.Delay = TimeSpan.FromMilliseconds(_settings.Recordings.Delay);
				}
				break;
			default:
				break;
		}
	}

	private static string GetFileSize(long size)
	{
		return ((double)(size) / (1024.0 * 1024.0)).ToString("F") + " MB";
	}

	private void _screenRecorder_Completed(object sender, ScreenRecordCompletedEventArgs e)
	{
		Gtk.Application.Invoke(delegate
		{
			FileInfo info = new FileInfo(e.FileName);
			
			elapsedLabel.Text = _screenRecorder.Elapsed.ToString("c");
			fileSizeLabel.Text = GetFileSize(info.Length);
			recordStopBtn.Label = "Record";

			if (uploadOnStop.Active)
			{
				switch (_uploader.CheckRequirementStatus(e.FileName))
				{
					case UploadRequirementStatus.Success:
						{
							UploadDialog dialog = new UploadDialog(_uploader, e.FileName);
							dialog.Run();
						}
						break;
					case UploadRequirementStatus.FileNotFound:
						{
							string msg = string.Format("The file \"{0}\" was not found", e.FileName);
							MessageDialog dialog = new MessageDialog(
								this,
								DialogFlags.Modal,
								MessageType.Info,
								ButtonsType.Ok,
								msg
							);
							dialog.Run();
							dialog.Destroy();
						}
						break;
					case UploadRequirementStatus.FileNull:
						{
							MessageDialog dialog = new MessageDialog(
								this,
								DialogFlags.Modal,
								MessageType.Info,
								ButtonsType.Ok,
								"File not set"
							);
							dialog.Run();
							dialog.Destroy();
						}
						break;
					case UploadRequirementStatus.FileTooLarge:
						{
							string msg = string.Format(
								"{0} service can upload files up to {1}, but the file {2} has a size of {3} which exceeds the service limit.",
								_uploader.Name,
								GetFileSize(_uploader.MaxFileSize),
								e.FileName,
								GetFileSize(info.Length)
							);
							MessageDialog dialog = new MessageDialog(
								this,
								DialogFlags.Modal,
								MessageType.Info,
								ButtonsType.Ok,
								msg
							);
							dialog.Run();
							dialog.Destroy();
						}
						break;
					default:
						{
							throw new Exception("Unknown requirement");
						}
				}
			}
			else
			{
				string msg = string.Format(
					"File location:{0}{0}{1}",
					Environment.NewLine,
					e.FileName
				);
				MessageDialog dialog = new MessageDialog(
					this,
					DialogFlags.Modal,
					MessageType.Info,
					ButtonsType.Ok,
					msg
				);
				dialog.Run();
				dialog.Destroy();
			}
		});
	}

	private void _screenRecorder_ScreenshotTaken(object sender, ScreenshotTakenEventArgs e)
	{
		Gtk.Application.Invoke(delegate
		{
			elapsedLabel.Text = _screenRecorder.Elapsed.ToString("c");
			fileSizeLabel.Text = GetFileSize(e.CurrentFileSize);
		});
	}

	protected void OnRecordStopBtnClicked(object sender, EventArgs e)
	{
		Gtk.Application.Invoke(delegate
		{
			if (_screenRecorder.Recording)
			{
				_screenRecorder.FinishAsync();
			}
			else
			{
				try
				{
					Directory.CreateDirectory(_settings.Recordings.Folder);
                    
					string fileName = System.IO.Path.Combine(
						_settings.Recordings.Folder,
						string.Format(
							"{0}{1}",
							Guid.NewGuid().ToString("N"),
							".gif"
						)
					);

					try
					{
						using (FileStream fs = File.Create(fileName))
						{

						}

						recordStopBtn.Label = "Stop";

						_screenRecorder.StartAsync(fileName, true);
					}
					catch
					{
						MessageDialog dialog = new MessageDialog(
							this,
							DialogFlags.Modal,
							MessageType.Error,
							ButtonsType.Ok,
							string.Format("Cannot create the file {0}", fileName)
						);

						dialog.Run();
						dialog.Destroy();
					}
				}
				catch
				{
					MessageDialog dialog = new MessageDialog(
						this,
						DialogFlags.Modal,
						MessageType.Error,
						ButtonsType.Ok,
						string.Format("Cannot create directory {0}", _settings.Recordings.Folder)
					);

					dialog.Run();
					dialog.Destroy();
				}
			}
		});
	}

	protected void OnRadioButtonToggled(object sender, EventArgs e)
	{
		if (!_releasing)
		{
			string name = (sender as RadioButton).Name;
			SetUploader(name);
		}

		_releasing = !_releasing;
	}

	private void SetUploader(string name)
	{
		switch (name)
		{
			case "imgur":
				{
					_uploader = new ImgurFileTrackedUpload(IMGUR_CLIENT_ID);
				}
				break;
			case "giphy":
				{
					_uploader = new GiphyFileTrackedUpload(GIPHY_BETA_CLIENT_ID);
				}
				break;
			case "gfycat":
				{
					_uploader = new GfycatFileTrackedUpload(GFYCAT_CLIENT_ID, GFYCAT_CLIENT_SECRET);
				}
				break;
			default:
				{
					throw new Exception("Unknown radio button option");
				}
		}

		_settings.UploadServiceName = name;
	}

	protected void OnUploadOnStopToggled(object sender, EventArgs e)
	{
		uploadOptions.Sensitive = uploadOnStop.Active;
		_settings.UploadOnStop = uploadOnStop.Active;
	}

	protected void OnSettingsActionActivated(object sender, EventArgs e)
	{
		RecordingSettings copy = new RecordingSettings(_settings);

		copy.Delay = _settings.Recordings.Delay;
		copy.Folder = _settings.Recordings.Folder;

		SettingsDialog settings = new SettingsDialog(copy);
		ResponseType r = (ResponseType)(settings.Run());

		switch (r)
		{
			case ResponseType.Ok:
				{
					_settings.Recordings = copy;
				}
				break;
			case ResponseType.DeleteEvent:
			case ResponseType.Cancel:
				break;
			default:
				{
					throw new Exception("Unknown response");
				}
		}

		settings.Destroy();
	}

	protected void OnAboutActionActivated(object sender, EventArgs e)
	{
		Gifuser.Help.About.AboutDialog dialog = new Gifuser.Help.About.AboutDialog();
		dialog.Run();
		dialog.Destroy();
	}
}
