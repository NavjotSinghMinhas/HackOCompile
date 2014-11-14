using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace HackOCompile
{
    public partial class MainWindow : Window
    {

        #region Declerations

        string[] CodeFormat = new string[12];

        public class Compile
        {
            public Dictionary<string, object> errors { get; set; }
            public Dictionary<string, object> run_status { get; set; }
            public string code_id { get; set; }
            public string web_link { get; set; }
            public string compile_status { get; set; }
            public int async { get; set; }
            public string message { get; set; }
            public string id { get; set; }
        }

        public class Run
        {
            public Dictionary<string, object> errors { get; set; }
            public string code_id { get; set; }
            public string web_link { get; set; }
            public string compile_status { get; set; }
            public string id { get; set; }
            public int async { get; set; }
            public string message { get; set; }
            public Dictionary<string, object> run_status { get; set; }
        }

        public class InternetChecker
        {
            [System.Runtime.InteropServices.DllImport("wininet.dll")]
            private extern static bool InternetGetConnectedState(out int Description, int ReservedValue);
            public static bool IsConnectedToInternet()
            {
                int Desc;
                return InternetGetConnectedState(out Desc, 0);
            }
        }

        public Compile CompileObject;
        public Run RunObject;

        string SaveURL = "";
        string code = "";
        string lang = "";
        string postData = "";
        int memory = 0;
        int time = 0;
        bool compiled = false;
        bool ran = false;
        bool IntenetConnected = false;
        DispatcherTimer timer1;
        DispatcherTimer InternetCheckerTimer;
        int MessageTicker = 0;

        #endregion

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            timer1 = new DispatcherTimer();
            InternetCheckerTimer = new DispatcherTimer();
            InternetCheckerTimer.Interval = TimeSpan.FromSeconds(1);
            InternetCheckerTimer.Tick += InternetCheckerTimer_Tick;
            InternetCheckerTimer.Start();

            Language.Items.Add("C");
            Language.Items.Add("C++");
            Language.Items.Add("C++11");
            Language.Items.Add("Clojure");
            Language.Items.Add("C#");
            Language.Items.Add("Java");
            Language.Items.Add("JavaScript");
            Language.Items.Add("Haskell");
            Language.Items.Add("Perl");
            Language.Items.Add("PHP");
            Language.Items.Add("Python");
            Language.Items.Add("Ruby");

            try
            {
                var Temp = File.OpenText(System.AppDomain.CurrentDomain.BaseDirectory + "\\Backup.txt").ReadToEnd();
                source.AppendText(Temp);
            }
            catch (Exception) { }
        }

        void InternetCheckerTimer_Tick(object sender, EventArgs e)
        {
            if (InternetChecker.IsConnectedToInternet())
            {
                Offline.Visibility = Visibility.Collapsed;
                Online.Visibility = Visibility.Visible;
            }
            else
            {
                Online.Visibility = Visibility.Collapsed;
                Offline.Visibility = Visibility.Visible;
            }

            if (MessageTicker % 2 == 0 && MessageTicker < 31)
            {
                Message.Visibility = Visibility.Visible;
                MessageTicker++;
            }
            else
            {
                Message.Visibility = Visibility.Collapsed;
                MessageTicker++;
            }
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {                    
            CodeFormat[0] = "#include <stdio.h>\r\n\r\nint main()\r\n{\r\n\t//Enter your code here\r\n\treturn 0;\r\n}";
            CodeFormat[1] = "#include <iostream>\r\nusing namespace std;\r\n\r\nint main()\r\n{\r\n\t//Enter your code here\r\n\treturn 0;\r\n}";
            CodeFormat[2] = "#include <iostream>\r\nusing namespace std;\r\n\r\nint main()\r\n{\r\n\t//Enter your code here\r\n\treturn 0;\r\n}";
            CodeFormat[3] = "using System;\r\nclass MyClass\r\n{\r\n\tstatic void Main(string[] args)\r\n\t{\r\n\t\t//Enter your code here\r\n\t}\r\n}";
            CodeFormat[4] = "//Enter your code here";
            CodeFormat[5] = "class TestClass\r\n{\r\n\tpublic static void main(String args[] ) throws Exception\r\n\t{\r\n\t\t//Enter your code here\r\n\t}\r\n}";
            CodeFormat[6] = "<?php\r\n\t//Enter your code here\r\n?>";
        }

        #region Compilation
        private void Compile_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (Language.SelectedItem != null)
            {
                Status.Foreground = new SolidColorBrush(Colors.Black);
                Status.Text = "Compiling...";
                Uri myUri = new Uri("http://api.hackerearth.com/code/compile/");
                HttpWebRequest myRequest = (HttpWebRequest)HttpWebRequest.Create(myUri);
                myRequest.Method = "POST";
                code = source.Text;

                timer1.Interval = TimeSpan.FromMilliseconds(10);
                timer1.Tick += timer1_Tick;
                timer1.Start();
                
                postData = "lang=" + lang;
                postData += "&async=0";
                postData += "&client_secret=7a391f1616d01b6b6f27cabb04a04438238ab0bb";
                if (Input.Text != "")
                    postData += "&input=" + Input.Text;

                postData += "&source=" + System.Net.WebUtility.UrlEncode(code);

                myRequest.BeginGetRequestStream(new AsyncCallback(GetCompileRequest), myRequest);
            }
            else
            {
                Status.Text = "Please select a language first";
                Status.Foreground = new SolidColorBrush(Colors.Red);
            }
        }

        void GetCompileRequest(IAsyncResult callbackResult)
        {
            try
            {
                HttpWebRequest myRequest = (HttpWebRequest)callbackResult.AsyncState;

                Stream postStream = myRequest.EndGetRequestStream(callbackResult);
                byte[] byteArray = Encoding.UTF8.GetBytes(postData);
                postStream.Write(byteArray, 0, byteArray.Length);
                postStream.Close();

                myRequest.BeginGetResponse(new AsyncCallback(RunCompile), myRequest);
            }
            catch (Exception)
            {
                Status.Text = "";
                Output.Text = "";
                memory_used.Text = "";
                time_used.Text = "";
                Status.Text = "Network Error!";
                Status.Foreground = new SolidColorBrush(Colors.Red);
            }
        }

        void RunCompile(IAsyncResult callbackResult)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)callbackResult.AsyncState;
                HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(callbackResult);
                using (StreamReader httpWebStreamReader = new StreamReader(response.GetResponseStream()))
                {
                    string result = httpWebStreamReader.ReadToEnd();
                    CompileObject = JsonConvert.DeserializeObject<Compile>(result); ;
                    compiled = true;
                }
            }
            catch (Exception e)
            {
                compiled = false;
                timer1.Stop();
            }
        }
        #endregion

        #region Run
        private void Run_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (Language.SelectedItem != null)
            {
                Status.Foreground = new SolidColorBrush(Colors.Black);
                Status.Text = "Executing...";
                Uri myUri = new Uri("http://api.hackerearth.com/code/run/");
                HttpWebRequest myRequest = (HttpWebRequest)HttpWebRequest.Create(myUri);
                myRequest.Method = "POST";
                code = source.Text;

                timer1.Interval = TimeSpan.FromMilliseconds(10); ;
                timer1.Tick += timer1_Tick;
                timer1.Start();

                string postData = "lang=" + lang;
                postData += "&async=0";
                postData += "&client_secret=7a391f1616d01b6b6f27cabb04a04438238ab0bb";
                if (Input.Text != "")
                    postData += "&input=" + Input.Text;

                myRequest.BeginGetRequestStream(new AsyncCallback(GetExecutionRequest), myRequest);
            }
            else
            {
                Status.Text = "Please select a language first";
                Status.Foreground = new SolidColorBrush(Colors.Red);
            }
        }

        void GetExecutionRequest(IAsyncResult callbackResult)
        {
            try
            {
                HttpWebRequest myRequest = (HttpWebRequest)callbackResult.AsyncState;

                Stream postStream = myRequest.EndGetRequestStream(callbackResult);
                postData += "&source=" + System.Net.WebUtility.UrlEncode(code);
                byte[] byteArray = Encoding.UTF8.GetBytes(postData);
                postStream.Write(byteArray, 0, byteArray.Length);
                postStream.Close();

                myRequest.BeginGetResponse(new AsyncCallback(RunExecution), myRequest);
            }
            catch (Exception)
            {
                Status.Text = "";
                Output.Text = "";
                memory_used.Text = "";
                time_used.Text = "";
                Status.Text = "Network Error!";
                Status.Foreground = new SolidColorBrush(Colors.Red);
            }
        }

        void RunExecution(IAsyncResult callbackResult)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)callbackResult.AsyncState;
                HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(callbackResult);
                using (StreamReader httpWebStreamReader = new StreamReader(response.GetResponseStream()))
                {
                    string result = httpWebStreamReader.ReadToEnd();
                    RunObject = JsonConvert.DeserializeObject<Run>(result); ;
                    ran = true;
                }
            }
            catch (Exception e)
            {
                ran = false;
                timer1.Stop();
            }
        }
        #endregion

        #region Other Functions

        private void Language_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string Input = source.Text;
            if (Language.SelectedItem != null)
            {
                if (Language.SelectedItem.ToString() == "C")
                {
                    lang = "C";
                    source.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition("C++");
                    source.Text = CodeFormat[0];
                }
                else if (Language.SelectedItem.ToString() == "C++")
                {
                    lang = "CPP";
                    source.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition("C++");
                    source.Text = CodeFormat[1];
                }
                else if (Language.SelectedItem.ToString() == "C++11")
                {
                    lang = "CPP11";
                    source.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition("C++");
                    source.Text = CodeFormat[2];
                }
                else if (Language.SelectedItem.ToString() == "C#")
                {
                    lang = "CSHARP";
                    source.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition("C#");
                    source.Text = CodeFormat[3];
                }
                else if (Language.SelectedItem.ToString() == "Clojure")
                {
                    lang = "CLOJURE";
                    source.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition("C#");
                    source.Text = CodeFormat[4];
                }
                else if (Language.SelectedItem.ToString() == "Java")
                {
                    lang = "JAVA";
                    source.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition("Java");
                    source.Text = CodeFormat[5];
                }
                else if (Language.SelectedItem.ToString() == "JavaScript")
                {
                    lang = "JAVASCRIPT";
                    source.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition("JavaScript");
                    source.Text = CodeFormat[4];
                }
                else if (Language.SelectedItem.ToString() == "Haskell")
                {
                    lang = "HASKELL";
                    source.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition("c#");
                    source.Text = CodeFormat[4];
                }
                else if (Language.SelectedItem.ToString() == "Perl")
                {
                    lang = "PERL";
                    source.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition("c#");
                    source.Text = CodeFormat[4];
                }
                else if (Language.SelectedItem.ToString() == "PHP")
                {
                    lang = "PHP";
                    source.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition("PHP");
                    source.Text = CodeFormat[6];
                }
                else if (Language.SelectedItem.ToString() == "Python")
                {
                    lang = "PYTHON";
                    source.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition("c#");
                    source.Text = CodeFormat[4];
                }
                else if (Language.SelectedItem.ToString() == "Ruby")
                {
                    lang = "RUBY";
                    source.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition("c#");
                    source.Text = CodeFormat[4];
                }
            }
        }

        void timer1_Tick(object sender, EventArgs e)
        {
            if (compiled == true)
            {
                switch (CompileObject.compile_status)
                {
                    case "OK": Status.Text = "Compile Successful!";
                        Status.Foreground = new SolidColorBrush(Colors.Green);
                        Output.Text = "";
                        break;
                    default: Status.Text = "Error Occured!";
                        Output.Text = CompileObject.compile_status;
                        Status.Foreground = new SolidColorBrush(Colors.Red);
                        break;
                }
                timer1.Stop();
                compiled = false;
            }
            if (CompileObject != null)
            {
                try
                {
                    memory_used.Text = "run to check memory usage";
                    time_used.Text = CompileObject.run_status["time_used"] + " Sec out of " + CompileObject.run_status["time_limit"] + " Sec";
                    CompileObject = null;
                }
                catch (Exception) { CompileObject = null; }
            }

            else if (ran == true)
            {
                switch (RunObject.compile_status)
                {
                    case "OK": Status.Text = "Compile Successful!";
                        Status.Foreground = new SolidColorBrush(Colors.Green);
                        Output.Text = RunObject.run_status["output"].ToString();
                        break;
                    default: Status.Text = "Error Occured!";
                        Output.Text = RunObject.compile_status;
                        Status.Foreground = new SolidColorBrush(Colors.Red);
                        break;
                }
                timer1.Stop();
                ran = false;
            }
            if (RunObject != null)
            {
                try
                {
                    memory_used.Text = int.Parse(RunObject.run_status["memory_used"].ToString()) > int.Parse(RunObject.run_status["memory_limit"].ToString()) ? "Unknown" : RunObject.run_status["memory_used"].ToString() + " KB out of " + RunObject.run_status["memory_limit"].ToString() + " Mb";
                    time_used.Text = RunObject.run_status["time_used"] + " Sec out of " + RunObject.run_status["time_limit"] + " Sec";
                    RunObject = null;
                }
                catch (Exception) { RunObject = null; }
            }
        }

        private void Navbar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void Close_MouseEnter(object sender, MouseEventArgs e)
        {
            Close.Background = new SolidColorBrush(Color.FromRgb(224, 67, 67));
        }

        private void Close_MouseLeave(object sender, MouseEventArgs e)
        {
            Close.Background = new SolidColorBrush(Color.FromRgb(199, 80, 80));
        }

        private void Close_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            MessageBoxResult key = MessageBox.Show("Are you sure you want to quit","Confirm",MessageBoxButton.YesNo,MessageBoxImage.Warning,MessageBoxResult.No);//TODO , check if it is saved then show the messg accrdngly
            if (key == MessageBoxResult.No)
            {
                return;
            }
            else
            {
                try
                {
                    File.WriteAllText(System.AppDomain.CurrentDomain.BaseDirectory + "\\Backup.txt", source.Text);
                    Application.Current.Shutdown();
                }
                catch (Exception) { }
            }
        }

        private void Minimize_MouseEnter(object sender, MouseEventArgs e)
        {
            Minimize.Background = new SolidColorBrush(Color.FromRgb(238, 238, 238)); ;
            MinimizeLabel.Foreground = new SolidColorBrush(Colors.Black);
        }

        private void Minimize_MouseLeave(object sender, MouseEventArgs e)
        {
            Minimize.Background = new SolidColorBrush(Colors.LightGray);
            MinimizeLabel.Foreground = new SolidColorBrush(Colors.White);
        }

        private void Minimize_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            About Object = new About();
            Object.Show();
        }

        private void Save_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            string Input = source.Text;
            if (Input != "")
            {
                if (Input != CodeFormat[0] && Input != CodeFormat[1] && Input != CodeFormat[2] && Input != CodeFormat[3] && Input != CodeFormat[4] && Input != CodeFormat[5] && Input != CodeFormat[9])
                {
                    if (SaveURL == "")
                    {
                        Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog();
                        if (Language.SelectedItem.ToString() == "C")
                            dialog.DefaultExt = "*.C";
                        else if (Language.SelectedItem.ToString() == "C++" || Language.SelectedItem.ToString() == "C++11")
                            dialog.DefaultExt = "*.CPP";
                        else if (Language.SelectedItem.ToString() == "Clojure")
                            dialog.DefaultExt = "*.clj";
                        else if (Language.SelectedItem.ToString() == "C#")
                            dialog.DefaultExt = "*.cs";
                        else if (Language.SelectedItem.ToString() == "Java")
                            dialog.DefaultExt = "*.java";
                        else if (Language.SelectedItem.ToString() == "JavaScript")
                            dialog.DefaultExt = "*.js";
                        else if (Language.SelectedItem.ToString() == "Haskell")
                            dialog.DefaultExt = "*.hs";
                        else if (Language.SelectedItem.ToString() == "Perl")
                            dialog.DefaultExt = "*.pl";
                        else if (Language.SelectedItem.ToString() == "PHP")
                            dialog.DefaultExt = "*.php";
                        else if (Language.SelectedItem.ToString() == "Python")
                            dialog.DefaultExt = "*.py";
                        else if (Language.SelectedItem.ToString() == "Ruby")
                            dialog.DefaultExt = "*.rb";
                        dialog.Filter = "Text Files (*.txt)|*.txt|C Files (*.c)|*.c|C++ Files (*.cpp)|*.cpp|C# Files (*.cs)|*.cs|Java Files (*.java)|*.java|JavaScript Files (*.js)|*.js|Haskell Files (*.hs)|*.hs|Python Files (*.py)|*.py|PHP Files (*.php)|*.php|Perl Files (*.pl)|*.pl|Ruby Files (*.rb)|*.rb|Clojure Files (*.clj)|*.clj";

                        Nullable<bool> result = dialog.ShowDialog();

                        if (result == true)
                        {
                            SaveURL = dialog.FileName;
                            try
                            {
                                File.WriteAllText(dialog.FileName, source.Text);
                            }
                            catch (Exception) { MessageBox.Show("Can't save the file right now, try again!", "Error in saving"); }
                        }
                    }
                    else
                    {
                        try
                        {
                            File.WriteAllText(SaveURL, source.Text);
                        }
                        catch (Exception) { MessageBox.Show("Can't save the file now, try again!", "Error in saving the file"); }
                    }
                }
            }
        }

        private void Save_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            string Input = source.Text;
            if (Input != "")
            {
                if (Input != CodeFormat[0] && Input != CodeFormat[1] && Input != CodeFormat[2] && Input != CodeFormat[3] && Input != CodeFormat[4] && Input != CodeFormat[5] && Input != CodeFormat[9])
                {
                    e.CanExecute = true;
                }
            }
        }

        private void New_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            string Input = source.Text;
            if (Input != "")
            {
                if (Input != CodeFormat[0] && Input != CodeFormat[1] && Input != CodeFormat[2] && Input != CodeFormat[3] && Input != CodeFormat[4] && Input != CodeFormat[5] && Input != CodeFormat[9])
                {
                    e.CanExecute = true;
                }
            }
        }

        private void New_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SaveURL = "";
            Language.SelectedIndex = -1;
            source.Text = "";
        }

        private void Open_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".txt";
            dlg.Filter = "Text Files (*.txt)|*.txt|C Files (*.c)|*.c|C++ Files (*.cpp)|*.cpp|C# Files (*.cs)|*.cs|Java Files (*.java)|*.java|JavaScript Files (*.js)|*.js|Haskell Files (*.hs)|*.hs|Python Files (*.py)|*.py|PHP Files (*.php)|*.php|Perl Files (*.pl)|*.pl|Ruby Files (*.rb)|*.rb|Clojure Files (*.clj)|*.clj";
            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                var Reader = File.OpenText(dlg.FileName);
                source.Text = Reader.ReadToEnd();
            }
        }

        private void SaveAs_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog();
            if (Language.SelectedItem.ToString() == "C")
                dialog.DefaultExt = ".C";
            else if (Language.SelectedItem.ToString() == "C++" || Language.SelectedItem.ToString() == "C++11")
                dialog.DefaultExt = ".CPP";
            else if (Language.SelectedItem.ToString() == "Clojure")
                dialog.DefaultExt = ".clj";
            else if (Language.SelectedItem.ToString() == "C#")
                dialog.DefaultExt = ".cs";
            else if (Language.SelectedItem.ToString() == "Java")
                dialog.DefaultExt = ".java";
            else if (Language.SelectedItem.ToString() == "JavaScript")
                dialog.DefaultExt = ".js";
            else if (Language.SelectedItem.ToString() == "Haskell")
                dialog.DefaultExt = ".hs";
            else if (Language.SelectedItem.ToString() == "Perl")
                dialog.DefaultExt = ".pl";
            else if (Language.SelectedItem.ToString() == "PHP")
                dialog.DefaultExt = ".php";
            else if (Language.SelectedItem.ToString() == "Python")
                dialog.DefaultExt = ".py";
            else if (Language.SelectedItem.ToString() == "Ruby")
                dialog.DefaultExt = ".rb";
            dialog.Filter = dialog.Filter = "Text Files (*.txt)|*.txt|C Files (*.c)|*.c|C++ Files (*.cpp)|*.cpp|C# Files (*.cs)|*.cs|Java Files (*.java)|*.java|JavaScript Files (*.js)|*.js|Haskell Files (*.hs)|*.hs|Python Files (*.py)|*.py|PHP Files (*.php)|*.php|Perl Files (*.pl)|*.pl|Ruby Files (*.rb)|*.rb|Clojure Files (*.clj)|*.clj";

            Nullable<bool> result = dialog.ShowDialog();

            if (result == true)
            {
                try
                {
                    File.WriteAllText(dialog.FileName, source.Text);
                }
                catch (Exception) { MessageBox.Show("Can't save the file right now, try again!", "Error in saving"); }
            }
        }

        private void SaveAs_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            string Input = source.Text; 
            if (Input != "")
            {
                if (Input != CodeFormat[0] && Input != CodeFormat[1] && Input != CodeFormat[2] && Input != CodeFormat[3] && Input != CodeFormat[4] && Input != CodeFormat[5] && Input != CodeFormat[9])
                {
                    e.CanExecute = true;
                }
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            MessageBoxResult key = MessageBox.Show("Are you sure you want to quit", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);//TODO , check if it is saved then show the messg accrdngly
            if (key == MessageBoxResult.No)//TODO
            {
                return;
            }
            else
            {
                Application.Current.Shutdown();
            }
        }

        private void source_MouseEnter(object sender, MouseEventArgs e)
        {
            SourceImage1.Source = new BitmapImage(new Uri(@"Images/BlueSeperator.png",UriKind.Relative));
            SourceImage2.Source = new BitmapImage(new Uri(@"Images/BlueSeperator.png",UriKind.Relative));
            SourceImage3.Source = new BitmapImage(new Uri(@"Images/BlueSeperator.png",UriKind.Relative));
            SourceImage4.Source = new BitmapImage(new Uri(@"Images/BlueSeperator.png", UriKind.Relative));
        }

        private void source_MouseLeave(object sender, MouseEventArgs e)
        {
            SourceImage1.Source = new BitmapImage(new Uri(@"Images/GraySeperator.png", UriKind.Relative));
            SourceImage2.Source = new BitmapImage(new Uri(@"Images/GraySeperator.png", UriKind.Relative));
            SourceImage3.Source = new BitmapImage(new Uri(@"Images/GraySeperator.png", UriKind.Relative));
            SourceImage4.Source = new BitmapImage(new Uri(@"Images/GraySeperator.png", UriKind.Relative));
        }

        private void Input_MouseEnter(object sender, MouseEventArgs e)
        {
            InputImage1.Source = new BitmapImage(new Uri(@"Images/BlueSeperator.png", UriKind.Relative));
            InputImage2.Source = new BitmapImage(new Uri(@"Images/BlueSeperator.png", UriKind.Relative));
            InputImage3.Source = new BitmapImage(new Uri(@"Images/BlueSeperator.png", UriKind.Relative));
        }

        private void Input_MouseLeave(object sender, MouseEventArgs e)
        {
            InputImage1.Source = new BitmapImage(new Uri(@"Images/GraySeperator.png", UriKind.Relative));
            InputImage2.Source = new BitmapImage(new Uri(@"Images/GraySeperator.png", UriKind.Relative));
            InputImage3.Source = new BitmapImage(new Uri(@"Images/GraySeperator.png", UriKind.Relative));
        }
        #endregion

        private void Run_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
    }
}
