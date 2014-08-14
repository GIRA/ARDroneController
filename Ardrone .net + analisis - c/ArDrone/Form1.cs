using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
namespace ARDrone
{
    public partial class Form1 : Form
    {
        ARDrone.Control.DroneControl drone;

        public Form1()
        {
            InitializeComponent();
        }

        private void form1_closing(object sender, EventArgs e)
        {
            sending = false;
            th.Abort();
            th = null;
        }

        Thread th;
        bool _running;
        Bitmap currentframe;
        Rectangle _selectedRectangle;

        Point centroDeImagen = new Point(0, 0);
        List<Point> _ROIArea = new List<Point>();

        Rectangle _follow;

        private void Form1_Load(object sender, EventArgs e)
        {
            this.FormClosing += form1_closing;
            drone = new ARDrone.Control.DroneControl();
            ARDrone.Control.DroneConfig conf = new ARDrone.Control.DroneConfig();
            conf.FirmwareVersion = Control.Data.SupportedFirmwareVersion.Firmware_151;
         
            drone.Init(conf);

            drone.ConnectToDrone();
            timer1.Interval = 300;
            timer1.Enabled = true;
            th = new Thread(new ParameterizedThreadStart(this.sender));
            th.Start();
            _running = true;
            this.FormClosing += delegate { _running = false; };

            this.KeyDown += keydown;
            this.KeyUp += keyup;

            drone.NewFrame += delegate(object sender2, EventArgs e2) {
                if (drone.BitmapImage != null)
                {
                    currentframe = CopyBitmap(drone.BitmapImage);
                    centroDeImagen = new Point(currentframe.Width / 2, currentframe.Height / 2);
                    //_currentRectangle = Analyzer.FindColor(currentframe, _selectedColor);
                     _selectedRectangle= ARDrone.Control.Utils.Mio.VideoAnalyzerUtils.CurrentRectangle;
                    follow();
                    pictureBox1.Image = currentframe;
                }
            };


Pen pen = new Pen(Color.Yellow, 3);
            pictureBox1.Paint += delegate(object sender2, PaintEventArgs args)
            {
                   Graphics g = args.Graphics;
                  
                    g.DrawRectangle(pen, _selectedRectangle);
                  
             };
            foreach (System.Windows.Forms.Control c in this.Controls)
            {
                c.KeyDown += keydown;
                c.KeyUp += keyup;
            }

            Thread.Sleep(100);
            sending = true;
        }



        /// <summary>
        /// this moves the ardrone in order to center the ROI and keep it at a fixed distance.
        /// </summary>
        private void follow()
        {
            if (checkBox1.Checked)
            {
                if (_selectedRectangle.Width != 0 && _selectedRectangle.Height != 0)
                {
                    int centerX = _selectedRectangle.Left + _selectedRectangle.Width / 2;
                    int centerY = _selectedRectangle.Top + _selectedRectangle.Height / 2;

                    yaw = ((float)(centerX - centroDeImagen.X)) / centroDeImagen.X * 0.7f;
                    gaz = ((float)(centerY - centroDeImagen.Y)) / centroDeImagen.Y * -0.4f;


                    if (_follow.Height != 0 && _selectedRectangle.Height != 0)
                    {
                        double rel1, rel2;
                        rel1 = _follow.Width / _follow.Height;
                        rel2 = _selectedRectangle.Width / _selectedRectangle.Height;

                        if (Math.Abs(rel1 - rel2) < 0.05)
                        {
                            //the width/height ratio is similar, this means, i am seeing the same object than the last frame
                            //and from the same angle or something

                            float areaDif = (_follow.Width * _follow.Height) - (_selectedRectangle.Width * _selectedRectangle.Height);
                            areaDif /= (_follow.Width * _follow.Height);
                            Console.WriteLine(areaDif);
                            if (Math.Abs(areaDif) > 0.15)
                            {
                                pitch = areaDif * -0.1f;
                                if (Math.Abs(pitch) > 0.2) { pitch = -0.2f * Math.Sign(areaDif); }
                            }

                        }
                    }
                    if (yaw < -1 || yaw > 1) yaw = 0;
                    if (gaz < -1 | gaz > 1) gaz = 0;
                    if (pitch < -1 || pitch > 1) pitch = 0;
                }
                else { gaz = yaw = pitch = 0; }


            }
        }

       


        float roll, pitch, yaw, gaz;
        bool sending = false;
        private void sender(object state)
        {
            while (true)
            {
                if (drone != null)
                {
                    if (drone.IsConnected)
                    {
                        if (sending)
                        {
                            if (roll == 0 && pitch == 0 && gaz == 0 && yaw == 0)
                            {
                                if (!drone.IsHovering)
                                {
                                    drone.SendCommand(new Control.Commands.HoverModeCommand(Control.Commands.DroneHoverMode.Hover));
                                }
                            }
                            else
                            {
                                if (drone.IsHovering)
                                {
                                    drone.SendCommand(new Control.Commands.HoverModeCommand(Control.Commands.DroneHoverMode.StopHovering));

                                }
                                else
                                {
                                    Console.WriteLine("Sent: Roll: " + roll + " Pitch: " + pitch + " Yaw: " + yaw + " Gaz: " + gaz);
                                    drone.SendCommand(new Control.Commands.FlightMoveCommand(roll, pitch, yaw, gaz));
                                }
                            }
                        }
                         

                    }
                }
               Thread.Sleep(100);
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {


            trackBar1.Value = (int)(roll * 100);
                trackBar2.Value = (int)(pitch * 100);
                 trackBar3.Value = (int)(yaw * 100);
                trackBar4.Value = (int)(gaz * 100);
           
        
            if (drone.IsConnected && _running)
            {
                if (drone.BitmapImage == null)
                {
           
                    drone.SendCommand(new ARDrone.Control.Commands.SwitchCameraCommand(ARDrone.Control.Commands.DroneCameraMode.FrontCamera));
                     
                }
                else
                {
                    listBox1.Items.Clear();
                    if (drone.IsConnected) listBox1.Items.Add("Connected");
                     
                    if (drone.IsHovering) listBox1.Items.Add("Hovering");
                    if (drone.IsEmergency) listBox1.Items.Add("Emergency");
                    if (drone.IsFlying) listBox1.Items.Add("flying");
                    listBox1.Items.Add("BAT - " + drone.NavigationData.BatteryLevel.ToString());
                    this.attitudeIndicatorInstrumentControl1.SetAttitudeIndicatorParameters(drone.NavigationData.Theta, -(drone.NavigationData.Phi));
             //       this.BackColor = Color.Green;
 
            //        pictureBox1.Image = drone.BitmapImage;

                     
                }
            }
         
        }


        //analisis de imagenes
        Color _selectedColor = Color.White;
        Color _lastColor = Color.White;
        double h1 = 0;
        double s=0;
        double v =0;


        private void updateColor(Color c)
        {
            _selectedColor = c;
            panel1.BackColor = _selectedColor;
            ARDrone.Control.Utils.Mio.Analyzer.RGBtoHSV((double)_selectedColor.R / 255, (double)_selectedColor.G / 255, (double)_selectedColor.B / 255, ref h1, ref s, ref v);
            ARDrone.Control.Utils.Mio.VideoAnalyzerUtils.minHue = h1 - trackBar5.Value;
            ARDrone.Control.Utils.Mio.VideoAnalyzerUtils.maxHue = h1 + trackBar5.Value;
            ARDrone.Control.Utils.Mio.VideoAnalyzerUtils.minSat = s - (double)(trackBar6.Value) / 50;
            ARDrone.Control.Utils.Mio.VideoAnalyzerUtils.maxSat = s + (double)(trackBar6.Value) / 50;
            ARDrone.Control.Utils.Mio.VideoAnalyzerUtils.minBright = v - (double)(trackBar7.Value) / 50;
            ARDrone.Control.Utils.Mio.VideoAnalyzerUtils.maxBright = v + (double)(trackBar7.Value) / 50;

            ARDrone.Control.Utils.Mio.Analyzer.setHue(h1 - trackBar5.Value, h1 + trackBar5.Value);
            ARDrone.Control.Utils.Mio.Analyzer.setBrightness(v - (double)(trackBar7.Value) / 50, v + (double)(trackBar7.Value) / 50);

            ARDrone.Control.Utils.Mio.Analyzer.setSaturation(s - (double)(trackBar6.Value) / 50, s + (double)(trackBar6.Value) / 50);
        }

        private Bitmap CopyBitmap(Bitmap image)
        {
            int width = image.Width;
            int height = image.Height;
            Rectangle rectangleToCopy = new Rectangle(0, 0, width, height);

            Bitmap newImage = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(newImage))
            {
                g.DrawImage(image, rectangleToCopy, rectangleToCopy, GraphicsUnit.Pixel);
            }

            return newImage;
        }

        #region "UI"

        void keydown(object sender, KeyEventArgs e)
        {
            switch (e.KeyValue)
            {
                case 65:
                    //a
                    roll = -0.2f;
                    break;
                case 68:
                    //d
                    roll = 0.2f;
                    break;
                case 87:
                    //w
                    pitch = -0.2f;
                    break;
                case 83:
                    //s
                    pitch = 0.2f;
                    break;
                case 38:
                    //up
                    gaz = 0.5f;
                    break;
                case 40:
                    gaz = -0.5f;
                    //down
                    break;
                case 37:
                    //left
                    yaw = -0.5f;
                    break;
                case 39:
                    yaw = 0.5f;
                    //right
                    break;
                default:
                    break;
            }
        }

        void keyup(object sender, KeyEventArgs e)
        {

            switch (e.KeyValue)
            {
                case 65:
                //a
                case 68:
                    //d
                    roll = 0f;
                    break;
                case 87:
                //w 
                case 83:
                    //s
                    pitch = 0f;
                    break;
                case 38:
                //up 
                case 40:
                    //down
                    gaz = 0f;
                    break;
                case 37:
                //left 
                case 39:
                    //right
                    yaw = 0f;
                    break;
                default:
                    break;
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        { 
                MouseEventArgs ev = (MouseEventArgs)e;
                int x = ev.X;
                int y = ev.Y;
                _lastColor = currentframe.GetPixel(x, y);
               updateColor(_lastColor);
       
        }

        private void trackBar6_Scroll(object sender, EventArgs e)
        {

            ARDrone.Control.Utils.Mio.VideoAnalyzerUtils.minSat = s - (double)(trackBar6.Value) / 50;
            ARDrone.Control.Utils.Mio.VideoAnalyzerUtils.maxSat = s + (double)(trackBar6.Value) / 50;
            ARDrone.Control.Utils.Mio.Analyzer.setSaturation(s - (double)(trackBar6.Value) / 50, s + (double)(trackBar6.Value) / 50);

        }

        private void trackBar5_Scroll(object sender, EventArgs e)
        {

             ARDrone.Control.Utils.Mio.VideoAnalyzerUtils.minHue = h1 - trackBar5.Value;
            ARDrone.Control.Utils.Mio.VideoAnalyzerUtils.maxHue = h1 + trackBar5.Value;
            ARDrone.Control.Utils.Mio.Analyzer.setHue(h1 - trackBar5.Value, h1 + trackBar5.Value);
        }

        private void trackBar7_Scroll(object sender, EventArgs e)
        {
            ARDrone.Control.Utils.Mio.VideoAnalyzerUtils.minBright = v - (double)(trackBar7.Value) / 50;
            ARDrone.Control.Utils.Mio.VideoAnalyzerUtils.maxBright = v + (double)(trackBar7.Value) / 50;
            ARDrone.Control.Utils.Mio.Analyzer.setBrightness(v - (double)(trackBar7.Value) / 50, v + (double)(trackBar7.Value) / 50);
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        { 

            if (checkBox1.Checked)
            {
                _follow = _selectedRectangle;

            }
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (drone.IsConnected)
            {

                if (drone.IsFlying || drone.IsHovering)
                {
                    drone.SendCommand(new Control.Commands.FlightModeCommand(Control.Commands.DroneFlightMode.Land));
                    button1.Text = "Takeoff";
                }
                else
                {

                    drone.SendCommand(new Control.Commands.FlightModeCommand(Control.Commands.DroneFlightMode.TakeOff));
                    button1.Text = "Land";
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (drone.IsConnected)
            {
                drone.SendCommand(new Control.Commands.FlightModeCommand(Control.Commands.DroneFlightMode.Emergency));

            }
        }


        private void button4_Click(object sender, EventArgs e)
        {
            if (drone.IsConnected)
            {
                if (drone.CurrentCameraType == Control.Commands.DroneCameraMode.FrontCamera)
                {
                    drone.SendCommand(new Control.Commands.SwitchCameraCommand(Control.Commands.DroneCameraMode.BottomCamera));
                }
                else
                {


                    drone.SendCommand(new Control.Commands.SwitchCameraCommand(Control.Commands.DroneCameraMode.FrontCamera));

                }


            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            _lastColor = Color.White;
            _selectedColor = Color.White;
            ARDrone.Control.Utils.Mio.VideoAnalyzerUtils.minHue = 0;
            ARDrone.Control.Utils.Mio.VideoAnalyzerUtils.maxHue = 360;
            ARDrone.Control.Utils.Mio.VideoAnalyzerUtils.minSat = 0;
            ARDrone.Control.Utils.Mio.VideoAnalyzerUtils.maxSat = 1;
            ARDrone.Control.Utils.Mio.VideoAnalyzerUtils.minBright = 0;
            ARDrone.Control.Utils.Mio.VideoAnalyzerUtils.maxBright = 1;


            ARDrone.Control.Utils.Mio.Analyzer.setHue(0, 360);
            ARDrone.Control.Utils.Mio.Analyzer.setBrightness(0, 1);
            ARDrone.Control.Utils.Mio.Analyzer.setSaturation(0, 1);

        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            roll = (float)(trackBar1.Value) / 100f;
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            pitch = (float)(trackBar2.Value) / 100f;
        }

        private void trackBar3_Scroll(object sender, EventArgs e)
        {
            yaw = (float)(trackBar3.Value) / 100f;
        }

        private void trackBar4_Scroll(object sender, EventArgs e)
        {
            gaz = (float)(trackBar4.Value) / 100f;
        }
#endregion
    }
}
