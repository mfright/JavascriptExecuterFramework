using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;

namespace JavascriptExecuter
{
    public partial class Form1 : Form
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool PostMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        private static extern int VkKeyScan(char ch);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string pClassName, string pWindowName);


        public ChromiumWebBrowser chromeBrowser;

        // メインスレッド
        Thread mainThread;

        // Form reference for Anti-minimize.
        static Form1 myForm;

        // WindowState for Anti-minimize.
        static FormWindowState preWindowState;

        // Delegate.
        public delegate void myDelegate();

        // Remembered password list.
        List<Password> passwordList = new List<Password>();
        



        // Anti-minimize. (Because CefSharp doesn't work in minimize-window.)
        private void UpWindow()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new myDelegate(this.UpWindow));
                return;
            }

            if (this.WindowState == FormWindowState.Minimized)
            {
                myForm.WindowState = preWindowState;

                int height = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;

                this.SetBounds(0, height - 10, 0, 0, BoundsSpecified.Y);
            }

        }




        // Message for setText()
        string message = "";

        // Set text.
        public void setText()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new myDelegate(this.setText));
                return;
            }

            this.Text = "JavascriptExecuter - " + message;

        }





        // キーストロークの入力.
        char myChar = ' ';
        public void insertChar()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new myDelegate(this.insertChar));
                return;
            }



            KeyEvent k = new KeyEvent();
            k.WindowsKeyCode = (int)myChar;
            k.FocusOnEditableField = true;
            k.IsSystemKey = false;
            k.Type = KeyEventType.Char;
            chromeBrowser.GetBrowser().GetHost().SendKeyEvent(k);


        }


        // Close the form.
        public void formClose()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new myDelegate(this.formClose));
                return;
            }

            myForm.Close();

        }

        // Down window
        private void downWindow()
        {

            if (this.InvokeRequired)
            {
                this.Invoke(new myDelegate(this.downWindow));
                return;
            }

            int height = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;
            this.SetBounds(0, height - 10, 0, 0, BoundsSpecified.Y);



        }





        // Constructor.
        public Form1()
        {
            InitializeComponent();

            InitializeChromium();
            


            // Remember current WindowState.
            myForm = this;
            preWindowState = this.WindowState;
            

            // passwords.ini読み込み
            if (File.Exists(System.AppDomain.CurrentDomain.BaseDirectory + "\\passwords.ini")) 
            {
                StreamReader sreader = new StreamReader(System.AppDomain.CurrentDomain.BaseDirectory + "\\passwords.ini", Encoding.GetEncoding("SHIFT_JIS"));
                while (sreader.EndOfStream == false)
                {
                    string line = sreader.ReadLine();
                    if (line.Length > 3)
                    {
                        string[] elements = line.Split(',');
                        passwordList.Add(new Password(elements[0], elements[1], elements[2]));
                    }
                }
                sreader.Close();
            }
            

            // under.txtがある場合、ウィンドウを自動的に下へ隠す。
            if (File.Exists(System.AppDomain.CurrentDomain.BaseDirectory + "\\under.txt"))
            {
                downWindow();
            }


            // autorun.iniがある場合、自動キック
            if (File.Exists(System.AppDomain.CurrentDomain.BaseDirectory + "\\autorun.ini"))
            {
                btnRun.Text = "AUTO-RUN";
                btnRun.Enabled = false;

                Thread myThread = new Thread(new ThreadStart(() =>
                {
                    Thread.Sleep(60000);

                    btnRun_Click(null, null);
                }));

                // 上記スレッドを起動する。
                myThread.Start();
            }

        }

        public void InitializeChromium()
        {

            string defaultUrl = loadJScript(System.AppDomain.CurrentDomain.BaseDirectory + "\\defaulturl.ini");

            CefSettings settings = new CefSettings();
            settings.BrowserSubprocessPath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                                                   Environment.Is64BitProcess ? "x64" : "x86",
                                                   "CefSharp.BrowserSubprocess.exe");
            // ロケールを日本に
            settings.Locale = "ja";
            settings.AcceptLanguageList = "ja-JP";

            settings.CachePath = System.AppDomain.CurrentDomain.BaseDirectory + "\\cache";
            settings.PersistSessionCookies = true;

            Cef.Initialize(settings, performDependencyCheck: false, browserProcessHandler: null);

            chromeBrowser = new ChromiumWebBrowser(defaultUrl);

            // アドレス変わったときのイベントハンドラ追加
            chromeBrowser.AddressChanged += Browser_AddressChanged;
            

            this.Controls.Add(chromeBrowser);
            chromeBrowser.Dock = DockStyle.Fill;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {

            // メインスレッド停止
            try
            {
                mainThread.Abort();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            // CEF停止
            try
            {
                Cef.Shutdown();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


        // テキストファイルを行ごとに読み込む。　ファイルが無ければnullを返す。
        private List<string> loadText(string fileName)
        {
            string line = "";

            List<string> al = new List<string>();

            try
            {
                using (StreamReader sr = new StreamReader(fileName, Encoding.GetEncoding("UTF-8")))
                {

                    while ((line = sr.ReadLine()) != null)
                    {
                        al.Add(line);
                    }
                }

                return al;

            }
            catch (Exception ex)
            {
                return null;
            }
        }



        // ファイル全体を１行として読み込む。
        private String loadJScript(string fileName)
        {
            StreamReader sr = new StreamReader(fileName, Encoding.GetEncoding("Shift_JIS"));

            string text = sr.ReadToEnd();

            sr.Close();

            return text;
        }


        // HTMLコードを取得
        private string GetHTMLFromWebBrowser()
        {
            try
            {
                Task<String> taskHtml = chromeBrowser.GetBrowser().MainFrame.GetSourceAsync();

                string response = taskHtml.Result;
                return response;
            }catch(Exception ex)
            {

            }
            return "";
        }




        //カレントURL
        string currentURL = "";

        // ユーザ名とパスワードのクラス名
        string username = "";
        string password = "";
        Boolean BAC_isRunning = false;

        private void Browser_AddressChanged(object sender, AddressChangedEventArgs e)
        {
            currentURL = e.Address;
            setUrl();


            try
            {
                Thread myThread = new Thread(new ThreadStart(() =>
                {
                    // スレッドが既に起動中でなければ
                    if (!BAC_isRunning)
                    {
                        // スレッドを立てる
                        BAC_isRunning = true;

                        for (int looper = 0; looper < 20; looper++)
                        {
                            Thread.Sleep(1000);

                            ClassNames myClassNames = getClassNames();

                            if (myClassNames != null)
                            {
                                // もしパスワード入力画面ならば、

                                //パスワード入力フォームのクラス名を取得

                                string classNameUsername = myClassNames.classNameUsername;
                                string classNamePassword = myClassNames.classNamePassword;

                                // パスワードの記憶を探す
                                //Boolean found = false;
                                for (int i = 0; i < passwordList.Count; i++)
                                {
                                    Password mypassword = passwordList[i];
                                    
                                    if (currentURL.IndexOf(mypassword.url) > -1)
                                    {
                                        // パスワードの記憶があるとき                                        

                                        //MessageBox.Show(classNameUsername + " " + mypassword.userName + "\r\n" + classNamePassword + " " + mypassword.passstring);
                                            

                                        // ユーザー名入力欄を選択
                                        UpWindow();
                                        selectBrowser();
                                        chromeBrowser.ExecuteScriptAsync("var buttons = document.getElementsByName(\"" + classNameUsername + "\"); \r\n buttons[0].click();");
                                        chromeBrowser.ExecuteScriptAsync("var buttons = document.getElementsByName(\"" + classNameUsername + "\"); \r\n buttons[0].parentElement.click();");
                                        chromeBrowser.ExecuteScriptAsync("var buttons = document.getElementsByName(\"" + classNameUsername + "\"); \r\n buttons[0].parentElement.parentElement.click();");
                                        chromeBrowser.ExecuteScriptAsync("var buttons = document.getElementsByName(\"" + classNameUsername + "\"); \r\n buttons[0].parentElement.parentElement.parentElement.click();");


                                        // ユーザ名を入力(手入力)
                                        for (int w = 0; w < mypassword.userName.Length; w++)
                                        {
                                            UpWindow();
                                            selectBrowser();
                                            Thread.Sleep(50);
                                            
                                            myChar = mypassword.userName[w];
                                            insertChar();
                                        }

                                        // ユーザ名を入力(valueセット)
                                        Thread.Sleep(500);
                                        chromeBrowser.ExecuteScriptAsync("var buttons = document.getElementsByName(\"" + classNameUsername + "\"); if(buttons[0].value==''){ buttons[0].value='" + mypassword.userName + "'; } ");


                                        // パスワード入力欄を選択
                                        UpWindow();
                                        selectBrowser();
                                        chromeBrowser.ExecuteScriptAsync("var buttons = document.getElementsByName(\"" + classNamePassword + "\"); \r\n buttons[0].click();");
                                        chromeBrowser.ExecuteScriptAsync("var buttons = document.getElementsByName(\"" + classNamePassword + "\"); \r\n buttons[0].parentElement.click();");
                                        chromeBrowser.ExecuteScriptAsync("var buttons = document.getElementsByName(\"" + classNamePassword + "\"); \r\n buttons[0].parentElement.parentElement.click();");
                                        chromeBrowser.ExecuteScriptAsync("var buttons = document.getElementsByName(\"" + classNamePassword + "\"); \r\n buttons[0].parentElement.parentElement.parentElement.click();");


                                        // パスワードを入力(手入力)
                                        for (int w = 0; w < mypassword.passstring.Length; w++)
                                        {
                                            UpWindow();
                                            selectBrowser();
                                            Thread.Sleep(50);

                                            // 文字入力
                                            myChar = mypassword.passstring[w];
                                            insertChar();
                                        }

                                        // パスワードを入力(valueセット)
                                        Thread.Sleep(500);
                                        chromeBrowser.ExecuteScriptAsync("var buttons = document.getElementsByName(\"" + classNamePassword + "\"); if(buttons[0].value==''){ buttons[0].value='" + mypassword.passstring + "'; } ");


                                        // パスワードの記憶を探るループを抜ける
                                        break;
                                    }
                                }

                                // パスワードフォームに入力する処理を終えたらループを抜ける
                                break;
                            }
                        }

                        BAC_isRunning = false;
                    }
                    

                }));
                
                myThread.Start();
            }
            catch (Exception ex)
            {
                this.Invoke(new Action(() => { MessageBox.Show(this, ex.Message, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error); }));

                BAC_isRunning = false;
            }
        }




        // 文字列中の特定のキーワードの登場回数を数える
        private int countStr(string target, string keyword)
        {
            int count = 0;
            int nextpos = 0;

            for (count = 0; count < 100; count++)
            {
                nextpos = target.IndexOf(keyword);

                if (nextpos >= 0)
                {
                    target = target.Substring(nextpos + 1);
                }
                else
                {
                    break;
                }
            }

            return count;
        }
        

        private void btnRun_Click(object sender, EventArgs e)
        {
            try
            {
                mainThread = new Thread(new ThreadStart(() =>
                {
                    // メインスレッド
                    


                    


                    // スクリプトファイル一覧を取得
                    string myDir = System.AppDomain.CurrentDomain.BaseDirectory;
                    string[] files = System.IO.Directory.GetFiles(myDir, "jscript*.ini", System.IO.SearchOption.AllDirectories);

                    // インターバルの読み込み
                    StreamReader sr = new StreamReader(myDir + "\\intervalsec.ini", Encoding.GetEncoding("Shift_JIS"));
                    string intervalstr = sr.ReadToEnd();
                    sr.Close();
                    int interval = int.Parse(intervalstr) * 1000;


                    for (int i = 0; i < files.Length; i++)
                    {
                        message = "Running script: " + files[i];
                        setText();

                        UpWindow();

                        chromeBrowser.ExecuteScriptAsync(loadJScript(files[i]));

                        Thread.Sleep(interval);
                    }
                    


                    // 終了した旨表示。
                    message = "処理完了. ";
                    setText();
                    UpWindow();

                    

                    // autorun.iniがあれば、1分後にプログラム終了。
                    if (File.Exists(System.AppDomain.CurrentDomain.BaseDirectory + "\\autorun.ini"))
                    {

                        Thread.Sleep(60000);

                        formClose();

                        Application.Exit();
                    }

                }));

                // 上記スレッドを起動する。
                mainThread.Start();


            }
            catch (Exception ex)
            {
                this.Invoke(new Action(() => { MessageBox.Show(this, ex.Message, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error); }));
            }
        }




        private void btnGo_Click(object sender, EventArgs e)
        {
            chromeBrowser.Load(txtUrl.Text);
        }




        // Set text.
        public void setUrl()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new myDelegate(this.setUrl));
                return;
            }

            txtUrl.Text = currentURL;

        }


        // Select chromeBrowser
        public void selectBrowser()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new myDelegate(this.selectBrowser));
                return;
            }

            chromeBrowser.Select();

        }
        

        private void txtUrl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnGo_Click(this, null);
            }
        }

        private void btnRememberPassword_Click(object sender, EventArgs e)
        {
            /*
            // パスワード入力画面ではない場合は拒否する。
            string myHtml = GetHTMLFromWebBrowser();
            if(myHtml.IndexOf("type=\"password") == -1 || myHtml.IndexOf("type=\"text") == -1)
            {
                MessageBox.Show("There are no user-ID/password area in this page.\r\nユーザ名/パスワード入力画面が見つかりません.");
                return;
            }*/


            //入力されているユーザ名とパスワードを取得
            ClassNames myClassNames = getClassNames();            

            for(int i = 0; i < passwordList.Count; i++)
            {
                Password myPassword = passwordList[i];
                if (currentURL.IndexOf(myPassword.url) != -1)
                {
                    // 既に記憶されているURLの場合

                    MessageBox.Show("このURLは既にpasswords.iniに登録されています。リセットしたい場合はpasswords.iniを削除してください。");
                    return;
                }
            }

            // 記憶されていないURLの場合は、PasswordRecorderを表示
            PasswordRecorder myPasswordRecorder = new PasswordRecorder(currentURL);
            myPasswordRecorder.Show();


        }



        private ClassNames getClassNames()
        {
            string myHtml = GetHTMLFromWebBrowser();

            int indexPassword = myHtml.IndexOf("type=\"password");

            if (indexPassword > -1)
            {
                // もしパスワード入力画面ならば、

                //パスワード入力フォームのクラス名を取得

                string myHtmlByPass = myHtml.Substring(0, indexPassword);
                int indexPasswordTagStart = myHtmlByPass.LastIndexOf("<");

                int indexPasswordTagEnd = myHtml.IndexOf(">", indexPassword) + 1;

                string tagPassword = myHtml.Substring(indexPasswordTagStart, indexPasswordTagEnd - indexPasswordTagStart);
                //Console.WriteLine("tagPassword:" + tagPassword);

                int indexClassNamePasswordStart = tagPassword.IndexOf("name=") + 6;
                int indexClassNamePasswordEnd = tagPassword.IndexOf("\"", indexClassNamePasswordStart + 2);
                string classNamePassword = tagPassword.Substring(indexClassNamePasswordStart, indexClassNamePasswordEnd - indexClassNamePasswordStart);
                //Console.WriteLine("classNamePassword:" + classNamePassword);

                int indexPassphraseStart = myHtml.IndexOf("value=", indexPasswordTagStart) + 7;
                int indexPassphraseEnd = myHtml.IndexOf("\"", indexPassphraseStart);
                string passphrase = myHtml.Substring(indexPassphraseStart, indexPassphraseEnd - indexPassphraseStart);
                //Console.WriteLine("passphrase:" + passphrase);


                int indexUsernameText = myHtmlByPass.LastIndexOf("type=\"text");
                string myHtmlByUsername = myHtml.Substring(0, indexUsernameText);

                int indexUsernameTagStart = myHtmlByUsername.LastIndexOf("<");
                int indexUsernameTagEnd = myHtml.IndexOf(">", indexUsernameTagStart + 2) + 1;
                string tagUsername = myHtml.Substring(indexUsernameTagStart, indexUsernameTagEnd - indexUsernameTagStart);
                //Console.WriteLine("tagUserName:" + tagUsername);

                int indexClassNameUsernameStart = tagUsername.IndexOf("name=") + 6;
                int indexClassNameUsernameEnd = tagUsername.IndexOf("\"", indexClassNameUsernameStart + 2);
                string classNameUsername = tagUsername.Substring(indexClassNameUsernameStart, indexClassNameUsernameEnd - indexClassNameUsernameStart);
                //Console.WriteLine("classNameUsername:" + classNameUsername);

                int indexUsernameStart = myHtml.IndexOf("value=", indexUsernameTagStart) + 7;
                int indexUsernameEnd = myHtml.IndexOf("\"", indexUsernameStart);
                string username = myHtml.Substring(indexUsernameStart, indexUsernameEnd - indexUsernameStart);
                //Console.WriteLine("username:" + username);


                ClassNames myClassNames = new ClassNames();
                myClassNames.classNameUsername = classNameUsername;
                myClassNames.classNamePassword = classNamePassword;
                myClassNames.userName = username;
                myClassNames.password = passphrase;

                return myClassNames;
            }

            return null;
        }
    }
}