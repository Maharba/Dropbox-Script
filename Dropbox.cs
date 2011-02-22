// Version: 1
// ScriptName: Dropbox
// Compiler: C#
// References: System.dll;System.Xml.dll;System.Windows.Forms.dll;System.Drawing.dll;
// SupportedMedias: Image, AcceptsMultipleFiles
// MaxExecutionTime: 180

namespace Script
{
    using System;
    using System.Windows.Forms;
    using System.Drawing;
    using System.Text;
    using System.IO;
    using System.Net;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Xml.Serialization;
    using System.IO.IsolatedStorage;
    
    [Serializable]
    public class DropboxHelper
    {
        int    _uid;
        string _publicFolderPath;
        string _filePath;
        
        const string DROPBOX_URL = "http://dl.dropbox.com/u/";
        
        public DropboxHelper()
        { }
        
        public DropboxHelper(int uid)
        {
            _uid = uid;
        }
        
        public DropboxHelper(int uid, string publicFolderPath)
        {
            _uid = uid;
            _publicFolderPath = publicFolderPath;
        }
        
        [XmlIgnore]
        public string FilePath {
            get { return _filePath; }
            set { _filePath = value; }
        }
        
        public string PublicFolderPath {
            get { return _publicFolderPath; }
            set { _publicFolderPath = value; }
        }
        
        public int UID {
            get { return _uid; }
            set { _uid = value; }
        }
        
        /// <summary>
        /// Returns the link of the file from the Dropbox Public folder.
        ///
        /// Exception: System.ApplicationException
        /// </summary>
        /// <param name="filePath">The path of the file.</param>
        /// <returns>The public link of the file.</returns>
        public string GetPublicLink(string filePath)
        {
            if (filePath != null && filePath != string.Empty) {
                string locationFile = filePath.Substring(_publicFolderPath.Length);
                return string.Format("{0}{1}{2}", DROPBOX_URL, _uid, locationFile.Replace('\\', '/').Replace(" ", "%20"));
            } else
                throw new ApplicationException("filePath");
        }
    }
    
    public class BinarySerializer
    {
        public static T LoadFile<T>(string file)
        {
            T local;
            FileStream serializationStream = new FileStream(file, FileMode.Open);
            try {
                // DON'T UNCOMMENT, IT WILL PRODUCE A NUCLEAR EXPLOSION.
                //BinaryFormatter formatter = new BinaryFormatter();
                XmlSerializer formatter = new XmlSerializer(typeof(T));
                local = (T)formatter.Deserialize(serializationStream);
            } 
            catch {
                throw;
            }
            finally {
                serializationStream.Close();
            }
            return local;
        }
    
        public static void SaveFile<T>(string file, T tipo)
        {
            FileStream serializationStream = new FileStream(file, FileMode.Create);
            
            // DON'T UNCOMMENT, IT WILL PRODUCE A NUCLEAR EXPLOSION.
            //BinaryFormatter formatter = new BinaryFormatter();
            
            XmlSerializer formatter = new XmlSerializer(typeof(DropboxHelper));
            try {
                formatter.Serialize(serializationStream, tipo);
            }
            catch {
                throw;
            }
            finally {
                serializationStream.Close();
            }
        }
    }

    /// <summary>
    /// Provides methods to verify the availability of the links.
    /// </summary>
    public class MyClient : WebClient
    {
        bool _headOnly;
        
        /// <summary>
        /// Gets or sets to only retrieve the head. 
        /// If true, it'll download only the head; if false, it'll download the entire page.
        /// </summary>
        public bool HeadOnly {
            get { return _headOnly; }
            set { _headOnly = value; }
        }
        
        /// <summmary>
        /// Returns a WebRequest object for the specified resource which its method depends of the value from the property HeadOnly.
        /// <summary>
        /// <param name="address">A Uri that identifies the resource to request.</param>
        /// <returns>A new WebRequest object for the specified resource with its Method changed depending of the value from the property HeadOnly.</returns>
        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest req = base.GetWebRequest(address);
            if (HeadOnly && req.Method == "GET")
                req.Method = "HEAD";
            return req;
        }
        
        /// <summary>
        /// Verifies if the URL is valid.
        /// </summary>
        /// <param name="url">The URL that is going to be checked</param>
        /// <returns>True if the URL is valid; false if not.
        public static bool CheckUrl(string url)
        {
            using (MyClient client = new MyClient())
            {
               client.HeadOnly = true;
                try {
                    if (client.DownloadString(url) != null)
                        return true;
                } catch (WebException) {
                    return false;
                }
            }
            return false;
        }
    }

    public class VerifyingForm : Form
    {
        Label               _lblVerifying;
        BackgroundWorker    _backWorker;
        DropboxHelper       _dropbox;
        bool                _requestCompleted;
        
        public VerifyingForm(DropboxHelper dropbox)
        {
            this.DrawForm();
            _dropbox = dropbox;
        }
        
        private void _backWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            if (MyClient.CheckUrl(e.Argument.ToString()))
                e.Result = DialogResult.OK;
            else
                e.Result = DialogResult.Cancel;
        }
        
        private void _backWorkerRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            DialogResult dResult = (DialogResult)e.Result;
            if (dResult == DialogResult.OK) {
                
                // Saves the Dropbox Data on the App directory as an XML file to avoid requesting it every time when the App exits.
                BinarySerializer.SaveFile<DropboxHelper>(string.Format("{0}\\uid.xml", Path.GetDirectoryName(Application.ExecutablePath)), _dropbox);
                
                // ...and then copy the public link to the clipboard.
                Clipboard.SetText(_dropbox.GetPublicLink(_dropbox.FilePath));
                this.DialogResult = DialogResult.OK;
            }
            else if (dResult == DialogResult.Cancel) {
                MessageBox.Show("It's not valid.", "Dropbox Script", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.DialogResult = DialogResult.Cancel;
            }
            _requestCompleted = true;
            this.Close();
        }
        
        private void verFormLoading(object sender, EventArgs e)
        {
            _backWorker.RunWorkerAsync(_dropbox.GetPublicLink(_dropbox.FilePath));
        }
        
        private void verFormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_requestCompleted) e.Cancel = true;
        }
        
        private void DrawForm()
        {
            this._lblVerifying = new Label();
            this._backWorker = new BackgroundWorker();
            this.SuspendLayout();
            // 
            // _lblVerifying
            // 
            this._lblVerifying.AutoSize = true;
            this._lblVerifying.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            this._lblVerifying.Location = new Point(90, 23);
            this._lblVerifying.Name = "_lblVerifying";
            this._lblVerifying.Size = new Size(69, 16);
            this._lblVerifying.TabIndex = 0;
            this._lblVerifying.Text = "Verifying...";
            //
            // _backWorker (BackgroundWorker)
            //
            _backWorker.DoWork += _backWorkerDoWork;
            _backWorker.RunWorkerCompleted += _backWorkerRunWorkerCompleted;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new SizeF(6F, 13F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(244, 62);
            this.Controls.Add(this._lblVerifying);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "VerifyingForm";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Dropbox Script";
            this.Load += verFormLoading;
            this.FormClosing += verFormClosing;
            this.ResumeLayout(false);
            this.PerformLayout();
            this.BringToFront();
        }
    }
    
    public class DropboxAssistentForm : Form
    {
        Label               _lblUid;
        MaskedTextBox       _mskUid;
        Button              _btnOk;
        Button              _btnCancel;
        Label               _lblDropboxFolder;
        TextBox             _txtDropboxFolder;
        FolderBrowserDialog _fbdDropbox;
        Button              _btnBrowse;
        LinkLabel           _lnkDropboxUidTutorial;
        GroupBox            _gbSettings;
        DropboxHelper       _dropbox;
        
        public DropboxAssistentForm(string filePath)
        {
            if (filePath != null && filePath != string.Empty) {
                this.DrawForm();
                _dropbox = new DropboxHelper();
                _dropbox.FilePath = filePath;
            } else
                throw new ApplicationException("filePath");
        }
        
        private void _btnOkClick(object sender, EventArgs e)
        {
            int uid;
            if (int.TryParse(_mskUid.Text, out uid)) {
                _dropbox.UID = uid;
                this.Close();
            }
        }
        
        private void _btnBrowseClick(object sender, EventArgs e)
        {
            if (_fbdDropbox.ShowDialog() == DialogResult.OK) {
                _dropbox.PublicFolderPath = _fbdDropbox.SelectedPath;
                _txtDropboxFolder.Text = _dropbox.PublicFolderPath;
            }
        }
        
        private void _lnkDropboxUidTutorialLinkClicked(object sender, EventArgs e)
        {
            Process.Start("http://maharbaz.tumblr.com/post/3260527136/how-do-i-obtain-my-dropbox-uid");
        }
        
        private void thisClosing(object sender, FormClosingEventArgs e)
        {
            if (this.DialogResult == DialogResult.Cancel) return;
            if (_dropbox.PublicFolderPath != null && _dropbox.FilePath != null) {
                if (File.Exists(_dropbox.PublicFolderPath + '\\' + _dropbox.FilePath.TrimStart(_dropbox.PublicFolderPath.ToCharArray()))) {
                    VerifyingForm verForm = new VerifyingForm(_dropbox);
                    DialogResult dResult = verForm.ShowDialog();
                    if (dResult == DialogResult.OK)
                        e.Cancel = false;
                    else if (dResult == DialogResult.Cancel)
                        e.Cancel = true;
                } else {
                    MessageBox.Show("Invalid path.", "Dropbox Script", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    e.Cancel = true;
                }
            } else {
                MessageBox.Show("You must fill all the fields.", "Dropbox Script", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                e.Cancel = true;
            }
        }
        
        public void DrawForm()
        {
            _gbSettings = new GroupBox();
            _btnBrowse = new Button();
            _txtDropboxFolder = new TextBox();
            _lblDropboxFolder = new Label();
            _btnCancel = new Button();
            _btnOk = new Button();
            _mskUid = new MaskedTextBox();
            _lblUid = new Label();
            _lnkDropboxUidTutorial = new LinkLabel();
            _fbdDropbox = new FolderBrowserDialog();
            _gbSettings.SuspendLayout();
            this.SuspendLayout();
            // 
            // _gbSettings
            // 
            _gbSettings.Controls.Add(_lnkDropboxUidTutorial);
            _gbSettings.Controls.Add(_btnBrowse);
            _gbSettings.Controls.Add(_lblUid);
            _gbSettings.Controls.Add(_txtDropboxFolder);
            _gbSettings.Controls.Add(_mskUid);
            _gbSettings.Controls.Add(_lblDropboxFolder);
            _gbSettings.Location = new Point(12, 12);
            _gbSettings.Name = "_gbSettings";
            _gbSettings.Size = new Size(387, 105);
            _gbSettings.TabIndex = 7;
            _gbSettings.TabStop = false;
            _gbSettings.Text = "Settings";
            // 
            // _btnBrowse
            // 
            _btnBrowse.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            _btnBrowse.Location = new Point(350, 60);
            _btnBrowse.Name = "_btnBrowse";
            _btnBrowse.Size = new Size(31, 23);
            _btnBrowse.TabIndex = 4;
            _btnBrowse.Text = "...";
            _btnBrowse.UseVisualStyleBackColor = true;
            _btnBrowse.Click += _btnBrowseClick;
            // 
            // _txtDropboxFolder
            // 
            _txtDropboxFolder.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            _txtDropboxFolder.Location = new Point(161, 60);
            _txtDropboxFolder.Name = "_txtDropboxFolder";
            _txtDropboxFolder.Size = new Size(183, 23);
            _txtDropboxFolder.ReadOnly = true;
            _txtDropboxFolder.TabIndex = 3;
            _txtDropboxFolder.TextAlign = HorizontalAlignment.Center;
            // 
            // _lblDropboxFolder
            // 
            _lblDropboxFolder.AutoSize = true;
            _lblDropboxFolder.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            _lblDropboxFolder.Location = new Point(6, 65);
            _lblDropboxFolder.Name = "_lblDropboxFolder";
            _lblDropboxFolder.Size = new Size(116, 13);
            _lblDropboxFolder.TabIndex = 9;
            _lblDropboxFolder.Text = "Dropbox Public folder location";
            // 
            // _btnCancel
            // 
            _btnCancel.DialogResult = DialogResult.Cancel;
            _btnCancel.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            _btnCancel.Location = new Point(326, 123);
            _btnCancel.Name = "_btnCancel";
            _btnCancel.Size = new Size(75, 23);
            _btnCancel.TabIndex = 6;
            _btnCancel.Text = "Cancel";
            _btnCancel.UseVisualStyleBackColor = true;
            // 
            // _btnOk
            // 
            _btnOk.DialogResult = DialogResult.OK;
            _btnOk.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            _btnOk.Location = new System.Drawing.Point(245, 123);
            _btnOk.Name = "_btnOk";
            _btnOk.Size = new Size(75, 23);
            _btnOk.TabIndex = 5;
            _btnOk.Text = "OK";
            _btnOk.UseVisualStyleBackColor = true;
            _btnOk.Click += _btnOkClick;
            // 
            // _mskUid
            // 
            _mskUid.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            _mskUid.Location = new Point(132, 28);
            _mskUid.Name = "_mskUid";
            _mskUid.Size = new Size(73, 20);
            _mskUid.TabIndex = 1;
            _mskUid.TextAlign = HorizontalAlignment.Center;
            _mskUid.Mask = "0000000";
            _mskUid.PromptChar = '_';
            _mskUid.HidePromptOnLeave = true;
            // 
            // _lblUid
            // 
            _lblUid.AutoSize = true;
            _lblUid.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            _lblUid.Location = new Point(6, 33);
            _lblUid.Name = "_lblUid";
            _lblUid.Size = new Size(120, 13);
            _lblUid.TabIndex = 0;
            _lblUid.Text = "Enter your Dropbox UID";
            // 
            // _lnkDropboxUidTutorial
            // 
            _lnkDropboxUidTutorial.Location = new Point(218, 19);
            _lnkDropboxUidTutorial.Name = "_lnkDropboxUidTutorial";
            _lnkDropboxUidTutorial.Size = new Size(107, 32);
            _lnkDropboxUidTutorial.TabIndex = 2;
            _lnkDropboxUidTutorial.TabStop = true;
            _lnkDropboxUidTutorial.Text = "How do I obtain my Dropbox UID?";
            _lnkDropboxUidTutorial.TextAlign = ContentAlignment.MiddleCenter;
            _lnkDropboxUidTutorial.LinkClicked += _lnkDropboxUidTutorialLinkClicked;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new SizeF(6F, 13F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(415, 155);
            this.Controls.Add(_gbSettings);
            this.Controls.Add(_btnCancel);
            this.Controls.Add(_btnOk);
            this.DialogResult = DialogResult.None;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MainForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Configuration";
            this.FormClosing += thisClosing;
            _gbSettings.ResumeLayout(false);
            _gbSettings.PerformLayout();
            this.ResumeLayout(false);
        }
    }
    
    public class MyScript
    {
        public void Execute(string[] filePaths, Form parent)
        {
            if (filePaths.Length <= 1) {  // If one image is selected...
                if (File.Exists(string.Format("{0}\\uid.xml", Path.GetDirectoryName(Application.ExecutablePath)))) {
                    try {
                        DropboxHelper dh = BinarySerializer.LoadFile<DropboxHelper>(string.Format("{0}\\uid.xml", Path.GetDirectoryName(Application.ExecutablePath)));
                        if (dh.UID.ToString().Length == 7) { // Please, kill me for doing this...
                            dh.FilePath = filePaths[0];
                            Clipboard.SetText(dh.GetPublicLink(dh.FilePath));
                        } else {
                            MessageBox.Show("Invalid UID", "Dropbox Script", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }
                    catch (Exception e) {
                        MessageBox.Show("An error has occured while loading the configuration file.", "Dropbox Script", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        DropboxAssistentForm assistent = new DropboxAssistentForm(filePaths[0]);
                        assistent.ShowDialog();
                    }
                } else {
                    DropboxAssistentForm assistent = new DropboxAssistentForm(filePaths[0]);
                    assistent.ShowDialog();
                }
            } else
                MessageBox.Show("Select only one screenshot.", "Dropbox Script", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }
    }
}