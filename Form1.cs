﻿using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Windows;
using System.Threading.Tasks;
using System.Net.Http;
using System.Reflection;
using System.Net;



namespace AndroidSideloader
{

    public partial class Form1 : Form
    {
#if DEBUG
            bool debugMode = true;
#else
        bool debugMode = false;
#endif
        string path;
        string obbPath = "";
        string obbFile;
        string allText;

        bool exit = false;
        string debugPath = "debug.log";
        public string adbPath = Environment.CurrentDirectory + "\\adb\\";
        string[] line;
        public Form1()
        {
            InitializeComponent();
        }

        public void runAdbCommand(string command)
        {  

            exit = false;

            Process cmd = new Process();
            cmd.StartInfo.FileName = Environment.CurrentDirectory + "\\adb\\adb.exe";
            cmd.StartInfo.Arguments = command;
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.StartInfo.WorkingDirectory = adbPath;
            cmd.Start();
            cmd.StandardInput.WriteLine(command);
            cmd.StandardInput.Flush();
            cmd.StandardInput.Close();
            allText = cmd.StandardOutput.ReadToEnd();
            cmd.WaitForExit();
            
            StreamWriter sw = File.AppendText(debugPath);
            sw.Write("Action name = " + command + '\n');
            sw.Write(allText);
            sw.Write("\n--------------------------------------------------------------------\n");
            sw.Flush();
            sw.Close();
            line = allText.Split('\n');
            exit = true;
        }

        private void sideload(string path)
        {
            Thread t1 = new Thread(() =>
            {
                runAdbCommand("install -r " + '"' + path + '"');
            });
            t1.IsBackground = true;
            t1.Start();
            t1.Join();
        }

        private async void startsideloadbutton_Click(object sender, EventArgs e)
        {

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Android apps (*.apk)|*.apk";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                    path = openFileDialog.FileName;
                else
                    return;
            }

            if (path == "" || path.EndsWith(".apk") == false)
                MessageBox.Show("You must select an apk");
            else
            {

                await Task.Run(() => sideload(path));

                MessageBox.Show(allText);
            }

        }

        private void devicesbutton_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Action Started, may take some time...");
            runAdbCommand("devices");
            MessageBox.Show(allText);
        }

        private void instructionsbutton_Click(object sender, EventArgs e)
        {
            string instructions = @"1. Plug in your Oculus Quest
2. Press adb devices and allow adb to connect from quest headset (one time only)
3. Press adb devices again and you should see a code and then 'device' (optional)
4. Select your apk with select apk button.
5. Press Sideload and wait...
6. If the game has an obb folder, select it by using select obb then press copy obb";
            MessageBox.Show(instructions);
        }

        public void ExtractFile(string sourceArchive, string destination)
        {
            string zPath = "7z.exe"; //add to proj and set CopyToOuputDir
                ProcessStartInfo pro = new ProcessStartInfo();
                pro.WindowStyle = ProcessWindowStyle.Hidden;
                pro.FileName = zPath;
                pro.Arguments = string.Format("x \"{0}\" -y -o\"{1}\"", sourceArchive, destination);
                Process x = Process.Start(pro);
                x.WaitForExit();
        }

        private void obbcopy(string obbPath)
        {
            Thread t1 = new Thread(() =>
            {
                runAdbCommand("push " + '"' + obbPath + '"' + " /sdcard/Android/obb");
            });
            t1.IsBackground = true;
            t1.Start();
            t1.Join();
        }

        private async void obbcopybutton_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    string[] files = Directory.GetFiles(fbd.SelectedPath);
                    obbFile = files[0];
                    obbPath = fbd.SelectedPath;

                }
                else return;
            }

            if (obbPath.Length>0)
            {
                await Task.Run(() => obbcopy(obbPath));
                
                MessageBox.Show(allText);
            }
            else
            {
                MessageBox.Show("You forgot to select the obb folder");
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (File.Exists(debugPath))
                File.Delete(debugPath);
            if (debugMode == false)
                debugbutton.Visible = false;
            if (Directory.Exists(adbPath)==false)
            {
                MessageBox.Show("Please wait for the software to download and install the adb");
                try
                {
                    using (var client = new WebClient())
                    {
                        ServicePointManager.Expect100Continue = true;
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        client.DownloadFile("https://github.com/nerdunit/androidsideloader/raw/master/7z.exe", "7z.exe");
                        client.DownloadFile("https://github.com/nerdunit/androidsideloader/raw/master/7z.dll", "7z.dll");
                        client.DownloadFile("https://github.com/nerdunit/androidsideloader/raw/master/adb.7z", "adb.7z");
                    }
                    ExtractFile(Environment.CurrentDirectory + "\\adb.7z", Environment.CurrentDirectory);
                    File.Delete("adb.7z");
                    File.Delete("7z.dll");
                    File.Delete("7z.exe");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Cannot download adb because you are not connected to the internet!");
                    StreamWriter sw = File.AppendText(debugPath);
                    sw.Write("\n++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++\n");
                    sw.Write(ex.ToString() + "\n");
                    sw.Flush();
                    sw.Close();
                    Environment.Exit(600);
                }
                
            }
            if (debugMode==false)
                checkForUpdate();
            intToolTips();
        }
        void intToolTips()
        {
            ToolTip ListAppsToolTip = new ToolTip();
            ListAppsToolTip.SetToolTip(this.ListApps, "Press this to show what packages you have installed");
        }
        void checkForUpdate()
        {
            try
            {
                string localVersion = "0.7";
                HttpClient client = new HttpClient();
                string currentVersion = client.GetStringAsync("https://raw.githubusercontent.com/nerdunit/androidsideloader/master/version").Result;
                currentVersion = currentVersion.Remove(currentVersion.Length - 1);
                if (localVersion != currentVersion)
                    MessageBox.Show("Your version is outdated, the latest version is " + currentVersion + " you can download it from https://github.com/nerdunit/", "OUTDATED");
            }
            catch
            {
            //No need for messages, the user has no internet
            }
        }

        private async void backup()
        {
            MessageBox.Show("Action Started, may take some time...");
            Thread t1 = new Thread(() =>
            {
                runAdbCommand("pull " + '"' + "/sdcard/Android/data" + '"');
            });
            t1.IsBackground = true;
            t1.Start();
            t1.Join();
        }

        private async void backupbutton_Click(object sender, EventArgs e)
        {
            if (exit==false)
            {
                MessageBox.Show("Finish Previous action first!");
                return;
            }

            await Task.Run(() => backup()); //we use async and await to not freeze the ui

            try
            {
                Directory.Move(adbPath + "data", Environment.CurrentDirectory + "\\data");
            }
            catch (Exception ex)
            {
                File.AppendAllText(debugPath, ex.ToString());
            }

            MessageBox.Show(allText);
        }

        private void debugbutton_Click(object sender, EventArgs e)
        {
            
        }

        private async void restore()
        {
            Thread t1 = new Thread(() =>
            {
                runAdbCommand("push " + '"' + obbPath + '"' + " /sdcard/Android/");
            });
            t1.IsBackground = true;
            t1.Start();
            t1.Join();
        }

        private async void restorebutton_Click(object sender, EventArgs e)
        {
            if (exit == false)
            {
                MessageBox.Show("Finish Previous action first!");
                return;
            }

            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    string[] files = Directory.GetFiles(fbd.SelectedPath);
                    obbPath = fbd.SelectedPath;
                }
                else return;
            }
                await Task.Run(() => restore());

                MessageBox.Show(allText);
        }

        private void customadbcmdbutton_Click(object sender, EventArgs e)
        {
            customAdbCommandForm adbCommandForm = new customAdbCommandForm();
            adbCommandForm.Show();
        }

        private async void listapps()
        {
            Thread t1 = new Thread(() =>
            {
                runAdbCommand("shell pm list packages");
            });
            t1.IsBackground = true;
            t1.Start();
            t1.Join();
        }

        private async void ListApps_Click(object sender, EventArgs e)
        {
            allText = "";

            comboBox1.Items.Clear();

            await Task.Run(() => listapps());

            foreach(string obj in line)
            {
                comboBox1.Items.Add(obj);
            }

            if (allText.Length > 0)
                MessageBox.Show("Fetched apks with success");
        }

        private async void getapk(string package)
        {
            Thread t1 = new Thread(() =>
            {
                runAdbCommand("shell pm path " + package);
            });
            t1.IsBackground = true;
            t1.Start();
            t1.Join();
        }

        private async void pullapk(string apkPath)
        {
            Thread t2 = new Thread(() =>
            {
                runAdbCommand("pull " + apkPath);
            });
            t2.IsBackground = true;
            t2.Start();
        }

        private async void getApkButton_Click(object sender, EventArgs e)
        {
            string package;
            allText = "";
            try
            {
                package = comboBox1.SelectedItem.ToString().Remove(0,8); //remove package:
                package = package.Remove(package.Length - 1);
            } catch { MessageBox.Show("You must first run list items"); return; }

            //MessageBox.Show(package);
            exit = false;


            await Task.Run(() => getapk(package));

            allText = allText.Remove(allText.Length - 1);
            //MessageBox.Show(allText);

            string apkPath = allText.Remove(0, 8); //remove package:
            apkPath = apkPath.Remove(apkPath.Length - 1);
            //MessageBox.Show(adbPath);
            exit = false;

            await Task.Run(() => pullapk(apkPath));

            string currApkPath = apkPath;
            while (currApkPath.Contains("/"))
                currApkPath = currApkPath.Substring(currApkPath.IndexOf("/") + 1);

            if (File.Exists(Environment.CurrentDirectory + "\\" + package + ".apk"))
                File.Delete(Environment.CurrentDirectory + "\\" + package + ".apk");

            File.Move(Environment.CurrentDirectory + "\\adb\\" + currApkPath, Environment.CurrentDirectory + "\\" + package + ".apk");

            //File.Delete(Environment.CurrentDirectory + "\\adb\\" + currApkPath);


            MessageBox.Show("Done");
        }

        private async void listappperms(string package)
        {
            Thread t1 = new Thread(() =>
            {
                runAdbCommand("shell dumpsys package " + package);
            });
            t1.IsBackground = true;
            t1.Start();
            t1.Join();
        }

        private async void listApkPermsButton_Click(object sender, EventArgs e)
        {
            string package;
            allText = "";
            try
            {
                package = comboBox1.SelectedItem.ToString().Remove(0, 8); //remove package:
                package = package.Remove(package.Length - 1);
            }
            catch { MessageBox.Show("You must first run list items"); return; }

            exit = false;

            await Task.Run(() => listappperms(package));

            var grantedPerms = allText.Substring(allText.LastIndexOf("install permissions:") + 22);
            grantedPerms.Substring(0, grantedPerms.IndexOf("User 0:"));

            line = grantedPerms.Split('\n');

            int pos1 = 12;
            int pos2 = 187;


            for (int i=0; i< line.Length; i++)
            {
                if (line[i].Contains("android.permission."))
                {
                    CheckBox chk = new CheckBox();
                    if (line[i].Contains("true"))
                        chk.Checked = true;
                    else
                        chk.Checked = false;
                    line[i] = line[i].Substring(0, line[i].IndexOf(": granted"));
                    line[i] = line[i].Substring(line[i].LastIndexOf(" "));


                    chk.Location = new System.Drawing.Point(pos1, pos2);
                    chk.Width = 420;
                    chk.Height = 17;
                    chk.Text = line[i];
                    chk.CheckedChanged += new EventHandler(CheckBox_Checked);
                    Controls.Add(chk);
                    pos2 += 20;
                }
            }

            

        }

        private void CheckBox_Checked(object sender, EventArgs e)
        {

        }

        private async void changePermsBtn_Click(object sender, EventArgs e)
        {
            string package;
            allText = "";
            try
            {
                package = comboBox1.SelectedItem.ToString().Remove(0, 8); //remove package:
                package = package.Remove(package.Length - 1);
            }
            catch { MessageBox.Show("You must first run list items"); return; }

            foreach (Control c in Controls)
            {
                if ((c is CheckBox))
                {
                    exit = false;
                    if (((CheckBox)c).Checked==true)
                    {
                        await Task.Run(() => changePerms(c, package, "grant"));
                    }
                    else
                    {
                        await Task.Run(() => changePerms(c, package, "revoke"));
                    }
                }
                
            }

            MessageBox.Show("Done!");

        }


        private async void changePerms(Control c, string package, string grant)
        {
            Thread t1 = new Thread(() =>
            {
                runAdbCommand("shell pm " + grant + " " + package + " " + c.Text);
            });
            t1.IsBackground = true;
            t1.Start();
            t1.Join();
        }

        private void launchApkButton_Click(object sender, EventArgs e)
        {
            exit = false;
            Thread t1 = new Thread(() =>
            {
                runAdbCommand("shell am start -n " + launchPackageTextBox.Text);
            });
            t1.IsBackground = true;
            t1.Start();

        }

        private void uninstallAppButton_Click(object sender, EventArgs e)
        {
            string package;
            allText = "";
            try
            {
                package = comboBox1.SelectedItem.ToString().Remove(0, 8); //remove package:
                package = package.Remove(package.Length - 1);
            }
            catch { MessageBox.Show("You must first run list items"); return; }

            DialogResult dialogResult = MessageBox.Show("Are you sure you want to uninstall " + package + " this CANNOT be undone!", "WARNING!", MessageBoxButtons.YesNo);
            if (dialogResult != DialogResult.Yes)
                return;

            exit = false;
            Thread t1 = new Thread(() =>
            {
                runAdbCommand("shell pm uninstall -k --user 0 " + package);
            });
            t1.IsBackground = true;
            t1.Start();

            MessageBox.Show(allText);
        }
    }

}
