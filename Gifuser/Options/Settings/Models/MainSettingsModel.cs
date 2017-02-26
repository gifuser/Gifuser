namespace Gifuser.Options.Settings.Models
{
	public class MainSettingsModel
	{
		public bool UploadOnStop
		{
			get;
			set;
		}

		public string UploadServiceName
		{
			get;
			set;
		}

		public RecordingModel Recordings
		{
			get;
			set;
		}
	}
}
