/* ========================================================================
 * Copyright (c) 2005-2019 The my Company, Inc. All rights reserved.
 *
 * my Company MIT License 1.00
 * 
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

using System;
using System.Windows.Forms;
using System.IO;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Client.Controls;
using System.Globalization;
//Add MySql Library
using MySql.Data.MySqlClient;

namespace Quickstarts.myMESClient
{
    /// <summary>
    /// The main form for a simple Quickstart Client application.
    /// </summary>
    public partial class MainForm : Form
    {
        #region Constructors
        /// <summary>
        /// Creates an empty form.
        /// </summary>
        private MainForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Creates a form which uses the specified client configuration.
        /// </summary>
        /// <param name="configuration">The configuration to use.</param>
        public MainForm(ApplicationConfiguration configuration)
        {
            InitializeComponent();
            ConnectServerCTRL.Configuration = m_configuration = configuration;
            ConnectServerCTRL.ServerUrl = "opc.tcp://desktop-6oo2d8b:62545/Quickstarts/myIMMControlServer";
            this.Text = m_configuration.ApplicationName;
        }
        #endregion

        #region Private Fields
        private ApplicationConfiguration m_configuration;
        private Session m_session;
        private bool m_connectedOnce;

        private Subscription m_subscription;
        private MonitoredItem monitoredItem;
        private double Actual_State;
        private uint Actual_State_handle;
        private string _comboBoxIndex;
        private int Piece_SN_Value;
        private uint Change_Mould_handle;
        private uint Start_Piece_handle;

        DataValueCollection results3;
        //DB
        private MySqlConnection DB_connection;
        private string DB_server = "localhost";
        private string DB_database = "db_project";
        private string DB_uid = "mateusqueiroz";
        private string DB_password = "S2det8dn.";
        #endregion

        #region Private Methods
        #endregion

        #region Event Handlers
        /// <summary>
        /// Connects to a server.
        /// </summary>
        private async void Server_ConnectMI_Click(object sender, EventArgs e)
        {
            try
            {
                await ConnectServerCTRL.Connect();
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
        }

        /// <summary>
        /// Disconnects from the current session.
        /// </summary>
        private void Server_DisconnectMI_Click(object sender, EventArgs e)
        {
            try
            {
                ConnectServerCTRL.Disconnect();
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
        }

        /// <summary>
        /// Prompts the user to choose a server on another host.
        /// </summary>
        private void Server_DiscoverMI_Click(object sender, EventArgs e)
        {
            try
            {
                ConnectServerCTRL.Discover(null);
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
        }

        /// <summary>
        /// Updates the application after connecting to or disconnecting from the server.
        /// </summary>
        private void Server_ConnectComplete(object sender, EventArgs e)
        {
            try
            {
                m_session = ConnectServerCTRL.Session;

                // set a suitable initial state.
                if (m_session != null && !m_connectedOnce)
                {
                    m_connectedOnce = true;
                }

                // browse the instances in the server.
                //BrowseCTRL.Initialize(m_session, ObjectIds.ObjectsFolder, ReferenceTypeIds.Organizes, ReferenceTypeIds.Aggregates);

                //verify if Session is established
                if (m_session != null)
                {
                    //verify if the Subscription was not already created
                    if (m_subscription == null)
                    {
                        //create the subscritpion
                        m_subscription = new Subscription(m_session.DefaultSubscription);
                        m_subscription.PublishingEnabled = true;
                        m_subscription.PublishingInterval = 1000; //1000msec interval
                        m_session.AddSubscription(m_subscription);
                        m_subscription.Create();
                    }
                 
                    //Add Start_Piece to monitored items
                    monitoredItem = new MonitoredItem(m_subscription.DefaultItem);
                    monitoredItem.StartNodeId = "ns=3;s=Start_Piece";
                    monitoredItem.AttributeId = Attributes.Value;
                    monitoredItem.MonitoringMode = MonitoringMode.Reporting;
                    monitoredItem.SamplingInterval = 1000;
                    monitoredItem.QueueSize = 0;
                    monitoredItem.DiscardOldest = true;
                    //get handle number
                    Start_Piece_handle = monitoredItem.ClientHandle;
                    //define event handler for this item, and then add to subscription
                    monitoredItem.Notification += new MonitoredItemNotificationEventHandler(monitoredItem_Notification);
                    m_subscription.AddItem(monitoredItem);
                    m_subscription.ApplyChanges();

                    //Add Change_Mould to monitored items
                    monitoredItem = new MonitoredItem(m_subscription.DefaultItem);
                    monitoredItem.StartNodeId = "ns=3;s=Change_Mould";
                    monitoredItem.AttributeId = Attributes.Value;
                    monitoredItem.MonitoringMode = MonitoringMode.Reporting;
                    monitoredItem.SamplingInterval = 1000;
                    monitoredItem.QueueSize = 0;
                    monitoredItem.DiscardOldest = true;
                    //get handle number
                    Change_Mould_handle = monitoredItem.ClientHandle;
                    //define event handler for this item, and then add to subscription
                    monitoredItem.Notification += new MonitoredItemNotificationEventHandler(monitoredItem_Notification);
                    m_subscription.AddItem(monitoredItem);
                    m_subscription.ApplyChanges();

                    //Add Actual_State to monitored items
                    monitoredItem = new MonitoredItem(m_subscription.DefaultItem);
                    monitoredItem.StartNodeId = "ns=3;s=Actual_State";
                    monitoredItem.AttributeId = Attributes.Value;
                    monitoredItem.MonitoringMode = MonitoringMode.Reporting;
                    monitoredItem.SamplingInterval = 1000;
                    monitoredItem.QueueSize = 0;
                    monitoredItem.DiscardOldest = true;
                    //get handle number
                    Actual_State_handle = monitoredItem.ClientHandle;
                    //define event handler for this item, and then add to subscription
                    monitoredItem.Notification += new MonitoredItemNotificationEventHandler(monitoredItem_Notification);
                    m_subscription.AddItem(monitoredItem);
                    m_subscription.ApplyChanges();

                    DataValueCollection results = Read_Value("ns=3;s=Mould_1");
                    DataValueCollection results1 = Read_Value("ns=3;s=Mould_2");
                    string Mould_1Value = (string)results[0].Value;
                    string Mould_2Value = (string)results1[0].Value;
                    comboBox1.Items.Add(Mould_1Value);
                    comboBox1.Items.Add(Mould_2Value);
                    //comboBox1.Text = Mould_1Value;

                    //Open connection to DB
                    string connectionString = "SERVER=" + DB_server + ";" +
                    "DATABASE=" + DB_database + ";" + "UID=" + DB_uid + ";" +
                    "PASSWORD=" + DB_password + ";";
                    DB_connection = new MySqlConnection(connectionString);
                    try
                    {
                        DB_connection.Open();
                    }
                    catch (MySqlException ex)
                    {
                        MessageBox.Show(ex.Message);
                    };
                }
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }            
        }

        void monitoredItem_Notification(MonitoredItem monitoredItem, MonitoredItemNotificationEventArgs e)
        {
            //This part of the code is needed to manage issues related to windows forms
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new MonitoredItemNotificationEventHandler(monitoredItem_Notification), monitoredItem, e);
                return;
            }
            MonitoredItemNotification notification = e.NotificationValue as MonitoredItemNotification;
            if (notification == null)
            {
                return;
            }            
            
            //is call because of atual_state?
            if (monitoredItem.ClientHandle == Actual_State_handle)
            {
                double value = (double)notification.Value.WrappedValue.Value;
                Actual_State = value;
                //update screen
                label4.Text = Actual_State.ToString();

                if (Actual_State > 1)
                {
                    try
                    {
                        bool arr = false;
                        WriteValue valueToWrite = new WriteValue();
                        valueToWrite.NodeId = "ns=3;s=Start_Piece";
                        valueToWrite.AttributeId = Attributes.Value;
                        valueToWrite.Value.Value = arr;
                        valueToWrite.Value.StatusCode = StatusCodes.Good;
                        valueToWrite.Value.ServerTimestamp = DateTime.MinValue;
                        valueToWrite.Value.SourceTimestamp = DateTime.MinValue;
                        WriteValueCollection valuesToWrite = new WriteValueCollection();
                        valuesToWrite.Add(valueToWrite);
                        // write current value.
                        StatusCodeCollection results = null;
                        DiagnosticInfoCollection diagnosticInfos = null;
                        m_session.Write(
                        null,
                        valuesToWrite,
                        out results,
                        out diagnosticInfos);
                        ClientBase.ValidateResponse(results, valuesToWrite);
                        ClientBase.ValidateDiagnosticInfos(diagnosticInfos, valuesToWrite);
                        if (StatusCode.IsBad(results[0]))
                        {
                            throw new ServiceResultException(results[0]);
                        }
                    }
                    catch (Exception exception)
                    {
                        ClientUtils.HandleException("Error Writing Value", exception);
                    }
                }
                
                if (Actual_State == 8)
                {
                    button1.Enabled = true;
                }
            }

            //is call because of Change_Mould?
            if (monitoredItem.ClientHandle == Change_Mould_handle)
            {
                bool value = (bool)notification.Value.WrappedValue.Value;
                //update screen
                if (value == false)
                {
                    button1.Enabled = true;
                }
            }
        }

        /// <summary>
        /// Updates the application after a communicate error was detected.
        /// </summary>
        private void Server_ReconnectStarting(object sender, EventArgs e)
        {
            try
            {
                //BrowseCTRL.ChangeSession(null);
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
        }

        /// <summary>
        /// Updates the application after reconnecting to the server.
        /// </summary>
        private void Server_ReconnectComplete(object sender, EventArgs e)
        {
            try
            {
                m_session = ConnectServerCTRL.Session;
                //BrowseCTRL.ChangeSession(m_session);
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
        }

        /// <summary>
        /// Cleans up when the main form closes.
        /// </summary>
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            ConnectServerCTRL.Disconnect();
        }
        #endregion

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Exit this application?", "my MESClient", MessageBoxButtons.YesNoCancel) == DialogResult.Yes)
            {
                Application.Exit();
            }
        }

        private void Help_ContentsMI_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start( Path.GetDirectoryName(Application.ExecutablePath) + "\\WebHelp\\overview_-_reference_client.htm");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to launch help documentation. Error: " + ex.Message);
            }
        }               

        private DataValueCollection Read_Value(string node)
        {
            //Read variables in the server’s address space
            ReadValueIdCollection nodesToRead = new ReadValueIdCollection();
            ReadValueId nodeToRead = new ReadValueId();
            nodeToRead.NodeId = node;
            nodeToRead.AttributeId = Attributes.Value;
            nodesToRead.Add(nodeToRead);

            DataValueCollection results = null;
            DiagnosticInfoCollection diagnosticInfos = null;

            m_session.Read(
            null,
            0,
            TimestampsToReturn.Neither,
            nodesToRead,
            out results,
            out diagnosticInfos);

            ClientBase.ValidateResponse(results, nodesToRead);
            ClientBase.ValidateDiagnosticInfos(diagnosticInfos, nodesToRead);

            return results;
        }
        
        private void comboBox1_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            
            //Change Mould
            string message = "Do you want to select this Mould?";
            var result = MessageBox.Show(message,(string)"Change Mould",MessageBoxButtons.YesNo);
            if(result == DialogResult.Yes)
            {
                //Show the Serial Number of the selected Mould
                if (comboBox1.SelectedIndex == 0)
                {
                    //Change the values of Change_Mould and Mould_Changed
                    try
                    {
                        bool arr = true;
                        WriteValue valueToWrite = new WriteValue();
                        valueToWrite.NodeId = "ns=3;s=Change_Mould";
                        valueToWrite.AttributeId = Attributes.Value;
                        valueToWrite.Value.Value = arr;
                        valueToWrite.Value.StatusCode = StatusCodes.Good;
                        valueToWrite.Value.ServerTimestamp = DateTime.MinValue;
                        valueToWrite.Value.SourceTimestamp = DateTime.MinValue;
                        WriteValueCollection valuesToWrite = new WriteValueCollection();
                        valuesToWrite.Add(valueToWrite);

                        // write current value.
                        StatusCodeCollection results2 = null;
                        DiagnosticInfoCollection diagnosticInfos2 = null;

                        m_session.Write(
                        null,
                        valuesToWrite,
                        out results2,
                        out diagnosticInfos2);
                        ClientBase.ValidateResponse(results2, valuesToWrite);
                        ClientBase.ValidateDiagnosticInfos(diagnosticInfos2, valuesToWrite);
                        if (StatusCode.IsBad(results2[0]))
                        {
                            throw new ServiceResultException(results2[0]);
                        }
                    }
                    catch (Exception exception)
                    {
                        ClientUtils.HandleException("Error Writing Value", exception);
                    }
                    try
                    {
                        bool arr = false;
                        WriteValue valueToWrite = new WriteValue();
                        valueToWrite.NodeId = "ns=3;s=Mould_Changed";
                        valueToWrite.AttributeId = Attributes.Value;
                        valueToWrite.Value.Value = arr;
                        valueToWrite.Value.StatusCode = StatusCodes.Good;
                        valueToWrite.Value.ServerTimestamp = DateTime.MinValue;
                        valueToWrite.Value.SourceTimestamp = DateTime.MinValue;
                        WriteValueCollection valuesToWrite = new WriteValueCollection();
                        valuesToWrite.Add(valueToWrite);

                        // write current value.
                        StatusCodeCollection results2 = null;
                        DiagnosticInfoCollection diagnosticInfos2 = null;

                        m_session.Write(
                        null,
                        valuesToWrite,
                        out results2,
                        out diagnosticInfos2);
                        ClientBase.ValidateResponse(results2, valuesToWrite);
                        ClientBase.ValidateDiagnosticInfos(diagnosticInfos2, valuesToWrite);
                        if (StatusCode.IsBad(results2[0]))
                        {
                            throw new ServiceResultException(results2[0]);
                        }
                    }
                    catch (Exception exception)
                    {
                        ClientUtils.HandleException("Error Writing Value", exception);
                    }

                    DataValueCollection values = Read_Value("ns=3;s=Mould_1_SN");
                    label8.Text = (string)values[0].Value;

                    //Update the value of Mould_tobeused
                    try
                    {
                        string arr = "1";
                        WriteValue valueToWrite = new WriteValue();
                        valueToWrite.NodeId = "ns=3;s=Mould_tobeused";
                        valueToWrite.AttributeId = Attributes.Value;
                        valueToWrite.Value.Value = arr;
                        valueToWrite.Value.StatusCode = StatusCodes.Good;
                        valueToWrite.Value.ServerTimestamp = DateTime.MinValue;
                        valueToWrite.Value.SourceTimestamp = DateTime.MinValue;
                        WriteValueCollection valuesToWrite = new WriteValueCollection();
                        valuesToWrite.Add(valueToWrite);

                        // write current value.
                        StatusCodeCollection results2 = null;
                        DiagnosticInfoCollection diagnosticInfos2 = null;

                        m_session.Write(
                        null,
                        valuesToWrite,
                        out results2,
                        out diagnosticInfos2);
                        ClientBase.ValidateResponse(results2, valuesToWrite);
                        ClientBase.ValidateDiagnosticInfos(diagnosticInfos2, valuesToWrite);
                        if (StatusCode.IsBad(results2[0]))
                        {
                            throw new ServiceResultException(results2[0]);
                        }
                    }
                    catch (Exception exception)
                    {
                        ClientUtils.HandleException("Error Writing Value", exception);
                    }

                    //Update the value of Actual_Mould
                    try
                    {
                        int arr = 1;
                        WriteValue valueToWrite = new WriteValue();
                        valueToWrite.NodeId = "ns=3;s=Actual_Mould";
                        valueToWrite.AttributeId = Attributes.Value;
                        valueToWrite.Value.Value = arr;
                        valueToWrite.Value.StatusCode = StatusCodes.Good;
                        valueToWrite.Value.ServerTimestamp = DateTime.MinValue;
                        valueToWrite.Value.SourceTimestamp = DateTime.MinValue;
                        WriteValueCollection valuesToWrite = new WriteValueCollection();
                        valuesToWrite.Add(valueToWrite);

                        // write current value.
                        StatusCodeCollection results2 = null;
                        DiagnosticInfoCollection diagnosticInfos2 = null;

                        m_session.Write(
                        null,
                        valuesToWrite,
                        out results2,
                        out diagnosticInfos2);
                        ClientBase.ValidateResponse(results2, valuesToWrite);
                        ClientBase.ValidateDiagnosticInfos(diagnosticInfos2, valuesToWrite);
                        if (StatusCode.IsBad(results2[0]))
                        {
                            throw new ServiceResultException(results2[0]);
                        }
                    }
                    catch (Exception exception)
                    {
                        ClientUtils.HandleException("Error Writing Value", exception);
                    }
                }
                else if (comboBox1.SelectedIndex == 1)
                {
                    //Change the values of Change_Mould and Mould_Changed
                    try
                    {
                        bool arr = true;
                        WriteValue valueToWrite = new WriteValue();
                        valueToWrite.NodeId = "ns=3;s=Change_Mould";
                        valueToWrite.AttributeId = Attributes.Value;
                        valueToWrite.Value.Value = arr;
                        valueToWrite.Value.StatusCode = StatusCodes.Good;
                        valueToWrite.Value.ServerTimestamp = DateTime.MinValue;
                        valueToWrite.Value.SourceTimestamp = DateTime.MinValue;
                        WriteValueCollection valuesToWrite = new WriteValueCollection();
                        valuesToWrite.Add(valueToWrite);

                        // write current value.
                        StatusCodeCollection results2 = null;
                        DiagnosticInfoCollection diagnosticInfos2 = null;

                        m_session.Write(
                        null,
                        valuesToWrite,
                        out results2,
                        out diagnosticInfos2);
                        ClientBase.ValidateResponse(results2, valuesToWrite);
                        ClientBase.ValidateDiagnosticInfos(diagnosticInfos2, valuesToWrite);
                        if (StatusCode.IsBad(results2[0]))
                        {
                            throw new ServiceResultException(results2[0]);
                        }
                    }
                    catch (Exception exception)
                    {
                        ClientUtils.HandleException("Error Writing Value", exception);
                    }
                    try
                    {
                        bool arr = false;
                        WriteValue valueToWrite = new WriteValue();
                        valueToWrite.NodeId = "ns=3;s=Mould_Changed";
                        valueToWrite.AttributeId = Attributes.Value;
                        valueToWrite.Value.Value = arr;
                        valueToWrite.Value.StatusCode = StatusCodes.Good;
                        valueToWrite.Value.ServerTimestamp = DateTime.MinValue;
                        valueToWrite.Value.SourceTimestamp = DateTime.MinValue;
                        WriteValueCollection valuesToWrite = new WriteValueCollection();
                        valuesToWrite.Add(valueToWrite);

                        // write current value.
                        StatusCodeCollection results2 = null;
                        DiagnosticInfoCollection diagnosticInfos2 = null;

                        m_session.Write(
                        null,
                        valuesToWrite,
                        out results2,
                        out diagnosticInfos2);
                        ClientBase.ValidateResponse(results2, valuesToWrite);
                        ClientBase.ValidateDiagnosticInfos(diagnosticInfos2, valuesToWrite);
                        if (StatusCode.IsBad(results2[0]))
                        {
                            throw new ServiceResultException(results2[0]);
                        }
                    }
                    catch (Exception exception)
                    {
                        ClientUtils.HandleException("Error Writing Value", exception);
                    }

                    DataValueCollection values = Read_Value("ns=3;s=Mould_2_SN");
                    label8.Text = (string)values[0].Value;

                    //Update the value of Mould_tobeused
                    try
                    {
                        string arr = "2";
                        WriteValue valueToWrite = new WriteValue();
                        valueToWrite.NodeId = "ns=3;s=Mould_tobeused";
                        valueToWrite.AttributeId = Attributes.Value;
                        valueToWrite.Value.Value = arr;
                        valueToWrite.Value.StatusCode = StatusCodes.Good;
                        valueToWrite.Value.ServerTimestamp = DateTime.MinValue;
                        valueToWrite.Value.SourceTimestamp = DateTime.MinValue;
                        WriteValueCollection valuesToWrite = new WriteValueCollection();
                        valuesToWrite.Add(valueToWrite);

                        // write current value.
                        StatusCodeCollection results2 = null;
                        DiagnosticInfoCollection diagnosticInfos2 = null;

                        m_session.Write(
                        null,
                        valuesToWrite,
                        out results2,
                        out diagnosticInfos2);
                        ClientBase.ValidateResponse(results2, valuesToWrite);
                        ClientBase.ValidateDiagnosticInfos(diagnosticInfos2, valuesToWrite);
                        if (StatusCode.IsBad(results2[0]))
                        {
                            throw new ServiceResultException(results2[0]);
                        }
                    }
                    catch (Exception exception)
                    {
                        ClientUtils.HandleException("Error Writing Value", exception);
                    }

                    //Update the value of Actual_Mould
                    try
                    {
                        int arr = 2;
                        WriteValue valueToWrite = new WriteValue();
                        valueToWrite.NodeId = "ns=3;s=Actual_Mould";
                        valueToWrite.AttributeId = Attributes.Value;
                        valueToWrite.Value.Value = arr;
                        valueToWrite.Value.StatusCode = StatusCodes.Good;
                        valueToWrite.Value.ServerTimestamp = DateTime.MinValue;
                        valueToWrite.Value.SourceTimestamp = DateTime.MinValue;
                        WriteValueCollection valuesToWrite = new WriteValueCollection();
                        valuesToWrite.Add(valueToWrite);

                        // write current value.
                        StatusCodeCollection results2 = null;
                        DiagnosticInfoCollection diagnosticInfos2 = null;

                        m_session.Write(
                        null,
                        valuesToWrite,
                        out results2,
                        out diagnosticInfos2);
                        ClientBase.ValidateResponse(results2, valuesToWrite);
                        ClientBase.ValidateDiagnosticInfos(diagnosticInfos2, valuesToWrite);
                        if (StatusCode.IsBad(results2[0]))
                        {
                            throw new ServiceResultException(results2[0]);
                        }
                    }
                    catch (Exception exception)
                    {
                        ClientUtils.HandleException("Error Writing Value", exception);
                    }
                }
                button1.Enabled = false;
            }
            else if(result == DialogResult.No)
            {
                comboBox1.SelectedIndexChanged -= comboBox1_SelectedIndexChanged_1;
                comboBox1.SelectedText = _comboBoxIndex;
                comboBox1.Text = _comboBoxIndex;
                comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged_1;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                int[] arr = { 1, 2 };
                WriteValue valueToWrite = new WriteValue();
                valueToWrite.NodeId = "ns=3;s=Actual_Mould";
                valueToWrite.AttributeId = Attributes.Value;
                if (comboBox1.SelectedIndex == 0)
                {
                    valueToWrite.Value.Value = arr[0];
                }
                else if(comboBox1.SelectedIndex == 1)
                {
                    valueToWrite.Value.Value = arr[1];
                }
                valueToWrite.Value.StatusCode = StatusCodes.Good;
                valueToWrite.Value.ServerTimestamp = DateTime.MinValue;
                valueToWrite.Value.SourceTimestamp = DateTime.MinValue;
                WriteValueCollection valuesToWrite = new WriteValueCollection();
                valuesToWrite.Add(valueToWrite);

                // write current value.
                StatusCodeCollection results2 = null;
                DiagnosticInfoCollection diagnosticInfos2 = null;

                m_session.Write(
                null,
                valuesToWrite,
                out results2,
                out diagnosticInfos2);
                ClientBase.ValidateResponse(results2, valuesToWrite);
                ClientBase.ValidateDiagnosticInfos(diagnosticInfos2, valuesToWrite);
                if (StatusCode.IsBad(results2[0]))
                {
                    throw new ServiceResultException(results2[0]);
                }
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException("Error Writing Value", exception);
            }

            //Start the production of the piece
            DataValueCollection results = Read_Value("ns=3;s=Start_Piece");
            bool Start_PieceValue = (bool)results[0].Value;            
            try
            {
                bool[] arr = { true, false };
                WriteValue valueToWrite = new WriteValue();
                valueToWrite.NodeId = "ns=3;s=Start_Piece";
                valueToWrite.AttributeId = Attributes.Value;
                if (Start_PieceValue)
                {
                    valueToWrite.Value.Value = arr[1];
                }
                else
                {
                    valueToWrite.Value.Value = arr[0];
                }
                valueToWrite.Value.StatusCode = StatusCodes.Good;
                valueToWrite.Value.ServerTimestamp = DateTime.MinValue;
                valueToWrite.Value.SourceTimestamp = DateTime.MinValue;
                WriteValueCollection valuesToWrite = new WriteValueCollection();
                valuesToWrite.Add(valueToWrite);

                // write current value.
                StatusCodeCollection results2 = null;
                DiagnosticInfoCollection diagnosticInfos2 = null;

                m_session.Write(
                null,
                valuesToWrite,
                out results2,
                out diagnosticInfos2);
                ClientBase.ValidateResponse(results2, valuesToWrite);
                ClientBase.ValidateDiagnosticInfos(diagnosticInfos2, valuesToWrite);
                if (StatusCode.IsBad(results2[0]))
                {
                    throw new ServiceResultException(results2[0]);
                }
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException("Error Writing Value", exception);
            }

            //Disable Start Piece button until piece is made
            button1.Enabled = false;

            //Assign a Serial Number to the pieces
            if (comboBox1.SelectedIndex == 0)
            {
                
                try
                {
                    int arr = Piece_SN_Value + 1;
                    WriteValue valueToWrite = new WriteValue();
                    valueToWrite.NodeId = "ns=3;s=Piece_SN";
                    valueToWrite.AttributeId = Attributes.Value;
                    valueToWrite.Value.Value = arr;
                    valueToWrite.Value.StatusCode = StatusCodes.Good;
                    valueToWrite.Value.ServerTimestamp = DateTime.MinValue;
                    valueToWrite.Value.SourceTimestamp = DateTime.MinValue;
                    WriteValueCollection valuesToWrite = new WriteValueCollection();
                    valuesToWrite.Add(valueToWrite);

                    // write current value.
                    StatusCodeCollection results2 = null;
                    DiagnosticInfoCollection diagnosticInfos2 = null;

                    m_session.Write(
                    null,
                    valuesToWrite,
                    out results2,
                    out diagnosticInfos2);
                    ClientBase.ValidateResponse(results2, valuesToWrite);
                    ClientBase.ValidateDiagnosticInfos(diagnosticInfos2, valuesToWrite);
                    if (StatusCode.IsBad(results2[0]))
                    {
                        throw new ServiceResultException(results2[0]);
                    }
                }
                catch (Exception exception)
                {
                    ClientUtils.HandleException("Error Writing Value", exception);
                }

                DataValueCollection results1 = Read_Value("ns=3;s=Piece_SN");
                Piece_SN_Value = (int)results1[0].Value;
            }
            else if (comboBox1.SelectedIndex == 1)
            {
                try
                {
                    int arr = Piece_SN_Value + 1;
                    WriteValue valueToWrite = new WriteValue();
                    valueToWrite.NodeId = "ns=3;s=Piece_SN";
                    valueToWrite.AttributeId = Attributes.Value;
                    valueToWrite.Value.Value = arr;
                    valueToWrite.Value.StatusCode = StatusCodes.Good;
                    valueToWrite.Value.ServerTimestamp = DateTime.MinValue;
                    valueToWrite.Value.SourceTimestamp = DateTime.MinValue;
                    WriteValueCollection valuesToWrite = new WriteValueCollection();
                    valuesToWrite.Add(valueToWrite);

                    // write current value.
                    StatusCodeCollection results2 = null;
                    DiagnosticInfoCollection diagnosticInfos2 = null;

                    m_session.Write(
                    null,
                    valuesToWrite,
                    out results2,
                    out diagnosticInfos2);
                    ClientBase.ValidateResponse(results2, valuesToWrite);
                    ClientBase.ValidateDiagnosticInfos(diagnosticInfos2, valuesToWrite);
                    if (StatusCode.IsBad(results2[0]))
                    {
                        throw new ServiceResultException(results2[0]);
                    }
                }
                catch (Exception exception)
                {
                    ClientUtils.HandleException("Error Writing Value", exception);
                }

                DataValueCollection results1 = Read_Value("ns=3;s=Piece_SN");
                Piece_SN_Value = (int)results1[0].Value;
            }

            label6.Text = Piece_SN_Value.ToString();

            //Read the value of the Serial Number of the Machine
            DataValueCollection results4 = Read_Value("ns=3;s=Machine_SN");
            string Machine_SN_Value = (string)results4[0].Value;

            //Read the value of the Serial Number of the Mould
            if (comboBox1.SelectedIndex == 0)
            {
                results3 = Read_Value("ns=3;s=Mould_1_SN");
                try
                {
                    int arr = 1;
                    WriteValue valueToWrite = new WriteValue();
                    valueToWrite.NodeId = "ns=3;s=Actual_Mould";
                    valueToWrite.AttributeId = Attributes.Value;
                    valueToWrite.Value.Value = arr;
                    valueToWrite.Value.StatusCode = StatusCodes.Good;
                    valueToWrite.Value.ServerTimestamp = DateTime.MinValue;
                    valueToWrite.Value.SourceTimestamp = DateTime.MinValue;
                    WriteValueCollection valuesToWrite = new WriteValueCollection();
                    valuesToWrite.Add(valueToWrite);

                    // write current value.
                    StatusCodeCollection results2 = null;
                    DiagnosticInfoCollection diagnosticInfos2 = null;

                    m_session.Write(
                    null,
                    valuesToWrite,
                    out results2,
                    out diagnosticInfos2);
                    ClientBase.ValidateResponse(results2, valuesToWrite);
                    ClientBase.ValidateDiagnosticInfos(diagnosticInfos2, valuesToWrite);
                    if (StatusCode.IsBad(results2[0]))
                    {
                        throw new ServiceResultException(results2[0]);
                    }
                }
                catch (Exception exception)
                {
                    ClientUtils.HandleException("Error Writing Value", exception);
                }

            }
            else if (comboBox1.SelectedIndex == 1)
            {
                results3 = Read_Value("ns=3;s=Mould_2_SN");
                try
                {
                    int arr = 2;
                    WriteValue valueToWrite = new WriteValue();
                    valueToWrite.NodeId = "ns=3;s=Actual_Mould";
                    valueToWrite.AttributeId = Attributes.Value;
                    valueToWrite.Value.Value = arr;
                    valueToWrite.Value.StatusCode = StatusCodes.Good;
                    valueToWrite.Value.ServerTimestamp = DateTime.MinValue;
                    valueToWrite.Value.SourceTimestamp = DateTime.MinValue;
                    WriteValueCollection valuesToWrite = new WriteValueCollection();
                    valuesToWrite.Add(valueToWrite);

                    // write current value.
                    StatusCodeCollection results2 = null;
                    DiagnosticInfoCollection diagnosticInfos2 = null;

                    m_session.Write(
                    null,
                    valuesToWrite,
                    out results2,
                    out diagnosticInfos2);
                    ClientBase.ValidateResponse(results2, valuesToWrite);
                    ClientBase.ValidateDiagnosticInfos(diagnosticInfos2, valuesToWrite);
                    if (StatusCode.IsBad(results2[0]))
                    {
                        throw new ServiceResultException(results2[0]);
                    }
                }
                catch (Exception exception)
                {
                    ClientUtils.HandleException("Error Writing Value", exception);
                }

            }
            string Mould_SN_Value = (string)results3[0].Value;

            DataValueCollection read = Read_Value("ns=3;s=Operator_ID");
            int Operator_ID_Value = (int)read[0].Value;

            //when new piece is made then update DB
            //format of convertion from real number to string
            NumberFormatInfo nfi = new NumberFormatInfo();
            nfi.NumberDecimalSeparator = ".";
            nfi.NumberGroupSeparator = "";
            nfi.NumberDecimalDigits = 2;
            //Execute command
            string query = Utils.Format("INSERT INTO db_project.table1 (Piece_SN, Machine_SN, Mould_SN, Operator_ID) VALUES({0},{1},{2},{3});",
            Piece_SN_Value.ToString("N", nfi), Machine_SN_Value, Mould_SN_Value, Operator_ID_Value.ToString("N", nfi));
            MySqlCommand cmd = new MySqlCommand(query, DB_connection);
            cmd.ExecuteNonQuery();
        }
    }
}