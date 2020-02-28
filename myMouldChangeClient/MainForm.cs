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
using System.Collections.Generic;
using System.Drawing;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;
using System.IO;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Client.Controls;

namespace Quickstarts.myMouldChangeClient
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
        private Int32 myIntVarValue;
        private uint myArrayVar_handle;
        private uint Start_Piece_handle;
        private uint Model_handle;
        private uint Temperature_handle;
        private uint myVar_handle;
        private uint atual_state_handle;
        private double atual_state;
        private uint Actual_State_Handle;
        private double Actual_State;
        private uint Actual_State_handle;
        private double Operator_ID;
        private int Operator_ID_Value;


        //**********************************************************************************
        private uint Change_Mould_handle;
        private uint Mould_Changed_handle;
        private uint Mould_tobeused_handle;
        private string Mould_tobeused;

        //**********************************************************************************

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

                    //Add myVar to monitored items
                    monitoredItem = new MonitoredItem(m_subscription.DefaultItem);
                    monitoredItem.StartNodeId = "ns=3;s=myVar1";
                    monitoredItem.AttributeId = Attributes.Value;
                    monitoredItem.MonitoringMode = MonitoringMode.Reporting;
                    monitoredItem.SamplingInterval = 1000;
                    monitoredItem.QueueSize = 0;
                    monitoredItem.DiscardOldest = true;
                    //get handle number
                    myVar_handle = monitoredItem.ClientHandle;
                    //define event handler for this item, and then add to subscription
                    monitoredItem.Notification += new MonitoredItemNotificationEventHandler(monitoredItem_Notification);
                    m_subscription.AddItem(monitoredItem);
                    m_subscription.ApplyChanges();

                    //Add model to monitored items
                    monitoredItem = new MonitoredItem(m_subscription.DefaultItem);
                    monitoredItem.StartNodeId = "ns=6;i=6023";
                    monitoredItem.AttributeId = Attributes.Value;
                    monitoredItem.MonitoringMode = MonitoringMode.Reporting;
                    monitoredItem.SamplingInterval = 1000;
                    monitoredItem.QueueSize = 0;
                    monitoredItem.DiscardOldest = true;
                    //get handle number
                    Model_handle = monitoredItem.ClientHandle;
                    //define event handler for this item, and then add to subscription
                    monitoredItem.Notification += new MonitoredItemNotificationEventHandler(monitoredItem_Notification);
                    m_subscription.AddItem(monitoredItem);
                    m_subscription.ApplyChanges();

                    //Add myArrayVar to monitored items
                    monitoredItem = new MonitoredItem(m_subscription.DefaultItem);
                    monitoredItem.StartNodeId = "ns=3;s=myArrayVar";
                    monitoredItem.AttributeId = Attributes.Value;
                    monitoredItem.MonitoringMode = MonitoringMode.Reporting;
                    monitoredItem.SamplingInterval = 1000;
                    monitoredItem.QueueSize = 0;
                    monitoredItem.DiscardOldest = true;
                    //get handle number
                    myArrayVar_handle = monitoredItem.ClientHandle;
                    //define event handler for this item, and then add to subscription
                    monitoredItem.Notification += new MonitoredItemNotificationEventHandler(monitoredItem_Notification);
                    m_subscription.AddItem(monitoredItem);
                    m_subscription.ApplyChanges();

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

                    //**********************************************************************************

                    //Add Change Mould to monitored items
                    monitoredItem = new MonitoredItem(m_subscription.DefaultItem);
                    monitoredItem.StartNodeId = "ns=3;s=Mould_Changed";
                    monitoredItem.AttributeId = Attributes.Value;
                    monitoredItem.MonitoringMode = MonitoringMode.Reporting;
                    monitoredItem.SamplingInterval = 1000;
                    monitoredItem.QueueSize = 0;
                    monitoredItem.DiscardOldest = true;
                    //get handle number
                    Mould_Changed_handle = monitoredItem.ClientHandle;
                    //define event handler for this item, and then add to subscription
                    monitoredItem.Notification += new MonitoredItemNotificationEventHandler(monitoredItem_Notification);
                    m_subscription.AddItem(monitoredItem);
                    m_subscription.ApplyChanges();

                    //Add Change Mould to monitored items
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

                    //Add Mould_tobeused to monitored items 
                    monitoredItem = new MonitoredItem(m_subscription.DefaultItem);
                    monitoredItem.StartNodeId = "ns=3;s=Mould_tobeused";
                    monitoredItem.AttributeId = Attributes.Value;
                    monitoredItem.MonitoringMode = MonitoringMode.Reporting;
                    monitoredItem.SamplingInterval = 1000;
                    monitoredItem.QueueSize = 0;
                    monitoredItem.DiscardOldest = true;
                    //get handle number
                    Mould_tobeused_handle = monitoredItem.ClientHandle;
                    //define event handler for this item, and then add to subscription
                    monitoredItem.Notification += new MonitoredItemNotificationEventHandler(monitoredItem_Notification);
                    m_subscription.AddItem(monitoredItem);
                    m_subscription.ApplyChanges();

                    //**********************************************************************************


                    //Add atual_state to monitored items
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

            /*
            //is call because of myVar?
            if (monitoredItem.ClientHandle == myVar_handle)
            {
                double value = (double)notification.Value.WrappedValue.Value;
                myVar = value;
                //update screen
                label2.Text = "value: " + Utils.Format("{0}", myVar.ToString()) +
                ";\nStatusCode: " + Utils.Format("{0}", notification.Value.StatusCode.ToString()) +
                ";\nSource timestamp: " + notification.Value.SourceTimestamp.ToString() +
                ";\nServer timestamp: " + notification.Value.ServerTimestamp.ToString();
            }

            //is call because of Model?
            if (monitoredItem.ClientHandle == Model_handle)
            {
                LocalizedText value = (LocalizedText)notification.Value.WrappedValue.Value;
                Model = value;
                //update screen
                label6.Text = "value: " + Utils.Format("{0}", Model.Text) +
                ";\nStatusCode: " + Utils.Format("{0}", notification.Value.StatusCode.ToString()) +
                ";\nSource timestamp: " + notification.Value.SourceTimestamp.ToString() +
                ";\nServer timestamp: " + notification.Value.ServerTimestamp.ToString();
            }

            //is call because of myArrayVar?
            if (monitoredItem.ClientHandle == myArrayVar_handle)
            {
                double[] value = (double[])notification.Value.WrappedValue.Value;
                myArrayVar = value;

                //when new value of Temperature then update DB
                var sensors = new LineProtocolPoint("sensors", new Dictionary<string, object>{{ "temperature", Temperature},
                                                                                                { "myVar", myVar },}, new Dictionary<string, string>
                                                    {
                                                    { "machineID", "XYZ123"}
                                                    });
                var payload = new LineProtocolPayload();
                payload.Add(sensors);

                for (int i = 0; i < 10; i++)
                {
                    //myArrayVar[0] at present time - 900ms; myArrayVar[1] at present time - 800ms; ...; myArrayVar[9] at present time - 0ms
                    DateTime t = DateTime.UtcNow.Subtract(TimeSpan.FromMilliseconds((9 - i) * 100));
                    var another_sensor = new LineProtocolPoint(
                    "another_sensor",
                    new Dictionary<string, object>
                    {
                        { "myArrayVar", myArrayVar[i] },
                    },
                    new Dictionary<string, string>
                    {
                        { "Machine_ID", "XYZ123" }
                    },
                    t
                    );
                    payload.Add(another_sensor);
                }

                var client = new LineProtocolClient(new Uri(DB_server), DB_database);
                //send data to DB
                var influxResult = client.WriteAsync(payload);
            }
            */


            
            //is call because of Mould_tobeused?
            if (monitoredItem.ClientHandle == Mould_tobeused_handle)
            {
                string value = (string)notification.Value.WrappedValue.Value;
                Mould_tobeused = value;
                //update screen
                label4.Text = Mould_tobeused.ToString();

                
            }
            //is call because of Change_Mould?
            if (monitoredItem.ClientHandle == Change_Mould_handle)
            {
                bool value = (bool)notification.Value.WrappedValue.Value;

                if (value)
                {
                    MessageBox.Show("Trocar o molde para o Molde "+ Mould_tobeused);
                    button1.Enabled = true;
                }
                else
                {
                    button1.Enabled = false;
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
            if (MessageBox.Show("Exit this application?", "my MouldChangeClient", MessageBoxButtons.YesNoCancel) == DialogResult.Yes)
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

        // delete
        private void comboBox1_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex == 0)
            {
                DataValueCollection values = Read_Value("ns=3;s=Mould_1_SN");
                label6.Text = (string)values[0].Value;
            }
            else if (comboBox1.SelectedIndex == 1)
            {
                DataValueCollection values = Read_Value("ns=3;s=Mould_2_SN");
                label6.Text = (string)values[0].Value;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            
            DataValueCollection results = Read_Value("ns=3;s=Change_Mould");
            bool Change_Mould_Value = (bool)results[0].Value;

            DataValueCollection results1 = Read_Value("ns=3;s=Mould_Changed");
            bool Mould_Changed_Value = (bool)results1[0].Value;

            Operator_ID_Value = int.Parse(textBox1.Text);
            if (Mould_tobeused == label6.Text)
            {
                if (Operator_ID_Value <= 3 && Operator_ID_Value > 0)
                {
                    try
                    {
                        bool[] arr = { true, false };
                        WriteValue valueToWrite = new WriteValue();
                        valueToWrite.NodeId = "ns=3;s=Change_Mould";
                        valueToWrite.AttributeId = Attributes.Value;
                        if (Change_Mould_Value)
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


                    try
                    {
                        bool[] arr = { true, false };
                        WriteValue valueToWrite = new WriteValue();
                        valueToWrite.NodeId = "ns=3;s=Mould_Changed";
                        valueToWrite.AttributeId = Attributes.Value;
                        if (!Mould_Changed_Value)
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
                }
                else
                {
                    MessageBox.Show("This operator doesn't exist!");
                }
                try
                {
                    int arr = Operator_ID_Value;
                    WriteValue valueToWrite = new WriteValue();
                    valueToWrite.NodeId = "ns=3;s=Operator_ID";
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
            else
            {
                MessageBox.Show("O molde colacado não corresponde ao molde pedido!");
            }

            //results1 = Read_Value("ns=3;s=Piece_SN");
            //label6.Text = Piece_SN_Value.ToString();
            
        }



        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }
    }

}

