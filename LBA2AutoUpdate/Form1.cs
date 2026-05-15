using System;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace LBA2AutoUpdate {
	public partial class Form1 : Form {
		private Endpoint[] endpoints = {
			new Endpoint("https://cabfiel.tmxc.ru", "Primary (Russia)"),
			new Endpoint("http://cabfeil.atwebpages.com/lba2", "AwardSpace (Bulgaria)"),
			new Endpoint("https://lba2mirror.pages.dev", "Cloudflare"),
			new Endpoint("https://sound.jp/coolio", "Sound.jp (Japan)"),
		};
		private int chunks = 0;
		private int currentChunk = 1;
		private bool final;
		private bool downloading;
		private bool cancelling;
		private Version cversion;
		private System.Net.WebClient client;
		private byte[] r;
		private long lastBytes;
		private long lastBytesSeen;
		private string calcSpeed = "...";
		private int bestEndpoint;
		public Form1() {
			InitializeComponent();
		}

		private void Form1_Load(object sender, EventArgs e) {
			if(!File.Exists("package.nw")) {
				MessageBox.Show("Couldn't find package.nw.\nMake sure the auto-updater is in the same directory as the game.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Close();
				return;
			}
			try {
				cversion = ReadVersion("package.nw");
			} catch {
				MessageBox.Show("Failed to read the game's version.\nThis might be a sign of corruption. Try redownloading the game through the website or itch.io.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Close();
				return;
			}
			System.Threading.Thread thread = new System.Threading.Thread(Initialize);
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

		private void Initialize() {
			SetStatus("Figuring out the best location to download the game from...\n(might take a few seconds)");
			var clientSpeedCheck = new System.Net.WebClient();
			var responseTimes = new long[endpoints.Length];
			for(int i = 0; i < endpoints.Length; i++) {
				var t = Stopwatch.StartNew();
				try {
					clientSpeedCheck.DownloadData(endpoints[i].address);
					responseTimes[i] = t.ElapsedMilliseconds;
				} catch { responseTimes[i] = long.MaxValue; }
			}
			long shortest = long.MaxValue;
			for(int i = 0; i < responseTimes.Length; i++)
				if(responseTimes[i] < shortest) {
					shortest = responseTimes[i];
					bestEndpoint = i;
				}
			if(shortest == long.MaxValue) Final("Every single location is inaccessible. Try downloading the latest update through the website or itch.io.");
			label2.Text = "Endpoint: " + endpoints[bestEndpoint].label;
			client = new System.Net.WebClient();
			client.DownloadDataCompleted += Client_DownloadDataCompleted;
			SetStatus("Getting information...");
			client.DownloadDataAsync(new Uri(endpoints[bestEndpoint].address + "/meta.txt"));
		}

		private void Client_DownloadDataCompleted(object sender, System.Net.DownloadDataCompletedEventArgs e) {
			if(e.Cancelled) {
				Close();
			}
			else if(downloading) {
				SetStatus("Writing...");
				r = e.Result;
				client.Dispose();
				System.Threading.Thread thread = new System.Threading.Thread(Write);
				thread.Start();
			} else {
				string[] d = Encoding.ASCII.GetString(e.Result).Split(';');
				Version ver = new Version();
				try {
					ver = new Version(d[0]);
				}
				catch {
					MessageBox.Show("Received an invalid response.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					Close();
					return;
				}
				if(d.Length > 1) int.TryParse(d[1], out chunks);
				if(ver == cversion) {
					Final("You already have the latest version.");
				}
				else if(ver < cversion) {
					Final("You appear to be using a newer version (" + cversion + ")...?\nThat can't be right. Please contact the developer.");
				}
				else {
					SetStatus("An update (v" + d[0] + ") is available.\nClick Update to download it.");
					ButtonShow(true);
				}
			}
		}

		private void Write() {
			timer1.Stop();
			if(chunks > 0) {
				ProgressValue2((int)(currentChunk / (float)chunks * 100));
				if(currentChunk == 1) File.WriteAllBytes("package.nw", r);
				else using(var fs = File.Open("package.nw", FileMode.Append)) { fs.Write(r, 0, r.Length); }
				if(currentChunk == chunks) Final("Update success!");
				else {
					currentChunk++;
					SetStatus("Downloading...");
					timer1.Start();
					client.DownloadDataAsync(new Uri(endpoints[bestEndpoint].address + "/package.nw." + currentChunk.ToString().PadLeft(3, '0')));
				}
			} else {
				File.WriteAllBytes("package.nw", r);
				Final("Update success!");
			}
		}

		private void Final(string t) {
			final = true;
			SetStatus(t);
			ProgressShow(false);
			ProgressShow2(false);
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
				if(chunks > 0) ProgressShow2(true);
				timer1.Start();
				client.DownloadDataAsync(new Uri(endpoints[bestEndpoint].address + "/package.nw" + (chunks > 0 ? ".001" : "")));
				client.DownloadProgressChanged += Client_DownloadProgressChanged;
			}
		}

		private void Client_DownloadProgressChanged(object sender, System.Net.DownloadProgressChangedEventArgs e) {
			if(cancelling) return;
			ProgressValue(e.ProgressPercentage);
			lastBytes = e.BytesReceived;
			SetStatus("Downloading... " + PrettyPrintSize(e.BytesReceived) + " / " + PrettyPrintSize(e.TotalBytesToReceive) + "\n" + calcSpeed);
		}
		private static string PrettyPrintSize(float size, bool d = false) {
			if(size < 1024) return size + " b";
			else if(size < 1048576) return A(size / 1024,d) + " kb";
			else if(size < 1073741824) return A(size / 1048576,d) + " mb";
			else return A(size / 1073741824,d) + " gb";
		}
		private static string PrettyPrintSize(long size) {
			return PrettyPrintSize((float)size);
		}

		private static float A(float x, bool d) => d ? (float)Math.Floor(x * 100) / 100 : (float)Math.Floor(x);
		private static Version ReadVersion(string filePath) {
			using(var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read)) {
				long bytesToRead = Math.Min(3, fs.Length);
				fs.Seek(fs.Length - bytesToRead, SeekOrigin.Begin);
				byte[] buffer = new byte[bytesToRead];
				fs.Read(buffer, 0, (int)bytesToRead);
				return new Version(buffer);
			}
		}

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
		private void ProgressShow2(bool show) {
			progressBar2.BeginInvoke((MethodInvoker)(() => progressBar2.Visible = show));
		}
		private void ProgressValue2(int v) {
			progressBar2.BeginInvoke((MethodInvoker)(() => progressBar2.Value = v));
		}
	}
	internal struct Endpoint {
		public string address;
		public string label;
		public Endpoint(string address, string label) {
			this.address = address;
			this.label = label;
		}
	}
}
