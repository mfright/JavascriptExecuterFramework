using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.IO;
using System.Diagnostics;
using System.Reflection;

namespace JavascriptExecuter
{
    public partial class PasswordRecorder : Form
    {
        Form1 mainForm;



        public PasswordRecorder(string url)
        {
            InitializeComponent();


            int indexSlash = url.IndexOf("/", 8);
            if (indexSlash != -1)
            {
                url = url.Substring(0, indexSlash);
            }
            lblURL.Text = url;
            //txtUserName.Text = user;
            //txtPassword.Text = pass;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            // 保存
            File.AppendAllText(System.AppDomain.CurrentDomain.BaseDirectory + "\\passwords.ini", "\r\n" + lblURL.Text + "," + txtUserName.Text + "," + txtPassword.Text);
            
            // 起動
            Assembly myAssembly = Assembly.GetEntryAssembly();
            string path = myAssembly.Location;
            Process.Start(path);

            // 終了
            Application.Exit();
        }


        private void txtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnOK_Click(this, null);
            }
        }

        private void txtUserName_KeyDown(object sender, KeyEventArgs e)
        {
            

            if (e.KeyCode == Keys.Enter)
            {
                txtPassword.Focus();
            }
        }

        private void btnLater_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
