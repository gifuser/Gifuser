using System;
using Gifuser.Upload;

namespace Gifuser
{
	public partial class UploadDialog : Gtk.Dialog
	{
		private readonly string _fileName;
		private readonly IFileTrackedUpload _uploader;
		private bool _canceled;
		private uint _timer;
		private bool _uploadFinished;

		public UploadDialog(IFileTrackedUpload uploader, string fileName)
		{
			if (uploader == null)
			{
				throw new ArgumentNullException("uploader");
			}

			if (fileName == null)
			{
				throw new ArgumentNullException("fileName");
			}
			
			_uploader = uploader;
			_fileName = fileName;
			_canceled = false;
			_uploadFinished = false;

			this.Build();
		}

		bool timerHandler()
		{
			progressbar1.Pulse();

			return !_canceled && !_uploadFinished;
		}

		protected override void OnShown()
		{
			base.OnShown();

			if (_uploader.ReportsProgress)
			{
				_uploader.Progress += Uploader_Progress;
			}
			else
			{
				_timer = GLib.Timeout.Add(100, timerHandler);
			}

			_uploader.Completed += Uploader_Completed;

			_uploader.StartAsync(_fileName);
		}

		protected override void OnClose()
		{
			CancelUploader();

			if (!_uploader.ReportsProgress)
			{
				GLib.Source.Remove(_timer);
			}
			
			base.OnClose();
		}

		protected override void OnDestroyed()
		{
			if (_uploader.ReportsProgress)
			{
				_uploader.Progress -= Uploader_Progress;
			}

			_uploader.Completed -= Uploader_Completed;

			base.OnDestroyed();
		}

		private void CancelUploader()
		{
			if (!_canceled)
			{
				_uploader.CancelAsync();
			}
			_canceled = true;
		}

		void Uploader_Completed(object sender, UploadCompletedEventArgs e)
		{
			Gtk.Application.Invoke(delegate
			{
				_uploadFinished = true;
				
				if (!e.Canceled)
				{
					string newLine = Environment.NewLine;
					
					Gtk.MessageDialog dialog;
					if (e.HasError)
					{
						string txt = string.Format("An error occurred in the upload.{0}{0}You will need to upload your file manually:{0}{0}{1}", newLine, _fileName);
						dialog = new Gtk.MessageDialog(
							this,
							Gtk.DialogFlags.Modal,
							Gtk.MessageType.Info,
							Gtk.ButtonsType.Ok,
							txt
						);
					}
					else
					{
						using (Gtk.Clipboard clipboard = Gtk.Clipboard.Get(Gdk.Atom.Intern("CLIPBOARD", false)))
						{
							clipboard.Text = e.Link;
						}
						
						string txt = string.Format("Done!{0}{0}{1}{0}{0}The link is already on your clipboard.", newLine, e.Link);
						dialog = new Gtk.MessageDialog(
							this,
							Gtk.DialogFlags.Modal,
							Gtk.MessageType.Info,
							Gtk.ButtonsType.Ok,
							txt
						);
					}
					
					dialog.Run();
					dialog.Destroy();
				}
					
				Destroy();
			});
		}

		void Uploader_Progress(object sender, UploadProgressEventArgs e)
		{
			Gtk.Application.Invoke(delegate
			{
				if (e.Progress == 100)
				{
					buttonCancel.Sensitive = false;
				}
				
				uploadLabel.Text = string.Format("Upload progress: {0}%", e.Progress);
				progressbar1.Fraction = ((double)(e.Progress)) / 100.0;
			});
        }

		protected void OnButtonCancelClicked(object sender, EventArgs e)
		{
			CancelUploader();
		}
	}
}
