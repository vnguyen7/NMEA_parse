using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Management; 

namespace parseGui
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //disabled close btn 
            btnClose.Enabled = false;

            //get all available port
            string[] ports = SerialPort.GetPortNames();
            //Find the port that contain u-blox 7 vendor id 1546 and product id 01A7
            List<string> vid = ComPortNames("1546","01A7");
            foreach (string s in ports)
            {
                if (vid.Contains(s))
                {
                    //set text for availPort label 
                    availPort.Text = s;
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (serialPort1.IsOpen)
                serialPort1.Close();
        }

        /* Private function to get vendor id and product id of ublox gps device. 
         * Get the ublox gps id from device manager. 
         * Create pattern by passing the vid and pid into the parameter. 
         * Generate a regex formula and ignore case sensitive. 
         * OpenSubKey : open the subfolder inside the HKEY_LOCAL_MACHINE registra 
         * GetSubKeyNames: list all the subfolder's name 
         * HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Enum\USB\VID_1546&PID_01A7\5&376aba2d&0&5\Device Parameters
         */
        private List<string> ComPortNames(String VID, String PID) 
        {
            String pattern = String.Format("^VID_{0}.PID_{1}", VID, PID);
            Console.WriteLine(pattern + "pattern"); 
            Regex _rx = new Regex(pattern, RegexOptions.IgnoreCase);
            List<string> comports = new List<string>();
            RegistryKey rk1 = Registry.LocalMachine;
            RegistryKey rk2 = rk1.OpenSubKey("SYSTEM\\CurrentControlSet\\Enum");

            //Go into enum subfolder in HKEY_LOCAL_MACHINE
            foreach (String s3 in rk2.GetSubKeyNames())
            {
                //consider only when the subfolder is USB 
                 if (s3 == "USB")
                { 
                    RegistryKey rk3 = rk2.OpenSubKey(s3);
                    foreach (String s in rk3.GetSubKeyNames())
                    {
                        //if match with the regex VID_0156&PID_01A7
                        if (_rx.Match(s).Success)
                        {
                            RegistryKey rk4 = rk3.OpenSubKey(s);

                            foreach (String s2 in rk4.GetSubKeyNames())
                            {
                                Console.WriteLine(s2);
                                RegistryKey rk5 = rk4.OpenSubKey(s2);
                                Console.WriteLine("rk5:" + rk5);
                                RegistryKey rk6 = rk5.OpenSubKey("Device Parameters");
                                Console.WriteLine("rk6:" + rk6);
                                //Get port name and return 
                                comports.Add((string)rk6.GetValue("PortName"));
                            }
                        }
                 }
                }
            }
            return comports;
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            btnOpen.Enabled = false;
            btnClose.Enabled = true;
            try
            {
              //set port to serialPort1 to open 
                serialPort1.PortName = availPort.Text; 
                serialPort1.Open();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "messsage", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            btnOpen.Enabled = true;
            btnClose.Enabled = false;
            try
            {
                serialPort1.Close();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "messsage", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtReceived.Clear();
        }

        private string rxString, parseString="";
        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            rxString = serialPort1.ReadLine();
            this.Invoke(new EventHandler(displayText));
        }

        /*
       $GPGLL,4916.45,N,12311.12,W,225444,A,*1D
            GLL: geographic position, latitude and longitude (dd mm,mmmm format 0-7 decimal places) 
            4916.45,N: Latitude 49 deg. 16.45 min. North    ( N / S) 
            12311.12,W: Longitude 123 deg. 11.12 min. West  (W / E) 
            225444 : Fix taken at 22:5444 UTC            (hhmmss.ss) 
            A: data Active or V (void)
            *1D: checksum data 
        */
        private string ParseGLL(string sentence)
        {
            //check for NMEA maximum length 
            if (sentence.Length < 82)
            {
                string[] arr = sentence.Split(',');
                //case of GLL,and check if enough information of latitude, longtitude to parse 
                if ((arr[0] == "$GPGLL") && (arr[1] != "") && (arr[2] == "N" || arr[2] == "S")
                    && (arr[3] != "") && (arr[4] == "W" || arr[4] == "E"))
                {
                    // Append latitude: first 2 digit deg, last 4 digit is min, north/south 
                    string Latitude = arr[1].Substring(0, 2) + " deg";
                    Latitude = Latitude + arr[1].Substring(2) + " min";
                    if (arr[3] == "N")
                        Latitude = Latitude + " North ";
                    else Latitude = Latitude + " South ";

                    //Append longitude: first 
                    string Longitude = arr[3].Substring(0, 2) + " deg" + arr[3].Substring(3) + " min";
                    if (arr[3] == "E")
                        Longitude = Longitude + " East ";
                    else Longitude = Longitude + " West ";

                    //Append UTC time hhmmss.ss
                    string time = arr[5].Substring(0, 2) + ":" + arr[5].Substring(2, 2) + ":" + arr[5].Substring(4) + " UTC";
                    string pos;
                    //valid data or not?
                    if (arr[6] == "A")
                        pos = "valid data\n";
                    else pos = "unvalid data\n";

                    parseString = "Lattitude: "+Latitude + "Longitude: " + Longitude + " " + time + " " + pos +"\n";
                }
                else if (arr[0] == "$GPGLL")
                {
                    parseString = "invalid data " + arr[5].Substring(0, 2) + ":" + arr[5].Substring(2, 2) + ":" + arr[5].Substring(4) + " UTC\n";
                }
            }
            else parseString = "invalid NMEA Length"; 
            return parseString;
        }
       // string text; 
        private void displayText(object o,EventArgs e)
        {
           // text = "$GPGLL,4916.45,N,12311.12,W,225444,A,*1D";
            parseString = ParseGLL(rxString); 
            Console.WriteLine(parseString); 
            txtReceived.AppendText(parseString); 
        }
      
    }
}
