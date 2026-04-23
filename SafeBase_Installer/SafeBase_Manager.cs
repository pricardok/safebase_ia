using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using SafeBase_Installer.Core;
using SafeBase_Installer.ExtensionMethods;
using System.Management;
using SafeBase_Manager.Core;
using System.Xml;
using System.IO;
using System.Net;
using System.IO.Compression;
using SafeBase_Manager;
using System.Security.AccessControl;
using System.Web;


namespace SafeBase_Installer
{
    public partial class frmSafeBaseInstaller : Form
    {

        string setDataBase = "SafeBase";
        int typeInstall = 0;
        int JobType = 0;
        int ParamOn = 0;
        int EngineParamOn = 0;
        int SideBarOpen = 1;
        string MsgNoImplemented = "Not implemented, select the Offline option";
        string MsgImplemented = "Not implemented, wait for the new version.";
        string LoginRequired = "Ops Login Required";
        DataSet DSServerList = new DataSet();
        string SelectedEnabled = "";
        string SelectedCompany = "";
        string SelectedServer = "";
        string SelectedInstance = "";
        string SelectedServerInstance = "";

        public frmSafeBaseInstaller()
        {
            InitializeComponent();
        }

        private void AddLog(string log, string Paint)
        {
            if (Paint == "Result")
                txtRLog.SelectionColor = Color.Black;
            else if (Paint == "Information")
                txtRLog.SelectionColor = Color.Blue;
            else if (Paint == "Error")
                txtRLog.SelectionColor = Color.Red;
            else if (Paint == "Command")
                txtRLog.SelectionColor = Color.Green;

            txtRLog.AppendText(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " : " + log + "\r\n" + "\r\n");

            txtRLog.SelectionColor = Color.Black;

            txtRLog.ScrollToCaret();
        }

        private void MyConnectionInfoMessage(object sender, SqlInfoMessageEventArgs e)
        {
            AddLog(e.Message, "Result");
        }

        private void ExecuteSQLCommand(string cmd, string use, string AddToLog = "")
        {

            SqlConnection sqlConn = new SqlConnection();

            sqlConn.ConnectionString = GetConnectionString(use);

            try
            {
                foreach (var sqlBatch in cmd.Split(new[] { "GO" }, StringSplitOptions.RemoveEmptyEntries))
                {

                    if (sqlConn.State == ConnectionState.Closed)
                        sqlConn.Open();

                    SqlCommand SQLScriptToRun = new SqlCommand(cmd, sqlConn);

                    if (AddToLog == "")
                        AddLog(SQLScriptToRun.CommandText.ToString(), "Command");
                    else
                        AddLog(AddToLog, "Command");

                    sqlConn.InfoMessage += new SqlInfoMessageEventHandler(MyConnectionInfoMessage);

                    int rows = SQLScriptToRun.ExecuteNonQuery();

                    if (rows > 0)
                        AddLog("(" + rows.ToString() + " row(s) affected)", "Result");

                    sqlConn.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(" Ops Error - Check the message field");
                AddLog(ex.Message, "Error");

            }
        }

        private void ExecuteSQLCommandForeachGO(string cmd, string use, string AddToLog = "")
        {

            SqlConnection sqlConn = new SqlConnection();

            sqlConn.ConnectionString = GetConnectionString(use);

            try
            {
                foreach (var sqlBatch in cmd.Split(new[] { "GO" }, StringSplitOptions.RemoveEmptyEntries))
                {

                    if (sqlConn.State == ConnectionState.Closed)
                        sqlConn.Open();

                    SqlCommand SQLScriptToRun = new SqlCommand(sqlBatch, sqlConn);

                    if (AddToLog == "")
                        AddLog(SQLScriptToRun.CommandText.ToString(), "Command");
                    else
                        AddLog(AddToLog, "Command");

                    sqlConn.InfoMessage += new SqlInfoMessageEventHandler(MyConnectionInfoMessage);

                    int rows = SQLScriptToRun.ExecuteNonQuery();

                    if (rows > 0)
                        AddLog("(" + rows.ToString() + " row(s) affected)", "Result");

                    sqlConn.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(" Ops Error - Check the message field");
                AddLog(ex.Message, "Error");

            }
        }

        private void ExecuteSQLQuery(string cmd, string use)
        {
            SqlConnection sqlConn = new SqlConnection();
            sqlConn.ConnectionString = GetConnectionString(use);

            try
            {
                if (sqlConn.State == ConnectionState.Closed)
                    sqlConn.Open();

                SqlCommand SQLScriptToRun = new SqlCommand(cmd, sqlConn);

                AddLog(SQLScriptToRun.CommandText.ToString(), "Command");
                sqlConn.InfoMessage += new SqlInfoMessageEventHandler(MyConnectionInfoMessage);

                SqlDataReader reader = SQLScriptToRun.ExecuteReader();

                if (reader.HasRows)
                {
                    DataTable dt = new DataTable();
                    dt.Load(reader);
                    gridResults.DataSource = dt;

                    AddLog("(" + dt.Rows.Count.ToString() + " row(s) affected)", "Result");

                }
                else
                {
                    AddLog("(0 row(s) affected)", "Result");
                }

                rdInstalloff.Enabled = true;
                rdInstalloOn.Enabled = true;
                rdJobUpdateOff.Enabled = true;
                rdJobUpdateOn.Enabled = true;
                rdParamOff.Enabled = true;
                rdParamOn.Enabled = true;
                //toolStrip1.Enabled = true;
                txtCommandToRun.Enabled = true;
                btnExecute.Enabled = true;
                btnHideSideBar.Enabled = true;
                dropDatabase.Enabled = true;
                txtRLog.Enabled = true;
                tblResults.Enabled = true;
                rdMailDBOn.Enabled = true;
                rdMailDB_Off.Enabled = true;
                rdEngineUpdateOn.Enabled = true;
                rdEngineUpdateOff.Enabled = true;

                sqlConn.Close();
            }
            catch (Exception ex)
            {
                AddLog(ex.Message, "Error");
            }

        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            txtRLog.Text = "";
            AddLog("Trying to logon, and checking if user is SysAdmin...", "Information");
            string use = "";

            if (use == "")
                use = "master";
            else
                use = txtInstancebase.Text;

            ExecuteSQLQuery("PRINT SUSER_NAME()", use);
            ExecuteSQLQuery("PRINT IS_SRVROLEMEMBER('sysadmin', SUSER_NAME())", use);

            AddLog("Done", "Information");

        }

        private void btnLogoff_Click(object sender, EventArgs e)
        {
            rdInstalloff.Enabled = false;
            rdInstalloOn.Enabled = false;
            rdJobUpdateOff.Enabled = false;
            rdJobUpdateOn.Enabled = false;
            rdParamOff.Enabled = false;
            rdParamOn.Enabled = false;
            rdInstalloff.Checked = false;
            rdInstalloOn.Checked = false;
            rdJobUpdateOff.Checked = false;
            rdJobUpdateOn.Checked = false;
            rdParamOff.Checked = false;
            rdParamOn.Checked = false;
            btnResetParam.Enabled = false;
            btnUpdateParam.Enabled = false;
            btnDisableAlert.Enabled = false;
            btnCreateJobs.Enabled = false;
            btnJobsUpdate.Enabled = false;
            btnDisableJobs.Enabled = false;
            btnInstall.Enabled = false;
            btnGetSchema.Enabled = false;
            btnHideSideBar.Enabled = false;
            btnUninstall.Enabled = false;
            btnExecute.Enabled = false;
            dropDatabase.Enabled = false;
            txtCommandToRun.Enabled = false;
            txtRLog.Enabled = false;
            tblResults.Enabled = false;
            btnInstallMail.Enabled = false;
            btnUpdateMail.Enabled = false;
            btnDisableMail.Enabled = false;
            btnUpdate.Enabled = false;
            rdMailDBOn.Enabled = false;
            rdMailDB_Off.Enabled = false;
            btnUpdateEngineDB.Enabled = false;
            btnActiveJobs.Enabled = false;
            btnInstallEngine.Enabled = false;
            btnUpdateEngine.Enabled = false;
            rdEngineUpdateOn.Enabled = false;
            rdEngineUpdateOff.Enabled = false;
            txtRLog.Text = "";
            gridResults.Enabled = false;
            gridResults.Columns.Clear();

            AddLog("Done", "Information");

        }

        private void GetDataBaseMail2(int who)
        {

            if (typeInstall == 1)
            {
                txtRLog.Text = "";
                ExecuteSQLCommand(MailDependencies.Query("msdb"), setDataBase, "Create Data Base Mail");
                AddLog("Done", "Information");
            }
            if (typeInstall == 2)
            {
                txtRLog.Text = "";
                ExecuteSQLCommand(MailDependencies.Query("msdb"), setDataBase, "Update Data Base Mail");
                AddLog("Done", "Information");
            }
            if (typeInstall == 3)
            {
                txtRLog.Text = "";
                ExecuteSQLCommand(MailDelete.Query("msdb"), setDataBase, "Delete Account Data Base Mail");
                AddLog("Done", "Information");
            }

        }

        private void GetDataBaseMail(int who)
        {
            if (who == 1)
            {

                if (typeInstall == 1)
                {
                    txtRLog.Text = "";
                    ExecuteSQLCommand(MailDependencies.Query("msdb"), setDataBase, "Create Data Base Mail");
                    ExecuteSQLCommand(MailDependenciesReport.Query("msdb"), setDataBase, "Create Data Base Mail Report");
                    AddLog("Done", "Information");
                }

                if (typeInstall == 2)
                {
                    txtRLog.Text = "";
                    //AddLog("Not implemented", "Information");
                    MessageBox.Show(MsgNoImplemented);
                    AddLog("Done", "Information");
                }

            }

            if (who == 2)
            {

                if (typeInstall == 1)
                {
                    txtRLog.Text = "";
                    ExecuteSQLCommand(MailDependencies.Query("msdb"), setDataBase, "Update Data Base Mail");
                    ExecuteSQLCommand(MailDependenciesReport.Query("msdb"), setDataBase, "Create Data Base Mail Report");
                    AddLog("Done", "Information");
                }
                if (typeInstall == 2)
                {
                    txtRLog.Text = "";
                    //AddLog("Not implemented", "Information");
                    MessageBox.Show(MsgNoImplemented);
                    AddLog("Done", "Information");
                }

            }

            if (who == 3)
            {

                if (typeInstall == 1)
                {
                    txtRLog.Text = "";
                    ExecuteSQLCommand(MailDelete.Query("msdb"), setDataBase, "Delete Account Data Base Mail");
                    AddLog("Done", "Information");
                }
                if (typeInstall == 2)
                {
                    txtRLog.Text = "";
                    //AddLog("Not implemented", "Information");
                    MessageBox.Show(MsgNoImplemented);
                    AddLog("Done", "Information");
                }

            }

        }

        private void JobsAgent(int who)
        {
            if (who == 1)
            {
                
                if (JobType == 1)
                {
                    txtRLog.Text = "";
                    string SetServerName = GetServerName();
                    ExecuteSQLCommandForeachGO(CreateJobsAgentDependencies.Query(), setDataBase, "Creating job dependency...");
                    ExecuteSQLCommandForeachGO(CreateJobsAgent.Query("" + setDataBase + ""), setDataBase, "Job Creator...");
                    ExecuteSQLCommandForeachGO(CreateJobsAgentStep.Query("" + SetServerName + "", "" + setDataBase + ""), setDataBase, "Create Jobs MSDB...");
                    AddLog("Done", "Information");
                }

                if (JobType == 2)
                {
                    txtRLog.Text = "";
                    //AddLog("Not implemented", "Information");
                    MessageBox.Show(MsgNoImplemented);
                    AddLog("Done", "Information");
                }

            }

            if (who == 2)
            {
                
                if (JobType == 1)
                {
                    txtRLog.Text = "";
                    string SetServerName = GetServerName();
                    ExecuteSQLCommandForeachGO(CreateJobsAgentDependencies.Query(), setDataBase, "Updating job dependency...");
                    ExecuteSQLCommandForeachGO(CreateJobsAgent.Query("" + setDataBase + ""), setDataBase, "Updating Job Creator...");
                    ExecuteSQLCommandForeachGO(CreateJobsAgentStep.Query("" + SetServerName + "", "" + setDataBase + ""), setDataBase, "Updating Jobs MSDB...");
                    AddLog("Done", "Information");
                }
                if (JobType == 2)
                {
                    txtRLog.Text = "";
                    //AddLog("Not implemented", "Information");
                    MessageBox.Show(MsgNoImplemented);
                    AddLog("Done", "Information");
                }

            }

            if (who == 3)
            {
                txtRLog.Text = "";
                ExecuteSQLCommandForeachGO(DisableJobs.Query(), setDataBase, "Disable Jobs " + setDataBase + "...");
                AddLog("Done", "Information");
            }

            if (who == 4)
            {
                txtRLog.Text = "";
                ExecuteSQLCommandForeachGO(EnableJobs.Query(), setDataBase, "Enable Jobs " + setDataBase + "...");
                AddLog("Done", "Information");
            }

        }

        private void ParameterTool(int who)
        {
            if (who == 1)
            {
                //guidedb.Query();
                if (ParamOn == 1)
                {
                    txtRLog.Text = "";
                    MessageBox.Show(MsgImplemented);
                    AddLog("Done", "Information");
                }

                if (ParamOn == 2)
                {
                    txtRLog.Text = "";
                    //AddLog("Not implemented", "Information");
                    MessageBox.Show(MsgImplemented);
                    AddLog("Done", "Information");
                }

            }

            if (who == 2)
            {

                if (ParamOn == 1)
                {
                    txtRLog.Text = "";
                    MessageBox.Show(MsgImplemented);
                    AddLog("Done", "Information");
                }
                if (ParamOn == 2)
                {
                    txtRLog.Text = "";
                    //AddLog("Not implemented", "Information");
                    MessageBox.Show(MsgImplemented);
                    AddLog("Done", "Information");
                }

            }

            if (who == 3)
            {
                txtRLog.Text = "";
                //AddLog("Not implemented", "Information");
                MessageBox.Show(MsgImplemented);
                AddLog("Done", "Information");
            }

        }

        private void EngineTool(int who)
        {
            if (who == 1)
            {
                //guidedb.Query();
                if (EngineParamOn == 1)
                {
                    txtRLog.Text = "";
                    MessageBox.Show(MsgImplemented);
                    AddLog("Done", "Information");
                }

                if (EngineParamOn == 2)
                {
                    txtRLog.Text = "";
                    //AddLog("Not implemented", "Information");
                    MessageBox.Show(MsgImplemented);
                    AddLog("Done", "Information");
                }

            }

            if (who == 2)
            {
                
                if (EngineParamOn == 1)
                {
                    txtRLog.Text = "";
                    MessageBox.Show(MsgImplemented);
                    AddLog("Done", "Information");
                }
                if (EngineParamOn == 2)
                {
                    txtRLog.Text = "";
                    //AddLog("Not implemented", "Information");
                    MessageBox.Show(MsgImplemented);
                    AddLog("Done", "Information");
                }

            }

        }

        private void LicenseCheck_UpdateLastContact(string WhatItem)
        {

            try
            {
                ExecuteSQLCommand("UPDATE [" + setDataBase + "].[dbo].[ConfigDB] SET " + WhatItem + " = getdate()", "" + setDataBase + "", "Updating Last Contact Info");
            }
            catch (Exception ex)
            {
                AddLog("Error trying to update Config...", "Error");
                AddLog(ex.Message, "Error");
            }

        }

        private void btnInstall_Click(object sender, EventArgs e)
        {
            txtRLog.Text = "";
            string use = "master";
            SqlConnection sqlConn = new SqlConnection();
            sqlConn.ConnectionString = GetConnectionString(use);
            string GetBase = "SELECT * FROM master.dbo.sysdatabases WHERE name ='" + setDataBase + "'";

            if (typeInstall == 1)
            {
                try
                {
                    if (sqlConn.State == ConnectionState.Closed)
                        sqlConn.Open();

                    SqlCommand SQLScriptToRun = new SqlCommand(GetBase, sqlConn);
                    SqlDataReader reader = SQLScriptToRun.ExecuteReader();

                    if (reader.HasRows)
                    {
                        DataTable dt = new DataTable();
                        dt.Load(reader);
                        gridResults.DataSource = dt;
                        txtRLog.Text = "";
                        AddLog("already exists. If necessary, run the 'Uninstall Database' " + setDataBase + " first", "Information");
                        MessageBox.Show("already exists. if necessary, run the 'Uninstall Database' " + setDataBase + " first");
                        tblResults.Enabled = false;
                    }
                    else
                    {
                        sqlConn.Close();
                        AddLog("Database is being installed...", "Information");
                        ExecuteSQLCommand("CREATE DATABASE [" + setDataBase + "]", use);
                        ExecuteSQLCommand("ALTER DATABASE [" + setDataBase + "] SET RECOVERY SIMPLE WITH NO_WAIT", use);
                        ExecuteSQLCommand("ALTER DATABASE [" + setDataBase + "] SET TRUSTWORTHY ON", use);
                        ExecuteSQLCommand("sp_configure 'show advanced options', 1", use);
                        ExecuteSQLCommand("RECONFIGURE", use);
                        ExecuteSQLCommand("sp_configure 'clr enabled', 1", use);
                        ExecuteSQLCommand("RECONFIGURE", use);
                        ExecuteSQLCommand("sp_configure 'Ole Automation Procedures', 1", use);
                        ExecuteSQLCommand("RECONFIGURE", use);
                        ExecuteSQLCommand("sp_configure 'Database Mail XPs', 1", use);
                        ExecuteSQLCommand("RECONFIGURE", use);
                        AddLog("Database has been installed... OK", "Information");
                        AddLog("Basic configurations is being created... ", "Information");
                        AddLog("DONE", "Information");

                    }

                    sqlConn.Close();

                }
                catch (Exception ex)
                {
                    AddLog(ex.Message, "Error");
                }

            }
            if (typeInstall == 2)
            {
                txtRLog.Text = "";
                AddLog(MsgNoImplemented, "Information");
                MessageBox.Show(MsgNoImplemented);
                AddLog("Done", "Information");
            }

        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {

            if (typeInstall == 1)
            {
                txtRLog.Text = "";
                string message = "This process will delete and recreate the tables from " + setDataBase + " Database, any customization in the parameter tables will be lost, in addition to historical records. Do you Confirm?";
                string caption = "" + setDataBase + " Removal";

                MessageBoxButtons buttons = MessageBoxButtons.YesNo;
                DialogResult result;

                result = MessageBox.Show(message, caption, buttons);

                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    try
                    {
                        txtRLog.Text = "";
                        ExecuteSQLCommandForeachGO(TableDelete.Query("" + setDataBase + ""), setDataBase, "Delete Tables");
                        ExecuteSQLCommandForeachGO(CreateTables.Query("" + setDataBase + ""), setDataBase, "Create Tables");
                        ExecuteSQLCommandForeachGO(InsertTables.Query("" + setDataBase + ""), setDataBase, "Insert Tables");
                        AddLog("Done", "Information");
                    }
                    catch (Exception ex)
                    {
                        AddLog(ex.Message, "Error");
                    }

                }

                if (result == System.Windows.Forms.DialogResult.No)
                {
                    AddLog("Done", "Information");
                }
            }

            if (typeInstall == 2)
            {
                txtRLog.Text = "";
                AddLog(MsgNoImplemented, "Information");
                MessageBox.Show(MsgNoImplemented);
                AddLog("Done", "Information");
            }

        }

        private void btnUpdateEngineDB_Click(object sender, EventArgs e)
        {
            if (typeInstall == 1)
            {
                txtRLog.Text = "";
                ExecuteSQLCommand(UpdateAssembly.Query("" + setDataBase + ""), setDataBase, "Update Engine DataBase");
                AddLog("Done", "Information");
            }
            if (typeInstall == 2)
            {
                txtRLog.Text = "";
                AddLog(MsgNoImplemented, "Information");
                MessageBox.Show(MsgNoImplemented);
                AddLog("Done", "Information");
            }
        }

        private string GetConnectionString(string database)
        {

            string conn = "Data Source=" + txtServerNameAuthWin.Text.Trim() + ";Initial Catalog=" + database + ";Integrated Security=true";

            if (rdSQLServerAuthentication.Checked)
            {
                conn = "Data Source=" + txtServerName.Text.Trim() + ";Initial Catalog=" + database + ";User ID=" + textUserName.Text + ";Password=" + textPassword.Text + "";
            }

            return conn;
        }

        private string GetServerName()
        {

            string conn = "";
            string abs = "";

            string ServerName = Environment.MachineName;

            string InstancenameAuthWin = txtServerNameAuthWin.Text.Trim();
            abs = ServerName + InstancenameAuthWin;
            String phrase = abs;
            Console.WriteLine("{0}", phrase);
            phrase = phrase.Replace(".", "");
            conn = phrase;

            if (rdSQLServerAuthentication.Checked)
            {
                string instancenameSQLAuth = txtServerName.Text.Trim();
                abs = ServerName + instancenameSQLAuth;
                String phrase1 = abs;
                Console.WriteLine("{0}", phrase);
                phrase1 = phrase1.Replace(".", "");
                conn = phrase1;
            }

            return conn;
        }

        private string GetConnectionDB(string database)
        {

            if (database == "")
                database = "master";
            else
                database = txtInstancebase.Text;

            string conn = "Data Source=" + txtServerNameAuthWin + ";Initial Catalog=" + database + ";Integrated Security=true";

            if (rdSQLServerAuthentication.Checked)
            {
                conn = "Data Source=" + txtServerName + ";Initial Catalog=" + database + ";User ID=" + textUserName.Text + ";Password=" + textPassword.Text + "";
            }

            return conn;
        }

        private string GetConfig()
        {
            string ServerName = SelectedServerInstance;

            if (SelectedServer == "(local)" || SelectedServer == "localhost")
            {
                ServerName = Environment.MachineName;
            }

            string xml = "<Customer CompanyName='" + SelectedCompany + "' ServerName='" + ServerName + "' InstanceName='" + SelectedInstance +"' />";

            return xml.Replace("'", "''");
        }

        private void rdWindowsAuthentication_CheckedChanged_1(object sender, EventArgs e)
        {
            if (rdWindowsAuthentication.Checked)
            {
                rdSQLServerAuthentication.Checked = false;
                txtServerName.Enabled = false;
                txtInstancebase.Enabled = false;
                textUserName.Enabled = false;
                textPassword.Enabled = false;
                txtServerNameAuthWin.Enabled = true;
                //rdInstalloff.Enabled = true; 
            }
        }

        private void rdSQLServerAuthentication_CheckedChanged(object sender, EventArgs e)
        {
            if (rdSQLServerAuthentication.Checked)
            {
                rdWindowsAuthentication.Checked = false;
                txtServerName.Enabled = true;
                txtInstancebase.Enabled = true;
                textUserName.Enabled = true;
                textPassword.Enabled = true;
                txtServerNameAuthWin.Enabled = false;
                //rdInstalloff.Enabled = true;
                //rdInstalloOn.Enabled = true;
            }

        }

        private void rdInstalloff_CheckedChanged(object sender, EventArgs e)
        {

            if (rdInstalloff.Checked)
            {
                typeInstall = 1;

                //rdWindowsAuthentication.Checked = false;
                btnInstall.Enabled = true;
                btnGetSchema.Enabled = true;
                btnUpdate.Enabled = true;
                btnUninstall.Enabled = true;
                btnUpdateEngineDB.Enabled = true;

            }

        }

        private void rdInstalloOn_CheckedChanged(object sender, EventArgs e)
        {
            if (rdInstalloOn.Checked)
            {
                typeInstall = 2;

                //rdWindowsAuthentication.Checked = false;
                btnInstall.Enabled = true;
                btnGetSchema.Enabled = true;
                btnUpdate.Enabled = true;
                btnUninstall.Enabled = true;
                btnUpdateEngineDB.Enabled = true;

            }

        }

        private void rdJobUpdateOn_CheckedChanged(object sender, EventArgs e)
        {

            if (rdJobUpdateOn.Checked)
            {
                JobType = 2;

                //rdWindowsAuthentication.Checked = false;
                btnCreateJobs.Enabled = true;
                btnJobsUpdate.Enabled = true;
                btnDisableJobs.Enabled = true;
                btnActiveJobs.Enabled = true;

            }

        }

        private void rdJobUpdateOff_CheckedChanged(object sender, EventArgs e)
        {
            if (rdJobUpdateOff.Checked)
            {
                JobType = 1;

                //rdWindowsAuthentication.Checked = false;
                btnCreateJobs.Enabled = true;
                btnJobsUpdate.Enabled = true;
                btnDisableJobs.Enabled = true;
                btnActiveJobs.Enabled = true;

            }

        }

        private void rdParamOn_CheckedChanged(object sender, EventArgs e)
        {
            if (rdParamOn.Checked)
            {
                ParamOn = 2;

                //rdWindowsAuthentication.Checked = false;
                btnResetParam.Enabled = true;
                btnUpdateParam.Enabled = true;
                btnDisableAlert.Enabled = true;

            }

        }

        private void rdParamOff_CheckedChanged(object sender, EventArgs e)
        {
            if (rdParamOff.Checked)
            {
                ParamOn = 1;

                //rdWindowsAuthentication.Checked = false;
                btnResetParam.Enabled = true;
                btnUpdateParam.Enabled = true;
                btnDisableAlert.Enabled = true;

            }

        }

        private void rdMailDBOn_CheckedChanged(object sender, EventArgs e)
        {
            if (rdMailDBOn.Checked)
            {
                typeInstall = 2;

                btnInstallMail.Enabled = true;
                btnUpdateMail.Enabled = true;
                btnDisableMail.Enabled = true;

            }
        }

        private void rdMailDB_Off_CheckedChanged(object sender, EventArgs e)
        {
            if (rdMailDB_Off.Checked)
            {
                typeInstall = 1;

                btnInstallMail.Enabled = true;
                btnUpdateMail.Enabled = true;
                btnDisableMail.Enabled = true;

            }
        }

        private void btnGetSchema_Click(object sender, EventArgs e)
        {

            txtRLog.Text = "";
            string use =  setDataBase ;
            SqlConnection sqlConn = new SqlConnection();
            sqlConn.ConnectionString = GetConnectionString(use);
            string GetBase = "select * from sys.objects where name like 'Alerta%'";

            if (typeInstall == 1)
            {
                try
                {
                    if (sqlConn.State == ConnectionState.Closed)
                        sqlConn.Open();

                    SqlCommand SQLScriptToRun = new SqlCommand(GetBase, sqlConn);
                    SqlDataReader reader = SQLScriptToRun.ExecuteReader();

                    if (reader.HasRows)
                    {
                        DataTable dt = new DataTable();
                        dt.Load(reader);
                        gridResults.DataSource = dt;
                        txtRLog.Text = "";
                        AddLog("already exists. If necessary, run the 'Uninstall Database' " + setDataBase + " first or run 'Update Schema'", "Information"); 
                        MessageBox.Show("already exists. if necessary, run the 'Uninstall Database' " + setDataBase + " first or run 'Update Schema'");
                        tblResults.Enabled = false;
                    }
                    else
                    {
                        txtRLog.Text = "";
                        ExecuteSQLCommandForeachGO(CreateTables.Query("" + setDataBase + ""), setDataBase, "Create Tables");
                        ExecuteSQLCommandForeachGO(InsertTables.Query("" + setDataBase + ""), setDataBase, "Insert Tables");
                        ExecuteSQLCommandForeachGO(CreateAssembly.Query("" + setDataBase + ""), setDataBase, "Create Assembly");
                        ExecuteSQLCommandForeachGO(CreateProcFuncCLR.Query("" + setDataBase + ""), setDataBase, "Create Proc Func CLR");
                        ExecuteSQLCommandForeachGO(CreateFncDB.Query("" + setDataBase + ""), setDataBase, "Create Func...");
                        ExecuteSQLCommandForeachGO(CreateVwDB.Query("" + setDataBase + ""), setDataBase, "Create View...");
                        ExecuteSQLCommandForeachGO(CreateProc.Query("" + setDataBase + ""), setDataBase, "Create Proc..");
                        //ExecuteSQLCommandForeachGO(CreateTrigger.Query("" + setDataBase + ""), setDataBase, "Create Trigger..");
                        //ExecuteSQLCommandForeachGO(CreateProcN.Query("" + setDataBase + ""), setDataBase, "Create Proc N..");
                        //ExecuteSQLCommandForeachGO(CreateProcNoCLR.Query("" + setDataBase + ""), setDataBase, "Create Proc no CLR complementary...");
                        AddLog("Done", "Information");
                    }

                    sqlConn.Close();

                }
                catch (Exception ex)
                {
                    AddLog(ex.Message, "Error");
                }
            }

            if (typeInstall == 2)
            {
                txtRLog.Text = "";
                AddLog(MsgNoImplemented, "Information");
                MessageBox.Show(MsgNoImplemented);
                AddLog("Done", "Information");
            }

        }

        private void btnCreateJobs_Click(object sender, EventArgs e)
        {
            JobsAgent(1);
        }

        private void btnJobsUpdate_Click(object sender, EventArgs e)
        {
            JobsAgent(2);
        }

        private void btnDisableJobs_Click(object sender, EventArgs e)
        {
            JobsAgent(3);
        }

        private void btnActiveJobs_Click(object sender, EventArgs e)
        {
            JobsAgent(4);
        }

        private void btnResetParam_Click(object sender, EventArgs e)
        {
            ParameterTool(1);
        }

        private void btnUpdateParam_Click(object sender, EventArgs e)
        {
            ParameterTool(2);
        }

        private void btnDisableAlert_Click(object sender, EventArgs e)
        {
            ParameterTool(3);
        }

        private void btnInstallMail_Click(object sender, EventArgs e)
        {
            GetDataBaseMail(1);
        }

        private void btnUpdateMail_Click(object sender, EventArgs e)
        {
            GetDataBaseMail(2);
        }

        private void btnDisableMail_Click(object sender, EventArgs e)
        {
            GetDataBaseMail(3);
        }

        private void btnUninstall_Click(object sender, EventArgs e)
        {
            txtRLog.Text = "";
            string message = "It will Drop all " + setDataBase + " jobs and Drop " + setDataBase + " Database. Do you Confirm?";
            string caption = "" + setDataBase + " Removal";

            MessageBoxButtons buttons = MessageBoxButtons.YesNo;
            DialogResult result;

            result = MessageBox.Show(message, caption, buttons);

            if (result == System.Windows.Forms.DialogResult.Yes)
            {
                try
                {
                    AddLog("Removing " + setDataBase + " Jobs...", "Information");
                    ExecuteSQLCommand(DeleteJobs.Query(), "Master");
                    AddLog("Removing " + setDataBase + " Database...", "Information");
                    ExecuteSQLCommand("ALTER DATABASE [" + setDataBase + "] SET OFFLINE WITH ROLLBACK IMMEDIATE", "Master");
                    ExecuteSQLCommand("ALTER DATABASE [" + setDataBase + "] SET ONLINE", "Master");
                    ExecuteSQLCommand("DROP DATABASE [" + setDataBase + "]", "Master");

                    AddLog("DONE", "Information");
                }
                catch (Exception ex)
                {
                    AddLog(ex.Message, "Error");
                }
            }
        }

        private void btnExecute_Click(object sender, EventArgs e)
        {

            try
            {
                ExecuteSQLQuery(txtCommandToRun.Text, dropDatabase.Text);
                tblResults.Enabled = true;
            }
            catch (Exception ex)
            {
                AddLog(ex.Message, "Error");
                MessageBox.Show(LoginRequired);
            }

        }

        private void btnHideSideBar_Click(object sender, EventArgs e)
        {
            if (SideBarOpen == 1)
            {
                pnlSideBar.Width = 0;
                SideBarOpen = 0;
            }
            else
            {
                pnlSideBar.Width = 537;
                SideBarOpen = 1;
            }

        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtRLog.Text = "";
            txtCommandToRun.Text = "";
            gridResults.Columns.Clear();

        }

        private void SetSelectedValues(string Enabled, string Company, string Server, string Instance, string GUID)
        {
            if (SelectedEnabled != Enabled || SelectedCompany != Company || SelectedServer != Server || SelectedInstance != Instance.Replace("(", "").Replace(")", ""))
            {
                SelectedEnabled = Enabled;
                SelectedCompany = Company;
                SelectedServer = Server;
                SelectedInstance = Instance.Replace("(", "").Replace(")", "");

                SelectedServerInstance = SelectedServer + (SelectedInstance == "MSSQLSERVER" ? "" : "\\" + SelectedInstance);
                lblServer.Text = SelectedServerInstance;

                AddLog("\nSELECTION:" +
                       "\n   Enabled\t: " + SelectedEnabled +
                       "\n   Company\t: " + SelectedCompany +
                       "\n   Server\t\t: " + SelectedServer +
                       "\n   Instance\t: " + (SelectedInstance == "MSSQLSERVER" ? "(MSSQLSERVER)" : SelectedInstance),
                       "Information");
            }
        }

        public static String Reader_FirstRowColumnOnly(string scriptLine)
        {
            string connString = "Server=SRV01;Database=safebase;Trusted_Connection=True;";

            SqlDataReader reader;
            string Value = "";

            using (SqlConnection connection = new SqlConnection(connString))
            {
                SqlCommand command = new SqlCommand(scriptLine, connection);
                command.CommandType = CommandType.Text;

                connection.Open();
                reader = command.ExecuteReader();

                if (reader.HasRows)
                    while (reader.Read())
                        Value = Value + reader[0].ToString();

                reader.Close();
                connection.Close();
            }

            return Value;
        }

        private void link_whoisactive_LinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                VisitLink();
            }
            catch (Exception)
            {
                MessageBox.Show("Unable to open link that was clicked.");
            }
        }

        private void VisitLink()
        {
            link_whoisactive.LinkVisited = true;
            System.Diagnostics.Process.Start("https://github.com/amachanic/sp_whoisactive/releases");
        }

        private void lblServer_Click(object sender, EventArgs e)
        {
            lblServer.Text = Environment.MachineName;
        }


        private void InstallEngineDB_Click(object sender, EventArgs e)
        {
            EngineTool(1);
        }

        private void UpdateEngineDB_Click(object sender, EventArgs e)
        {
            EngineTool(2);
        }

        private void rdEngineUpdateOff_CheckedChanged(object sender, EventArgs e)
        {

            if (rdEngineUpdateOff.Checked)
            {
                EngineParamOn = 1;

                //rdWindowsAuthentication.Checked = false;
                btnInstallEngine.Enabled = true;
                btnUpdateEngine.Enabled = true;
 
            }

        }

        private void rdEngineUpdateOn_CheckedChanged(object sender, EventArgs e)
        {
            if (rdEngineUpdateOn.Checked)
            {
                EngineParamOn = 2;

                //rdWindowsAuthentication.Checked = false;
                btnInstallEngine.Enabled = true;
                btnUpdateEngine.Enabled = true;
 
            }

        }


    }
}
