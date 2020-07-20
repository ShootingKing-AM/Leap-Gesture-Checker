using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Leap;

namespace Guesture_checker_GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Controller lController = new Controller();
        private CGestureMatcher CGMaterObj = new CGestureMatcher();
        public MainWindow()
        {
            InitializeComponent();
            
            if (lController.IsConnected)
                SetButtonStatus(true, true);
            else
                SetButtonStatus(true, false);

            /*if (lController.IsServiceConnected)
                SetButtonStatus(true, true);
            else
                SetButtonStatus(true, false);*/

            //lController.Connect += OnServiceConnect;
            //lController.Disconnect += OnServiceDisconnect;
            lController.FrameReady += OnFrame;
            //lController.DeviceFailure += OnDeviceFailure;
            lController.Device += OnConnect;
            lController.DeviceLost += OnDisconnect;
        }

        /* public void OnServiceConnect(object sender, ConnectionEventArgs args)
         {
             Label testLbl;
             testLbl = (Label)FindName("lTriggerFinger");
             testLbl.Content = "Service Connected";
             Console.WriteLine("Service OnServiceConnect");
         }*/

        public void SetButtonStatus(bool isDStatus, bool isConnected)
        {
            Label lbl;
            if (isDStatus)
                lbl = (Label)FindName("lDStatusShow");
            else
                lbl = (Label)FindName("lSStatusShow");

            if(isConnected)
            {
                lbl.Content = "Connected";
                lbl.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#000000")); //Black; #B2FF0000 - Red; #B2A2EC0B - Green
                lbl.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#B2A2EC0B"));
            }
            else
            {
                lbl.Content = "Disconnected";
                lbl.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFFFFF")); //Black; #B2FF0000 - Red; #B2A2EC0B - Green
                lbl.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#B2FF0000"));
            }
        }

        public void OnConnect(object sender, DeviceEventArgs args)
        {
            Console.WriteLine("Connected");
            SetButtonStatus(true, true);
        }

        public void OnDisconnect(object sender, DeviceEventArgs args)
        {
            Console.WriteLine("Disconnected");
            SetButtonStatus(true, false);
        }
        void OnFrame(object sender, FrameEventArgs args)
        {
            Leap.Frame l_frame = args.frame;

            float[] l_scoresLeft = new float[(uint)(CGestureMatcher.GestureType.GT_GesturesCount)+1];
            float[] l_scoresRight = new float[(uint)(CGestureMatcher.GestureType.GT_GesturesCount)+1];

            CGMaterObj.GetGestures(ref l_frame, CGestureMatcher.GestureHand.GH_LeftHand, ref l_scoresLeft);
            CGMaterObj.GetGestures(ref l_frame, CGestureMatcher.GestureHand.GH_RightHand, ref l_scoresRight);

            //HeadPose l_headPose = controller.headPose(l_frame.timestamp());
            //Vector l_headPos = l_headPose.position();
            //Leap::Quaternion l_headRot = l_headPose.orientation();

            //fprintf(stderr, "Head orientation: P(%.4f,%.4f,%.4f) R(%.4f,%.4f,%.4f,%.4f)\n", l_headPos.x, l_headPos.y, l_headPos.z, l_headRot.x, l_headRot.y, l_headRot.z, l_headRot.w);
            //fprintf(stderr, "<-- CGestureMatcher data -->\n");

            string l_gestureName = "";
            ProgressBar pbObj;
            Label lbObj;
            CheckBox cbValues;
            cbValues = (CheckBox)FindName("cbValues");

            for (uint i = 0U; i < (uint)CGestureMatcher.GestureType.GT_GesturesCount; i++)
            {
                CGMaterObj.GetGestureName((CGestureMatcher.GestureType)(i), ref l_gestureName);
                if(l_gestureName.Length > 0)
                {
                    pbObj = (ProgressBar)FindName("pb" + l_gestureName);
                    pbObj.Value = l_scoresLeft[i];

                    pbObj = (ProgressBar)FindName("pb" + l_gestureName + "_Copy");
                    pbObj.Value = l_scoresRight[i];

                    if(cbValues.IsChecked.GetValueOrDefault())
                    {
                        lbObj = (Label)FindName(l_gestureName + "l");
                        lbObj.Content = l_scoresLeft[i];

                        lbObj = (Label)FindName(l_gestureName + "r");
                        lbObj.Content = l_scoresRight[i];
                    }
                }                    
            }
        }
        /*public void OnServiceConnect(object sender, ConnectionEventArgs args)
        {
            Console.WriteLine("Service Connected");
            SetButtonStatus(false, true);
        }

        public void OnServiceDisconnect(object sender, ConnectionLostEventArgs args)
        {
            Console.WriteLine("Service Disconnected");
            SetButtonStatus(false, false);
        }

        public void OnServiceChange(Controller controller)
        {
            Console.WriteLine("Service Changed");
            if (lController.IsServiceConnected)
                SetButtonStatus(true, true);
            else
                SetButtonStatus(true, false);
        }

        public void OnDeviceFailure(object sender, DeviceFailureEventArgs args)
        {
            Console.WriteLine("Device Error");
            Console.WriteLine("  PNP ID:" + args.DeviceSerialNumber);
            Console.WriteLine("  Failure message:" + args.ErrorMessage);
        }*/
        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Uri.AbsoluteUri);
        }
    }
}
