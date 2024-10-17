using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Security.Policy;

namespace LBA2AutoUpdate {
	public partial class Form1 : Form {
		const string endpoint = "http://cabfiel.tmxc.ru/";
		private string hash;
		private bool final;
		private bool downloading;
		private bool cancelling;
		private System.Net.WebClient client;
		private byte[] r;
		private long lastBytes;
		private long lastBytesSeen;
		private string calcSpeed = "...";
		public Form1() {
			InitializeComponent();
		}

		private void Form1_Load(object sender, EventArgs e) {
			if(!File.Exists("package.nw")) {
				Final("Couldn't find package.nw.\nMake sure the auto-updater is in the same directory\nas the game.");
				return;
			}
			System.Threading.Thread thread = new System.Threading.Thread(Stuff);
			thread.Start();
		}

		public void Form1_FormClosing(object sender, FormClosingEventArgs e) {
			if(downloading && !cancelling && !final) {
				SetStatus("Cancelling...");
				cancelling = true;
				client.CancelAsync();
				e.Cancel = true;
			}
		}

		private void Stuff() {
			var md5 = System.Security.Cryptography.MD5.Create();
			hash = BitConverter.ToString(md5.ComputeHash(File.ReadAllBytes("package.nw"))).Replace("-", "");
			client = new System.Net.WebClient();
			client.DownloadDataAsync(new Uri(endpoint + "meta.txt"));
			client.DownloadDataCompleted += Client_DownloadDataCompleted;
		}

		private void Client_DownloadDataCompleted(object sender, System.Net.DownloadDataCompletedEventArgs e) {
			if(e.Cancelled) {
				Close();
			}
			else if(downloading) {
				ProgressShow(false);
				SetStatus("Writing...");
				r = e.Result;
				client.Dispose();
				System.Threading.Thread thread = new System.Threading.Thread(Write);
				thread.Start();
			} else {
				string[] d = Encoding.ASCII.GetString(e.Result).Split(';');
				if(d[1] == hash) {
					Final("You have the latest version.");
				}
				else {
					SetStatus("An update (v" + d[0] + ") is available.\nClick Update to begin.");
					ButtonShow(true);
				}
			}
		}

		private void Write() {
			File.WriteAllBytes("package.nw", r);
			Final("Update success!");
		}

		private void Final(string t) {
			final = true;
			SetStatus(t);
			ButtonEnable(true);
			ButtonShow(true);
			ButtonText("OK");
		}

		private void button1_Click(object sender, EventArgs e) {
			if(final) Close();
			else if(!downloading) {
				downloading = true;
				button1.Enabled = false;
				SetStatus("Downloading...");
				ProgressShow(true);
				timer1.Start();
				client.DownloadDataAsync(new Uri(endpoint + "package.nw"));
				client.DownloadProgressChanged += Client_DownloadProgressChanged;
			}
		}

		private void Client_DownloadProgressChanged(object sender, System.Net.DownloadProgressChangedEventArgs e) {
			if(cancelling) return;
			ProgressValue(e.ProgressPercentage);
			lastBytes = e.BytesReceived;
			SetStatus("Downloading... " + PrettyPrintSize(e.BytesReceived) + " / " + PrettyPrintSize(e.TotalBytesToReceive) + "\n" + calcSpeed);
		}
		public static string PrettyPrintSize(float size, bool d = false) {
			if(size < 1024) return size + " b";
			else if(size < 1048576) return A(size / 1024,d) + " kb";
			else if(size < 1073741824) return A(size / 1048576,d) + " mb";
			else return A(size / 1073741824,d) + " gb";
		}
		public static string PrettyPrintSize(long size) {
			return PrettyPrintSize((float)size);
		}

		private static float A(float x, bool d) => d ? (float)Math.Floor(x * 100) / 100 : (float)Math.Floor(x);

		private void timer1_Tick(object sender, EventArgs e) {
			calcSpeed = PrettyPrintSize(lastBytes - lastBytesSeen,true) + "/s";
			lastBytesSeen = lastBytes;
		}
		// fuck winforms
		private void SetStatus(string text) {
			label1.BeginInvoke((MethodInvoker)(() => label1.Text = text));
		}
		private void ButtonShow(bool show) {
			button1.BeginInvoke((MethodInvoker)(() => button1.Visible = show));
		}
		private void ButtonText(string text) {
			button1.BeginInvoke((MethodInvoker)(() => button1.Text = text));
		}
		private void ButtonEnable(bool enable) {
			button1.BeginInvoke((MethodInvoker)(() => button1.Enabled = enable));
		}
		private void ProgressShow(bool show) {
			progressBar1.BeginInvoke((MethodInvoker)(() => progressBar1.Visible = show));
		}
		private void ProgressValue(int v) {
			progressBar1.BeginInvoke((MethodInvoker)(() => progressBar1.Value = v));
		}
	}
}
