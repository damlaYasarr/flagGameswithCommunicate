using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Threading;
using System.Net.Sockets;


namespace client
{
    public partial class Form1 : Form
    {
        private Socket client;
        private byte[] data = new byte[10];
        private int flagField = 16;
        private List<int> coor = new List<int>();
        private List<string> flag = new List<string>();
        private List<List<string>> Player1 = new List<List<string>>();
        private int flagcounter = 0;
        private string receivedCoords;
        private Boolean waitForHitResult = true;

        public Form1()
        {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;

            sendButton.Click += new EventHandler(sendbut_Click);          
            enterKoordinat.Click += new EventHandler(enter_Click_1);
            sendButton.Enabled = waitForHitResult;
            enterKoordinat.Enabled = waitForHitResult;
        }

        private void enter_Click_1(object sender, EventArgs e)
        {
            opTextBox.Text = "";
            playerTextBox.Text = "";
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            MouseEventArgs me = (MouseEventArgs)e;
            Point coordinates = me.Location;

            var MouseX = coordinates.X;
            var MouseY = coordinates.Y;

            var coorText = MouseX + "," + MouseY;       

            if (setFlagMode.Checked){
                flagcounter++;
                FlagCoordinates1.Items.Add(coorText);
                var coords = coorText.Split(',');
               
               coor.Add(Int32.Parse(coords[0]));
               coor.Add(Int32.Parse(coords[1]));

                List<string> flag = new List<string>();
                for (int x = coor.ElementAt(0) - flagField / 2; x < coor.ElementAt(0) + flagField / 2; x++){
                    for (int y = coor.ElementAt(1) - flagField / 2; y < coor.ElementAt(1) + flagField / 2; y++){
                        var coord_temp = x+","+y;
                        flag.Add(coord_temp);
                    }
                }
                Player1.Add(flag);
                coor.Clear();
                if (flagcounter == 5)
                {
                    setFlagMode.Checked = false;
                    setFlagMode.Enabled = false;
                   
                    for(int i = 0; i<Player1.Count; i++)
                    {
                        Console.WriteLine((i + 1) + ". Flag : {");
                        for (int c = 0; c < flagField * flagField; c++) {
                            Console.Write(Player1.ElementAt(i).ElementAt(c)+", " );
                            if((c+1) %16 == 0) { Console.WriteLine(""); }
                        }
                        Console.WriteLine("}");
                    }
                }
                else if (flagcounter < 5)
                {
                    enterKoordinat.Enabled = false;
                }
            }
            else { enterKoordinat.Text = coorText; }         
        }

        private void sendbut_Click(object sender, EventArgs e)
        { 
            byte[] message = Encoding.ASCII.GetBytes(enterKoordinat.Text);
            enterKoordinat.Clear();
            opTextBox.Text = "";
            client.BeginSend(message, 0, message.Length, 0,
            new AsyncCallback(SendData), client);
        }

        void Connected(IAsyncResult iar)
        {
            try
            {
                client.EndConnect(iar);
                Console.WriteLine("Connected to: " +client.RemoteEndPoint.ToString());
                Thread receiver = new Thread(new ThreadStart(ReceiveData));
                receiver.Start();
            }
            catch (SocketException)
            {
                Console.WriteLine("error");
            }
        }

        void SendData(IAsyncResult iar)
        {
            Socket remote = (Socket)iar.AsyncState;
            int sent = remote.EndSend(iar);
        }

        void ReceiveData()
        {

            int recv=0;
            string stringData;
            while (true)
            {
                recv = client.Receive(data);
                stringData = Encoding.ASCII.GetString(data, 0, recv);
                receivedCoords = stringData;
                Console.WriteLine(receivedCoords);
                if (stringData == "bye")
                {
                    break;
                }
                else if (stringData == "win")
                {
                    playerTextBox.Text = "Lose";
                    opTextBox.Text = "Win";
                    waitForHitResult = false;
                    sendButton.Enabled = waitForHitResult;
                    enterKoordinat.Enabled = waitForHitResult;
                }

                else if (waitForHitResult)
                {
                    playerTextBox.Text = "";
                    if (receivedCoords != "-1,-1")
                    {
                        playerTextBox.Text = " success!";
                        flagCoordinates2.Items.Add(receivedCoords);
                        if (flagCoordinates2.Items.Count == 5)
                        {
                            playerTextBox.Text = "Win";
                            opTextBox.Text = "Lose";
                            client.Send(Encoding.ASCII.GetBytes("win"));
                        }
                    }
                    else
                    {
                        playerTextBox.Text = "unsuccess.";
                    }

                    waitForHitResult = false;
                    sendButton.Enabled = waitForHitResult;
                    enterKoordinat.Enabled = waitForHitResult;
                }
                else
                {
                    
                    Boolean hit = false, repeatedHit = false;
                    int flagIndex = -1;
                    string flagCoords = "-1,-1";
                    for (int flag = 0; flag < Player1.Count; flag++)
                    {
                        if (Player1.ElementAt(flag).Contains(receivedCoords))
                        {

                            if (!(FlagCoordinates1.Items[flag] + "").Contains("(Eliminated)"))
                            {
                                hit = true;
                                opTextBox.Text = " success!";
                                flagIndex = flag;
                                flagCoords = FlagCoordinates1.Items[flag] + "";
                                FlagCoordinates1.Items.RemoveAt(flag); // buna bi bak
                                FlagCoordinates1.Items.Insert(flag, flagCoords + " (Eliminated) ");
                            }
                            else
                            {
                                opTextBox.Text = (flag + 1) + "th flag is choosed again.";
                                repeatedHit = true;
                            }
                        }         
                    }
                    if (hit == false && repeatedHit == false)
                    {
                        opTextBox.Text = " unsuccess!";
                    }

                    client.Send(Encoding.ASCII.GetBytes(flagCoords));
                    waitForHitResult = true;
                    sendButton.Enabled = waitForHitResult;
                    enterKoordinat.Enabled = waitForHitResult;
                }
                receivedCoords = "";
            }
            stringData = "bye";
            byte[] message = Encoding.ASCII.GetBytes(stringData);
            client.Send(message);
            client.Close();
            Console.WriteLine("Connection stopped");
            return;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Console.WriteLine("“Connecting...”");
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream,
            ProtocolType.Tcp);
            IPEndPoint iep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9050);
            client.BeginConnect(iep, new AsyncCallback(Connected), client);
        }
    }
}