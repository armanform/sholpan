using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

//----------------additional libraries--------------
using Microsoft.Speech.Recognition;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Synthesis;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Media;
//----------------kinect libraries------------------
using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit.FaceTracking;
using System.IO;
namespace AwesomeFaceTracking
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        KinectSensor kinectSensor;
        FaceTracker faceTracker;
        private byte[] colorPixelData;
        private short[] depthPixelData;
        private Skeleton[] skeletonData;
        double distance;
        //initialize serial port
        public static SerialPort serialP = new SerialPort("COM6", 38400, 0, 8, StopBits.One);

        //save data: distance to object 
        double FIXED_DISTANCE = 0;

        // Activator which switch on condition "slejenie za ob'ektom"
        bool isActive = false;

        // operations with Sholpan
        enum typeCondition { NONE, FIRST, SECOND, THIRD, FORTH, FIVTH, SIXTH, SEVENTH };

        // face conditions
        enum faceConditions { NONE, FIRST, SECOND};

        // currentFace
        int currentFace = (int)faceConditions.NONE;

        // say hi to right hand
        int activatorRightHand = 0;
        
        // say hi to left hand
        bool activatorLeftHand = false;

        // shows the current condition
        int currentAction = 10;
        
        // we can only one time for session call activator
        bool slejenie = false;

        // turn head to the right
        bool headToRight = false;

        // turn head to the left
        bool headToLeft = false;

        // position of head is center
        bool headToCenter = false;

        // session begin
        bool firstMeet = false;

        // session close
        bool sessionClose = false;


        public MainWindow()
        {
            InitializeComponent();

            // initialize serial port
            //serialP = new SerialPort("COM8", 115200, 0, 8, StopBits.One);
            serialP.Open();
            currentAction = (int)typeCondition.NONE;
            
            // For a KinectSensor to be detected, we can plug it in after the application has been started.
            KinectSensor.KinectSensors.StatusChanged += KinectSensors_StatusChanged;
            // Or it's already plugged in, so we will look for it.
            var kinect = KinectSensor.KinectSensors.FirstOrDefault(k => k.Status == KinectStatus.Connected);
            if (kinect != null)
            {
              if (!(kinect.IsRunning)){
                OpenKinect(kinect);}
            }
            //MessageBox.Show(Directory.GetCurrentDirectory());
            init_speech();
           
        }

        /// <summary>
        /// Handles the StatusChanged event of the KinectSensors control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Microsoft.Kinect.StatusChangedEventArgs"/> instance containing the event data.</param>
        void KinectSensors_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            if (e.Status == KinectStatus.Connected)
            {
                OpenKinect(e.Sensor);
            }
        }

        /// <summary>
        /// Opens the kinect.
        /// </summary>
        /// <param name="newSensor">The new sensor.</param>
        private void OpenKinect(KinectSensor newSensor)
        {
            kinectSensor = newSensor;
            
            // Initialize all the necessary streams:
            // - ColorStream with default format
            // - DepthStream with Near mode
            // - SkeletonStream with tracking in NearReange and Seated mode.

            kinectSensor.ColorStream.Enable();

            kinectSensor.DepthStream.Range = DepthRange.Near;
            kinectSensor.DepthStream.Enable(DepthImageFormat.Resolution80x60Fps30);

            kinectSensor.SkeletonStream.EnableTrackingInNearRange = true;
            //kinectSensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
            kinectSensor.SkeletonStream.Enable(new TransformSmoothParameters() { Correction = 0.5f, JitterRadius = 0.05f, MaxDeviationRadius = 0.05f, Prediction = 0.5f, Smoothing = 0.5f });
            
            // Listen to the AllFramesReady event to receive KinectSensor's data.
            kinectSensor.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(kinectSensor_AllFramesReady);

            // Initialize data arrays
            colorPixelData = new byte[kinectSensor.ColorStream.FramePixelDataLength];
            depthPixelData = new short[kinectSensor.DepthStream.FramePixelDataLength];
            skeletonData = new Skeleton[6];
            
            // Starts the Sensor
            kinectSensor.Start();
            
            // Initialize a new FaceTracker with the KinectSensor
            faceTracker = new FaceTracker(kinectSensor);
            
        }
        #region "voice recognition"
        int last = -1;
        Random rend = new Random();
        Collection<int> massiv=new Collection<int>();
        Stream[] wavstream;
        long[] wavposes = new long[10];
        RecognizerInfo _recinfo;
        SpeechRecognitionEngine _recognizer;
        GrammarBuilder _gb;
        SpeechSynthesizer _synth;
        SoundPlayer sndp = new SoundPlayer();
        int[] indexor = { 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 5, 5, 5, 5, 6, 6, 7 };
        int[] ansind = { 1,1,1,4,5,1,3,1};
        string[] commsru = {  "Здравствуйте!"
,"Привет Шолпан!"
,"Привет!"
,"Шолпан!"
, "Как тебя зовут?"
,"Расскажи о себе"
,"Кто ты?"
,"Твое имя?"
,"Сколько тебе лет?"
,"Когда тебя создали"
,"Когда тебя изобрели"
,"Когда тебя сделали?"
,"Как долго ты работаешь?"
,"Пока Шолпан!"
,"До свидания"
,"Прощай"
,"До скорой встречи"
,"Что ты умеешь?"
,"Что ты умеешь делать?"
,"Что можешь делать"
,"Почему тебя назвали «Шолпан»?"
,"Что означает твое имя?"
,"Почему тебя зовут «Шолпан»?"
,"Расскажи о своем имени"
,"Следуй за мной"
,"стоп"
,"Кто тебя собрал"
        };
        //string kz="wavkaz";
        //string ru = "wav";
        string[] comms = {  "Салееем!"
,"Салем, Шолпан!"
,"Салем!"
,"Шолпан!"
, "Сенын атын кым?"
,"Озынды таныстыршы"
,"Сен кымсын?"
,"Есымын кым?"
,"Сенын жасын нешеде?"
,"Сены кашан кұрастырды"
,"Сены кашан ойлап тапты"
,"Сены кашан жасап шыгарды?"
,"Косылганыңа канша уақыт болды?"
,"Сау бол, Шолпан!"
,"Кездескенше"
,"кош бол"
,"Келесыде кездескенше"
,"Сен не жасай аласың?"
,"Сенын колыннан не келеды?"
,"Сенын кандай кабылеттерын бар"
,"Сенын атынды неге «Шолпан» деп койды?"
,"Есымын не былдыреды?"
,"Сеның есымын неге «Шолпан»?"
,"Есымын туралы баяндап бершы"
,"Артымнан ершы"
,"токта"
,"Сены кым жасады?"
        };
        private void init_speech()
        {
            this.label1.Content = "like";
            ReadOnlyCollection<RecognizerInfo> a = SpeechRecognitionEngine.InstalledRecognizers();
            _recinfo = a[1];
            _recognizer = new SpeechRecognitionEngine(_recinfo);
            Choices cmd = new Choices();
            string[] d = Directory.GetFiles("wavkaz");
            wavstream = new Stream[d.Length];
            for (int i = 0; i < comms.Length; i++)
            {

                cmd.Add(comms[i]);
            }
            _gb = new GrammarBuilder(cmd);
            _gb.Culture = _recinfo.Culture;
            _recognizer.LoadGrammar(new Grammar(_gb));
            _synth = new SpeechSynthesizer();
            _synth.SetOutputToDefaultAudioDevice();

            _recognizer.SetInputToDefaultAudioDevice();

            _recognizer.RecognizeAsync(RecognizeMode.Multiple);
            _recognizer.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(_recognizer_SpeechRecognized);
        }

        /// <summary>
        /// Handles the AllFramesReady event of the kinectSensor control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Microsoft.Kinect.AllFramesReadyEventArgs"/> instance containing the event data.</param>
        void _recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {

            // for (int j, x, i = o.length; i; j = parseInt(Math.random() * i), x = o[--i], o[i] = o[j], o[j] = x) ;
            labeluf.Content = e.Result.Confidence.ToString();
            labeluf.Content = labeluf.Content + e.Result.Text;

            if (e.Result.Confidence > 0.62)
            {
                playsound(e.Result.Text);
                if (e.Result.Text == comms[comms.Length - 3])
                {
                    isActive = true;
                    FIXED_DISTANCE = distance;
                    slejenie = true;
               
                }
                if (e.Result.Text == comms[comms.Length - 2]) {
                    isActive = false;
                }
            }
        }
        void playsound(String c) {

            int i;
            for (i = 0; i < comms.Length; i++)
            {
                if (comms[i] == c)
                {
                    break;
                }
            }
            if (sndp.Stream != null)
                sndp.Stream.Close();
            int rand = rend.Next();
            rand = rend.Next(0, ansind[indexor[i]]);

            if (indexor[i] == last)
            {
                if (massiv.Count >= ansind[indexor[i]])
                {
                    massiv.Clear();
                    rand = rend.Next(0, ansind[indexor[i]]);
                    massiv.Add(rand);
                }
                else
                {
                    while (massiv.Count(s => s == rand) >= 1)
                        rand = rend.Next(0, ansind[indexor[i]]);
                    {
                        massiv.Add(rand);
                    }
                }
            }
            else
            {
                last = indexor[i];
                rand = rend.Next(0, ansind[indexor[i]]);
                massiv.Clear();
                massiv.Add(rand);
            }
            String temp = "wavkaz\\" + (indexor[i]).ToString() + massiv.Last().ToString();

            sndp.Stream = new FileStream(temp + ".wav", FileMode.Open);
            //raimbek?
            // 
            String comand = "m";
            Object[] param = new Object[1];
            param[0] = comand;
            this.Dispatcher.BeginInvoke(new invoker(sendcom), param);
            sndp.Play();
        
        }
        void sendcom(String a)
        {
            serialP.WriteLine(a);
        }
        delegate void invoker(String a);
        #endregion
        void kinectSensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            // Retrieve each single frame and copy the data
            using (ColorImageFrame colorImageFrame = e.OpenColorImageFrame())
            {
                if (colorImageFrame == null)
                    return;
                colorImageFrame.CopyPixelDataTo(colorPixelData);
                //int strade = colorImageFrame.Width * 4;
                //image1.Source = BitmapSource.Create(colorImageFrame.Width, colorImageFrame.Height, 96, 96,
                //                                    PixelFormats.Bgr32, null, colorPixelData, strade);
            }

            using (DepthImageFrame depthImageFrame = e.OpenDepthImageFrame())
            {
                if (depthImageFrame == null)
                    return;
                depthImageFrame.CopyPixelDataTo(depthPixelData);
            }

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame == null)
                    return;
                skeletonFrame.CopySkeletonDataTo(skeletonData);
            }

            // Retrieve the first tracked skeleton if any. Otherwise, do nothing.
            var skeleton = skeletonData.FirstOrDefault(s => s.TrackingState == SkeletonTrackingState.Tracked);
            if (skeleton == null && !sessionClose)
            {
                serialP.WriteLine("s");
                serialP.WriteLine("c");
                serialP.WriteLine("p");
                serialP.WriteLine("g");
                if (isActive) isActive = false;

                slejenie = false;
                activatorRightHand = 0;
                activatorLeftHand = false;
                firstMeet = false;

                sessionClose = true;
                return;
            }
            else if (skeleton != null && !firstMeet) 
            {
                serialP.WriteLine("i");
                playsound(comms[0]);
                firstMeet = true;
                sessionClose = false;
            }
            if (sessionClose) {
                return;
            }
            // Make the faceTracker processing the data.
            FaceTrackFrame faceFrame = faceTracker.Track(kinectSensor.ColorStream.Format, colorPixelData,
                                              kinectSensor.DepthStream.Format, depthPixelData,
                                              skeleton);
            
            EnumIndexableCollection<FeaturePoint, PointF> facePoints = faceFrame.GetProjected3DShape();

            
            // points of hands and shoulder - to determine HELLO, etc.
            Joint shoulderCenter = skeleton.Joints[JointType.ShoulderCenter];
            Joint head = skeleton.Joints[JointType.Head];
            Joint rightHand = skeleton.Joints[JointType.HandRight];
            Joint leftHand = skeleton.Joints[JointType.HandLeft];
            
            // initialize sound for hello
            //SoundPlayer a = new SoundPlayer("C:\\sal.wav");

            
            // open stream for uart reading
            //serialP.Open();

            // points of lip's corner - with help of this I determine smile
            double x1 = facePoints[88].X;
            double y1 = facePoints[88].Y;
            System.Windows.Point leftLip = new System.Windows.Point(x1, y1);
            double x2 = facePoints[89].X;
            double y2 = facePoints[89].Y;
            System.Windows.Point rightLip = new System.Windows.Point(x2, y2);
            Vector subtr = System.Windows.Point.Subtract(leftLip, rightLip);
            
            // distance between kinect and human
            distance = skeleton.Position.Z*100;

            // distance between two corners of lip
            double length = Math.Sqrt(subtr.X * subtr.X + subtr.Y * subtr.Y);

            int check = 100;
            
            double angle1 = 0d;
            double angle2 = 0d;
            double angle = skeleton.Position.X*100;

            #region "Smile deterine"
            if (distance >= 95 && distance < 110)
            {
                check = 22;
            }
            else if (distance >= 110 && distance < 120)
            {
                check = 19;
            }
            else if (distance >= 120 && distance < 130)
            {
                check = 18;
            }
            else if (distance >= 130 && distance < 140)
            {
                check = 17;
            }
            else if (distance >= 140 && distance < 150)
            {
                check = 16;
            }
            else if (distance >= 150 && distance < 160)
            {
                check = 14;
            }
            else if (distance >= 160 && distance < 170)
            {
                check = 13;
            }
            else if (distance >= 170 && distance < 180)
            {
                check = 12;
            }
            else if (distance >= 180 && distance < 190)
            {
                check = 11;
            }

            #endregion

            #region "Angle"
            if (distance >= 90 && distance < 110)
            {
                angle1 = -15;
                angle2 = 15;
            }
            else if (distance >= 110 && distance < 150)
            {
                angle1 = -20;
                angle2 = 20;
            }
            else if (distance >= 150 && distance < 170)
            {
                angle1 = -30;
                angle2 = 30;
            }
            else if (distance >= 170 && distance < 200)
            {
                angle1 = -35;
                angle2 = 35;
            }
            else if (distance >= 200)
            {
                angle1 = -40;
                angle2 = 40;
            }
            #endregion

            double condition1 = Math.Abs(leftHand.Position.Z*100 - shoulderCenter.Position.Z*100);
            double condition2 = Math.Abs(rightHand.Position.Z*100 - shoulderCenter.Position.Z*100);

            // If position of two hands higher than shoulder it's activate 'slejenie za ob'ektom'
            if (condition1 > 45 
                && condition2 > 45
                && leftHand.Position.X < rightHand.Position.X) 
            {
                if (!slejenie)
                {
                    isActive = true;
                    FIXED_DISTANCE = distance;
                    slejenie = true;
                }
            }
            
            // The command to stop 'slejenie za ob'ektom'
            if (leftHand.Position.X > rightHand.Position.X)
            {
                    isActive = false;
            }

            // Slejenie za ob'ektom
            if (isActive)
            {
                int pinkIs = (int)typeCondition.THIRD;
                int purpleIs = (int)typeCondition.FORTH;
                int redIs = (int)typeCondition.FIVTH;
                int yellowIs = (int)typeCondition.SIXTH;
                
                if (distance > FIXED_DISTANCE + 10.0d)
                {
                    if (angle < angle1)
                    {
                        ellipseSmile.Fill = Brushes.Pink;
                        if (currentAction != pinkIs)//povorot na pravo
                        {
                            currentAction = pinkIs;
                            serialP.WriteLine("r");
                        }
                    }
                    else if (angle > angle2)//povorot na levo
                    {
                        ellipseSmile.Fill = Brushes.Purple;
                        if (currentAction != purpleIs)
                        {
                            currentAction = purpleIs;
                            serialP.WriteLine("l");
                        }
                    }
                    else
                    {
                        ellipseSmile.Fill = Brushes.Red;
                        if (currentAction != redIs)// vpered
                        {
                            currentAction = redIs;
                           serialP.WriteLine("f");
                        }
                    }
                }
                else if (distance > 90)
                {
                    if (angle < angle1)
                    {
                        ellipseSmile.Fill = Brushes.Pink;
                        if (currentAction != pinkIs)//na pravo
                        {
                            currentAction = pinkIs;
                            serialP.WriteLine("r");
                        }
                    }
                    else if (angle > angle2)
                    {
                        ellipseSmile.Fill = Brushes.Purple;
                        if (currentAction != purpleIs)// na levo
                        {
                            currentAction = purpleIs;                           
                            serialP.WriteLine("l");
                        }
                    }
                    else
                    {
                        ellipseSmile.Fill = Brushes.Yellow;
                        if (currentAction != yellowIs)//stop, ili - do nothing
                        {
                            currentAction = yellowIs;
                           serialP.WriteLine("s");
                       }
                    }
                }
                else {
                    ellipseSmile.Fill = Brushes.Yellow;
                    if (currentAction != yellowIs)//stop, ili - do nothing
                    {
                        currentAction = yellowIs;
                        serialP.WriteLine("s");
                    }
                }
            }


            // esli 'slejenie za ob'ektom' otklu4en
            else if(!isActive) 
            {
                int blueIs = (int)typeCondition.FIRST;
                int blackIs = (int)typeCondition.SECOND;
                int onkol = (int)typeCondition.SEVENTH;

                if (leftHand.Position.Y > head.Position.Y && rightHand.Position.Y < shoulderCenter.Position.Y)
                {
                    ellipseSmile.Fill = Brushes.Blue;
                    if (currentAction != blueIs && !activatorLeftHand)//privet levoi rukoi ----------------------------------------------------------------------------
                    
                    {
                        currentAction = blueIs;
                        serialP.WriteLine("q");
                        activatorLeftHand = true;
                    }

                }

                else if (rightHand.Position.Y > head.Position.Y && leftHand.Position.Y < shoulderCenter.Position.Y)
                {

                    ellipseSmile.Fill = Brushes.Blue;
                    if (currentAction != onkol && activatorRightHand != 12)//privet pravoi rukoi   -----------------------------------------------------------------------------
                    {
                        currentAction = onkol;
                        serialP.WriteLine("w");
                        activatorRightHand = 12;
                    }
                }
                
                else
                {
                    ellipseSmile.Fill = Brushes.Black;
                    if (currentAction != blackIs)// toktaidy ili do nothing
                    {
                        currentAction = blackIs;
                       serialP.WriteLine("s");
                    }


                    if (currentAction == blackIs)
                    {

                        if (length >= check && currentFace != (int)faceConditions.FIRST)
                        {
                            serialP.WriteLine("z"); // smile
                            currentFace = (int)faceConditions.FIRST;
                            ellipseSmile.Fill = Brushes.Brown;
                            
                        }
                        else if (length < check && currentFace != (int)faceConditions.SECOND)
                        {
                            serialP.WriteLine("x"); // poker face
                            currentFace = (int)faceConditions.SECOND;
                            ellipseSmile.Fill = Brushes.Gold;
                        }

                        #region "povoroti golovoi"
                        if (angle < angle1)
                        {
                            ellipseSmile.Fill = Brushes.Pink;
                            if (!headToRight)//povorot golovi na pravo
                            {
                                headToRight = true;
                                headToCenter = false;
                                headToLeft = false;
                                serialP.WriteLine("k");
                            }
                        }
                        else if (angle > angle2)//povorot golovi na levo
                        {
                            if (!headToLeft)
                            {
                                headToLeft = true;
                                headToCenter = false;
                                headToRight = false;
                                serialP.WriteLine("j");
                            }
                        }
                        else if (angle < angle2 && angle > angle1)//golova v centre
                        {
                            if (!headToCenter)
                            {
                                headToCenter = true;
                                headToRight = false;
                                headToLeft = false;
                                serialP.WriteLine("p");
                            }
                        }
                        #endregion

                    }
                    else if (!faceFrame.TrackSuccessful && currentFace != (int)faceConditions.NONE) 
                    {
                        serialP.WriteLine("c"); // sad face
                        currentFace = (int)faceConditions.NONE;
                        ellipseSmile.Fill = Brushes.Chocolate;
                    }
                    
                }

            }

            label2.Content = distance.ToString();
            //label1.Content = (leftHand.Position.Z * 100).ToString();
            //label3.Content = (shoulderCenter.Position.Z * 100).ToString();

            //serialP.Close();
        }
    }
}
