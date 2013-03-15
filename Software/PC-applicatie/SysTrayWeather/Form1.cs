using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using System.Globalization;

namespace SysTrayWeather
{
	public partial class Form1 : Form
	{
		delegate void show_results(object status);

		bool doc_compl_first = true;

		static System.Threading.Timer timer;
		bool shutdownflag = false;
		string apikey = "apitestreadkey";//read only - //full access key: 24071b850fcf32699e66637f3d043e5762979894
		string sid = "14730"; //apitest system
		DateTime date = new DateTime(2012, 11, 9);

		public Form1()
		{
			InitializeComponent();
			int initinterval = 7 * 1000;
			int timeinterval = 2 * 60 * 1000;
			timer = new System.Threading.Timer(timer_ThreadingCallBack, null, initinterval, timeinterval);
		}

		private void Form1_Resize(object sender, EventArgs e)
		{
			if (WindowState == FormWindowState.Minimized)
			{
				notifyIcon1.Visible = true;
				this.ShowInTaskbar = false;
			}
			if (WindowState == FormWindowState.Normal)
			{
				notifyIcon1.Visible = false;
				this.ShowInTaskbar = true;
			}
		}

		private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			this.WindowState = FormWindowState.Normal;
		}
		
		private void timer_ThreadingCallBack(object state)
		{
			string result = "";

			date = date.AddDays(1);
			string datestring = date.ToString("yyyyMMdd");

			string url = "http://pvoutput.org/service/r2/getoutput.jsp?key=" + apikey + "&sid=" + sid + "&df=" + datestring + "&limit=20";
			WebRequest webGETreq;
			Stream so;
			StreamReader sr;

			webGETreq = WebRequest.Create(url);
			try
			{
				so = webGETreq.GetResponse().GetResponseStream();
				sr = new StreamReader(so);

				string readout = sr.ReadToEnd();
				int item_cnt = readout.Length - readout.Replace(";", "").Length + 1;
				readout = readout.Replace(';', '\r');
				//label1.Text = readout;
				using (StringReader reader = new StringReader(readout))
				{
					int counter = 0;
					int totaal = 0;
					DateTime dag_plus1 = new DateTime(0);
					List<string> opvallende_waardes = new List<string>();
					string line;
					while ((line = reader.ReadLine()) != null)
					{
						//calculate average
						string[] elem = line.Split(',');
						string datestr = elem[0];
						string kw = elem[1];
						string eff = elem[2];

						int year = Convert.ToInt32(datestr.Substring(0, 4));
						int maand = Convert.ToInt32(datestr.Substring(4, 2));
						int dag = Convert.ToInt32(datestr.Substring(6, 2));
						DateTime dag_line = new DateTime(year, maand, dag);

						totaal += Convert.ToInt32(kw);
						double test = Convert.ToDouble(eff, CultureInfo.InvariantCulture);
						if (test < 0.5)
						{
							opvallende_waardes.Add("Efficientie minder dan 50%:" + dag_line.ToShortDateString() + ", KW:" + kw + ", Eff:" + eff +"%");
						}
					

						if (dag_plus1.Ticks == 0)
						{
							dag_plus1 = dag_line;
						}
						else
						{
							if (counter <= item_cnt)
							{
								DateTime dag_c = dag_line;
								dag_c = dag_c.AddDays(1);
								while (dag_plus1 != (dag_c))
								{
									opvallende_waardes.Add("$ missende datum:" + dag_c.ToShortDateString());
									dag_c = dag_c.AddDays(1);
								}
								dag_plus1 = dag_line;
							}
						}

						counter++;
					}
					int avg = totaal / counter;
					result = "Gemiddelde: " + avg + " kWh\r\n";
					int index = 0;
					for(;index < opvallende_waardes.Count; index++)
					{
						result += opvallende_waardes[index] + "\r\n";
					}
				}
				
			}
			catch (WebException ex)
			{
				result = ex.Message;
			}

			WebClient weer_rss = new WebClient();
			XmlDocument xml = new XmlDocument();
			try
			{
				string rss_xml = ASCIIEncoding.Default.GetString(weer_rss.DownloadData("http://api.wxbug.net/getLiveCompactWeatherRSS.aspx?ACode=A3661025697&stationid=EHLW&unittype=1"));

				xml.LoadXml(rss_xml);
				XmlNodeList awsnode = xml.GetElementsByTagName("aws:weather");
				XmlNode weathernode = awsnode.Item(0);
				string cond = weathernode["aws:current-condition"].InnerText;
				string temp = weathernode["aws:temp"].InnerText;
					   temp += " °C";
				string rain = weathernode["aws:rain-today"].InnerText;

				if (weathernode != null)
				{
					result += cond + "\r\n";
					result += "Temperatuur: "+temp + "\r\n";
					result += "Regen: "+ rain + "mm\r\n";
				}

			}
			catch (Exception ex)
			{
				result += ex.Message;
			}

			show_result((object)result);
			
		}

		void show_result(object status)
		{
			if (InvokeRequired)
			{
				BeginInvoke(new show_results(show_result), new object[] { status });
				return;
			}

			label1.Text = (string)status;

			this.WindowState = FormWindowState.Normal;
		}

		private void Form1_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (shutdownflag)
			{
			}
			else
			{
				e.Cancel = true;
				this.WindowState = FormWindowState.Minimized;
				notifyIcon1.Visible = true;
				this.ShowInTaskbar = false;
			}
		}

		private void openToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this.WindowState = FormWindowState.Normal;
			notifyIcon1.Visible = false;
			this.ShowInTaskbar = true;
		}

		private void aflsuitenToolStripMenuItem_Click(object sender, EventArgs e)
		{
			shutdownflag = true;
			this.Close();
		}

		private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
		{
			if (doc_compl_first)
			{
				webBrowser1.Document.Window.ScrollTo(0, 90);
				doc_compl_first = false;
			}
		}
	}
}
